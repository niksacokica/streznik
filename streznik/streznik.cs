using Newtonsoft.Json;
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

        private string info = "[INFO]\r\n";
        private string error = "[ERROR]\r\n";
        private string alert = "[ALERT]\r\n";

        delegate void SetTextCallback( TextBox type, string text );

        public Strežnik(){
            InitializeComponent();

            setStats( stats, "" );

            Timer cc = new Timer();
            cc.Tick += delegate {
                foreach (TcpClient cl in allClients.ToList() ){
                    if (!isConnected(cl.Client))
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
                type.AppendText( txt + ( type.Name.Equals( "log" ) ? "\r\n\r\n" : "\r\n" ) );
        }

        private void removeText( TextBox type, string txt ){
            if( connected.InvokeRequired )
                this.Invoke( new SetTextCallback( removeText ), new object[] { type, txt } );
            else{
                string[] tmp = connected.Text.Split( '\n' );
                foreach( string s in tmp ){
                    if( txt.Equals( s.Replace( "\r", "" ) ) ){
                        tmp = tmp.Where( w => w != s ).ToArray();
                        break;
                    }
                }

                connected.Text = string.Join( "\r\n", tmp );
            }
        }

        public static bool isConnected( Socket socket ){
            try{
                return !( socket.Poll( 1, SelectMode.SelectRead ) && socket.Available == 0 );
            }catch{
                return false;
            }
        }

        private void onClientConnect( TcpClient client, string cn ){
            allClients.Add(client);

            setText( log, info + cn + " has connected." );
            setText( connected, cn );

            statsD["CONNECTED CLIENTS"] = ( Int32.Parse( statsD["CONNECTED CLIENTS"] ) + 1 ).ToString();
            setStats( stats, "" );

            sendToClients( "SERVER", "update online", "sc", string.Join( "\r\n", allClients.Select( c => c.Client.RemoteEndPoint.ToString() ) ) );
        }

        private void onClientConnected( TcpClient client, NetworkStream ns, string cn ){
            while( sOn ){
                byte[] buffer = new byte[Int32.Parse( statsD["MESSAGE SIZE"] )];
                string read = "";
                try{
                    read = Encoding.UTF8.GetString( buffer, 0, ns.Read( buffer, 0, buffer.Length ) );
                }catch{}

                if( !string.IsNullOrEmpty( read ) ){
                    Dictionary<string, string> msg = JsonConvert.DeserializeObject<Dictionary<string, string>>( @read );

                    handleMessage( msg, cn );
                }
            }
        }

        private void onClientDisconnect( TcpClient client, string cn ){
            allClients.Remove( client );

            setText( log, info + cn + " has disconnected." );
            removeText( connected, cn );

            statsD["CONNECTED CLIENTS"] = ( Int32.Parse( statsD["CONNECTED CLIENTS"] ) - 1 ).ToString();
            setStats( stats, "" );

            sendToClients( "SERVER", "update online", "sc", string.Join( "\r\n", allClients.Select( c => c.Client.RemoteEndPoint.ToString() ) ) );
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
            onClientConnected( client, ns, cn );
            onClientDisconnect( client, cn );
        }

        private void serverToggle( bool on ){
            setText( log, info + ( on ? "Stopping server." : "Starting server." ) );

            if ( !on ){
                listener.Start();
                listener.BeginAcceptTcpClient( new AsyncCallback( callback ), null);
                sOn = !on;
            }
            else{
                setText(log, string.Join(", ", allClients.ToList()));

                foreach ( TcpClient cl in allClients.ToList() )
                    sendToClient( cl, "SERVER", "disconnect", "sc", "" );

                sOn = !on;
                try{
                    TcpClient stop = new TcpClient( statsD["IP"], Int32.Parse( statsD["PORT"] ) );
                    stop.GetStream().Close();
                    stop.Close();
                }catch{
                    foreach( TcpClient cl in allClients.ToList() )
                        sendToClient( cl, "SERVER", "disconnect", "sc", "" );
                }
            }

            statsD["SERVER STATUS"] = sOn ? "ON" : "OFF" ;
            setStats( stats, "" );
        }

        private void sendToClients( string who, string cmd, string type, string msg ){
            foreach( TcpClient cl in allClients.ToList() ){
                sendToClient( cl, who, cmd, type, msg );
            }
        }

        private void sendToClient( TcpClient cl, string who, string cmd, string type, string msg ){
            try{
                NetworkStream cls = cl.GetStream();

                Dictionary<string, string> forJson = new Dictionary<string, string>(){
                    { "sender", who },
                    { "command", cmd },
                    { "type", type },
                    { "message", msg }
                };

                string json = JsonConvert.SerializeObject(forJson);

                byte[] send = Encoding.UTF8.GetBytes(json.ToCharArray(), 0, json.Length);
                cls.Write(send, 0, send.Length);
            }catch{
                setText(log, "Unable to execute: " + cmd + ". Client not responding!");
            }
        }

        //tukaj dobimo text z chat boxa kdaj uporabnik pritisne enter
        private void chat_KeyPress( object sender, KeyPressEventArgs e ){
            if( e.KeyChar == ( char )13 && !string.IsNullOrEmpty( ( sender as TextBox ).Text ) ){
                string txt = (sender as TextBox).Text;
                ( sender as TextBox ).Text = "";

                e.Handled = true;

                log.AppendText( "[SERVER(YOU)]\r\n" + txt + "\r\n\r\n" );
                string ret = handleInput( txt );

                if( !string.IsNullOrEmpty( ret ) )
                    log.AppendText( ret + "\r\n\r\n" );
            }
        }

        //tukaj se preveri ali je uporabnik vnesel pravilen ukaz, in či je nekaj z njim naredimo
        private string handleInput( string txt ){
            string[] cmd = txt.Split( ' ' );

            switch( cmd[0] ){
                case "disconnect":
                    if( !sOn )
                        return alert + "Server is not on!";
                    else if( cmd.Length < 4 || ( cmd[1] != "sc" && cmd[1] != "c" ) )
                        return alert + "Argument error!";

                    foreach( TcpClient cl in allClients.ToList() ){
                        if( cmd[2].Equals( cl.Client.RemoteEndPoint.ToString() ) ){
                            string reason = string.Join( " ", cmd.Where( w => w != cmd[1] && w != cmd[0] && w != cmd[2] ).ToArray() );

                            sendToClient( cl, "SERVER", "disconnect", cmd[1], reason );

                            return info + "Disconnected \"" + cmd[2] + "\" from the server.";
                        }
                    }

                    return alert + "Unable to find \"" + cmd[2] + "\"!";
                case "exit":
                    Timer cls = new Timer();
                    cls.Tick += delegate{
                        this.Close();
                    };
                    cls.Interval = 1000;
                    cls.Start();

                    return sOn ? handleInput( "stop" ) : "";
                case "help":
                    return info + "Available commands are:"
                           + "\r\nhelp - shows help"
                           + "\r\ndisconnect [sc/c] [ip:port] [reason] - disconnects a user from the server"
                           + "\r\nexit - quit the program (will first turn off server if server is running)"
                           + "\r\nhelp - displays all commands"
                           + "\r\nmessage [ip:port] [message] - send a message to the client"
                           + "\r\nport [port] - changes the servers port to a new one (will stop server if its running)"
                           + "\r\nrestart - restarts the server"
                           + "\r\nsize [size] - change maximum size of incoming message"
                           + "\r\nstart - start the server"
                           + "\r\nstop - stop the serve"
                           + "\r\ntoggle - toggle the server state";
                case "message":
                    if( !sOn )
                        return alert + "Server is not on!";
                    else if( cmd.Length < 3 )
                        return alert + "Not enough arguments!";

                    foreach( TcpClient cl in allClients.ToList()){
                        if( cmd[1].Equals( cl.Client.RemoteEndPoint.ToString() ) ) {
                            string msg = string.Join( " ", cmd.Where( w => w != cmd[1] && w != cmd[0] ).ToArray() );
                            
                            sendToClient( cl, "SERVER", "", "m", msg );

                            return info + "Sent a message \"" + msg + "\" to \"" + cmd[1] + "\".";
                        }
                    }

                    return alert + "Unable to find \"" + cmd[1] + "\"!";
                case "port":
                    if( sOn )
                        setText( log, handleInput( "stop" ) );

                    try{
                        int num = Int32.Parse( cmd[1] );

                        if( num < 65353 && num > 0 )
                            statsD["PORT"] = num.ToString();

                        else
                            num = Int32.Parse( "" );
                    }catch{
                        return error + cmd[1] + " is not a valid number to be converted to a port!";
                    }

                    setStats( stats, "" );
                    listener = new TcpListener( IPAddress.Parse( statsD["IP"] ), Int32.Parse( statsD["PORT"] ) );
                    return info + "Changed port to: " + cmd[1];
                case "restart":
                    if( !sOn )
                        return alert + "Server is not on!";

                    setText( log, handleInput( "stop" ) );
                    Task.Delay( 5000 );
                    setText( log, handleInput( "start" ) );

                    Task.Delay(50000);
                    return info + "Server has been restarted.";
                case "start":
                    if( sOn )
                        return alert + "Server already running!";
                    else
                        serverToggle( sOn );

                    return info + "Server started.";
                case "stop":
                    if( !sOn )
                        return alert + "Server already stopped!";
                    else
                        serverToggle( sOn );

                    Task.Delay( 5000 );
                    return info + "Server stopped.";
                case "toggle":
                    return sOn ? handleInput( "stop" ) : handleInput( "start" );
                default:
                    return alert + "Unknown command: \"" + cmd[0] + "\"! Try help to get all commands.";
            }
        }

        private void handleMessage( Dictionary<string, string> msg, string sender ){
            switch( msg["type"] ){
                case "m":
                    if( msg["recepient"].Equals( "SERVER" ) || msg["recepient"].Equals( statsD["IP"] + ":" + statsD["PORT"] ) )
                        setText( log, "[" + sender + "]\r\n" + msg["message"] );
                    else if( msg["recepient"].Equals("all") ){
                        foreach( TcpClient cl in allClients.ToList() )
                            if( !sender.Equals( cl.Client.RemoteEndPoint.ToString() ) )
                                sendToClient( cl, sender, "", "ma", msg["message"]);

                        setText( log, "[" + sender + "] -> [all]\r\n" + msg["message"] );
                    }else{
                        foreach( TcpClient cl in allClients.ToList() )
                            if( msg["recepient"].Equals( cl.Client.RemoteEndPoint.ToString() ) ){
                                sendToClient( cl, sender, "", "m", msg["message"] );

                                setText( log, "[" + sender + "] -> [" + msg["recepient"] + "]\r\n" + msg["message"] );
                                break;
                            }
                    }

                    break;
                default:
                    break;
            }
        }
    }
}
