using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace streznik{
    public partial class Strežnik : Form {
        private static Dictionary<string, string> statsD = new Dictionary<string, string>(){
            { "IP", "127.0.0.1" },
            { "PORT", "1507" },
            { "MESSAGE SIZE", "1024" },
            { "SERVER STATUS", "OFF" },
            { "CONNECTED CLIENTS", "0" }
        };

        private TcpListener listener = new TcpListener( IPAddress.Parse( statsD["IP"] ), Int32.Parse( statsD["PORT"] ) );
        private static List<TcpClient> allClients = new List<TcpClient>();

        private bool sOn = false;

        private static int pad = 25;
        private string info = "[INFO]".PadLeft( pad, ' ' );
        private string error = "[ERROR]".PadLeft (pad, ' ' );
        private string alert = "[ALERT]".PadLeft( pad, ' ' );

        delegate void SetTextCallback(TextBox type, string text);

        public Strežnik(){
            InitializeComponent();

            setStats( stats, "" );

            Timer cc = new Timer();
            cc.Tick += delegate {
                foreach (TcpClient cl in allClients.ToList() ){
                    if (!IsConnected(cl.Client))
                        try{ onClientDisconnect(cl, cl.Client.RemoteEndPoint.ToString()); }catch{}
                }
            };
            cc.Interval = 1000;
            cc.Start();
        }

        private void setStats(TextBox type, string txt){
            if( type.InvokeRequired )
                this.Invoke( new SetTextCallback( setStats ), new object[] { type, txt });
            else{
                type.Text = "";
                foreach( KeyValuePair<string, string> stat in statsD )
                    type.AppendText( stat.Key + ":" + stat.Value.PadLeft( 15, ' ' ) + "\r\n" );
            }
        }

        private void setText( TextBox type, string txt ){
            if( type.InvokeRequired )
                this.Invoke( new SetTextCallback( setText ), new object[] { type, txt } );
            else
                type.AppendText( txt + "\r\n" );
        }

        private void removeText( TextBox type, string txt ){
            if( connected.InvokeRequired )
                this.Invoke( new SetTextCallback( removeText ), new object[] { type, txt } );
            else{
                string[] tmp = connected.Text.Split( '\n' );
                for( int i = 0; i < tmp.Length; i++ ){
                    if( string.Equals( tmp[i].Replace( "\r", "" ), txt ) ){
                        tmp = tmp.Where( w => w != tmp[i] ).ToArray();
                        break;
                    }
                }

                connected.Text = string.Join( "\r\n", tmp );
            }
        }

        public static bool IsConnected( Socket socket ){
            try{
                return !( socket.Poll( 1, SelectMode.SelectRead ) && socket.Available == 0 );
            }catch{
                return false;
            }
        }

        private void onClientConnect( TcpClient client, string cn ){
            allClients.Add(client);

            setText( log, cn + " has connected." + info );
            setText( connected, cn );

            statsD["CONNECTED CLIENTS"] = ( Int32.Parse( statsD["CONNECTED CLIENTS"] ) + 1 ).ToString();
            setStats( stats, "" );
        }

        private void onClientConnected( TcpClient client, NetworkStream ns, string cn ){
            byte[] buffer = new byte[Int32.Parse( statsD["MESSAGE SIZE"] )];
            string read;
            try{
                read = Encoding.UTF8.GetString( buffer, 0, ns.Read( buffer, 0, buffer.Length ) );
                ns.Close();
            }catch{
                setText( log, "Couldn't read data! Remote host disconnected!" + alert );

                return;
            }
            
            if( !string.IsNullOrEmpty( read ) )
                Task.Run( async () => handleMessage( read ) );
        }

        private void onClientDisconnect( TcpClient client, string cn ){
            allClients.Remove( client );

            setText( log, cn + " has disconnected." + info );
            removeText( connected, cn );

            statsD["CONNECTED CLIENTS"] = ( Int32.Parse( statsD["CONNECTED CLIENTS"] ) - 1 ).ToString();
            setStats( stats, "" );
        }

        private void callback( IAsyncResult iar ){
            TcpClient client = listener.EndAcceptTcpClient( iar );
            if( !sOn ){
                listener.Stop();
                return;
            }

            listener.BeginAcceptTcpClient( new AsyncCallback( callback ), null );
            NetworkStream ns = client.GetStream();
            string cn = client.Client.RemoteEndPoint.ToString();

            onClientConnect( client, cn );
            while( sOn ){
                onClientConnected(client, ns, cn);
            }
            onClientDisconnect( client, cn );
        }

        private async void serverToggle( bool on ){
            setText( log, ( on ? "Stopping server." : "Starting server." ) + info );

            if ( !on ){
                listener.Start();
                listener.BeginAcceptTcpClient( new AsyncCallback( callback ), null);
                sOn = !on;

                statsD["SERVER STATUS"] = "ON";
            }
            else{
                foreach ( TcpClient cl in allClients.ToList() )
                    sendToClient( cl, "SERVER sc disconnect" );

                sOn = !on;
                TcpClient stop = new TcpClient( statsD["IP"], Int32.Parse( statsD["PORT"] ) );
                stop.GetStream().Close();
                stop.Close();

                statsD["SERVER STATUS"] = "OFF";
            }

            setStats(stats, "");
        }

        private void sendToClient( TcpClient cl, string msg ){
            NetworkStream cls = cl.GetStream();

            byte[] send = Encoding.UTF8.GetBytes( msg.ToCharArray(), 0, msg.Length );
            cls.Write( send, 0, send.Length );
        }

        //tukaj dobimo text z chat boxa kdaj uporabnik pritisne enter
        private void chat_KeyPress( object sender, KeyPressEventArgs e ){
            if( e.KeyChar == ( char )13 && !string.IsNullOrEmpty( ( sender as TextBox ).Text ) ){
                string txt = (sender as TextBox).Text;
                ( sender as TextBox ).Text = "";

                e.Handled = true;

                log.AppendText( txt + "[SERVER(YOU)]".PadLeft( pad, ' ' ) + "\r\n" );
                string ret = handleCommand( txt );

                if( !string.IsNullOrEmpty( ret ) )
                    log.AppendText( ret + "\r\n" );
            }
        }

        //tukaj se preveri ali je uporabnik vnesel pravilen ukaz, in či je nekaj z njim naredimo
        private string handleCommand(string txt){
            string[] cmd = txt.Split( ' ' );
            string help = "".PadLeft( pad, ' ' );

            switch( cmd[0] ){
                case "disconnect":
                    if( !sOn )
                        return "Server is not on!" + alert;
                    else if( cmd.Length < 3 || ( cmd[1] != "sc" && cmd[1] != "c" ) )
                        return "Argument error!" + alert;

                    foreach( TcpClient cl in allClients.ToList() ){
                        if( string.Equals( cmd[2], cl.Client.RemoteEndPoint.ToString() ) ){
                            string msg = string.Join( " ", cmd.Where( w => w != cmd[1] && w != cmd[0] ).ToArray() );

                            sendToClient( cl, "SERVER " + cmd[1] + " disconnect" );

                            return "Disconnected \"" + cmd[2] + "\" from the server." + info;
                        }
                    }

                    return "Unable to find \"" + cmd[2] + "\"!" + alert;
                case "exit":
                    Timer cls = new Timer();
                    cls.Tick += delegate{
                        this.Close();
                    };
                    cls.Interval = 1000;
                    cls.Start();

                    return sOn ? handleCommand( "stop" ) : "";
                case "help":
                    return "Available commands are:" + info + "\r\nhelp - shows help" + help
                           + "\r\ndisconnect [sc/c] [ip:port] - disconnects a user from the server" + help
                           + "\r\nexit - quit the program (will first turn off server if server is running)" + help
                           + "\r\nhelp - displays all commands" + help
                           + "\r\nmessage [ip:port] [message] - send a message to the client" + help
                           + "\r\nport [port] - changes the servers port to a new one (will stop server if its running)" + help
                           + "\r\nrestart - restarts the server" + help
                           + "\r\nsize [size] - change maximum size of incoming message" + help
                           + "\r\nstart - start the server" + help
                           + "\r\nstop - stop the server" + help
                           + "\r\ntoggle - toggle the server state" + help;
                case "message":
                    if( !sOn )
                        return "Server is not on!" + alert;
                    else if( cmd.Length < 3 )
                        return "Not enough arguments!" + alert;

                    foreach( TcpClient cl in allClients.ToList()){
                        if( string.Equals( cmd[1], cl.Client.RemoteEndPoint.ToString() ) ) {
                            string msg = string.Join( " ", cmd.Where( w => w != cmd[1] && w != cmd[0] ).ToArray() );
                            
                            sendToClient( cl, "SERVER m " + msg );

                            return "Sent a message \"" + msg + "\" to \"" + cmd[1] + "\"." + info;
                        }
                    }

                    return "Unable to find \"" + cmd[1] + "\"!" + alert;
                case "port":
                    if( sOn )
                        setText( log, handleCommand( "stop" ) );

                    try{
                        int num = Int32.Parse( cmd[1] );

                        if( num < 65353 && num > 0 )
                            statsD["PORT"] = num.ToString();

                        else
                            num = Int32.Parse( "" );
                    }catch{
                        return cmd[1] + " is not a valid number to be converted to a port!" + error;
                    }

                    setStats( stats, "" );
                    listener = new TcpListener( IPAddress.Parse( statsD["IP"] ), Int32.Parse( statsD["PORT"] ) );
                    return "Changed port to: " + cmd[1] + info;
                case "restart":
                    if( !sOn )
                        return "Server is not on!" + alert;

                    setText( log, handleCommand( "stop" ) );
                    setText( log, handleCommand( "start" ) );

                    return "Server has been restarted." + info;
                case "size":
                    try{
                        int num = Int32.Parse( cmd[1] );

                        if( num > 0 )
                            statsD["MESSAGE SIZE"] = num.ToString();

                        else
                            num = Int32.Parse("");
                    }
                    catch( Exception ){
                        return cmd[1] + " is not a valid number to be converted to a message size!" + error;
                    }

                    setStats(stats, "");
                    return "Changed message size to: " + cmd[1] + info;
                case "start":
                    if( sOn )
                        return "Server already running!" + alert;
                    else
                        serverToggle( sOn );

                    return "Server started." + info;
                case "stop":
                    if( !sOn )
                        return "Server already stopped!" + alert;
                    else
                        serverToggle( sOn );
                    
                    return "Server stopped." + info;
                case "toggle":
                    return sOn ? handleCommand( "stop" ) : handleCommand( "start" );
                default:
                    return "Unknown command: \"" + cmd[0] + "\"! Try help to get all commands." + alert;
            }
        }

        private async void handleMessage( string msg ){
            string[] msgAr = msg.Split( ' ' );

            switch( msgAr[0] ){
                case "COMMAND":
                    break;
                case "MESSAGE":
                    break;
                default:
                    break;
            }
            //setText(log, read + ("[" + cn + "]").PadLeft(pad, ' '));
        }
    }
}
