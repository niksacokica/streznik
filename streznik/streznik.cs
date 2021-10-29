using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace streznik{
    public partial class Strežnik : Form{
        private static string ip = "127.0.0.1";
        private static int port = 1507;
        private TcpListener listener = new TcpListener( IPAddress.Parse(ip), port );
        private static List<TcpClient> allClients = new List<TcpClient>();
        private int msg_size = 1024;

        private bool sOn = false;

        private static int pad = 25;
        private string info = "[INFO]".PadLeft( pad, ' ' );
        private string error = "[ERROR]".PadLeft(pad, ' ');
        private string alert = "[ALERT]".PadLeft(pad, ' ');

        delegate void SetTextCallback( TextBox type, string text );

        public Strežnik(){
            InitializeComponent();
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
        }

        private void onClientDisconnect( TcpClient client, string cn ){
            allClients.Remove(client);
            setText( log, cn + " has disconnected." + info );
            removeText( connected, cn );
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
                byte[] buffer = new byte[msg_size];
                string read = "";
                try{
                    read = Encoding.UTF8.GetString( buffer, 0, ns.Read( buffer, 0, buffer.Length ) );
                    ns.Close();
                }catch{
                    if (!client.Client.Connected)
                        break;
                    setText( log, "Couldn't read data! Remote host disconnected!" + alert );
                }

                if( !string.IsNullOrEmpty( read ) && sOn )
                    setText( log, read + ( "[" + cn + "]" ).PadLeft( pad, ' ' ) );
            }

            onClientDisconnect( client, cn );
        }

        private async void serverToggle( bool on ){
            if( !on ){
                listener.Start();
                listener.BeginAcceptTcpClient( new AsyncCallback( callback ), null);
                sOn = !on;
            }else{
                foreach( TcpClient cl in allClients ){
                    NetworkStream cls = cl.GetStream();

                    string msg = "Hello!";

                    byte[] send = Encoding.UTF8.GetBytes(msg.ToCharArray(), 0, msg.Length);
                    cls.Write(send, 0, send.Length);
                    cls.Close();
                }
                sOn = !on;
                TcpClient stop = new TcpClient( ip, port );
                stop.GetStream().Close();
                stop.Close();
            }
        }

        //tukaj dobimo text z chat boxa kdaj uporabnik pritisne enter
        private void chat_KeyPress( object sender, KeyPressEventArgs e ){
            if( e.KeyChar == ( char )13 && !string.IsNullOrEmpty( ( sender as TextBox ).Text ) ){
                string txt = (sender as TextBox).Text;
                ( sender as TextBox ).Text = "";

                e.Handled = true;

                string ret = handleCommand( txt );

                txt += "[SERVER(YOU)]".PadLeft( pad, ' ' );
                if ( !string.IsNullOrEmpty( ret ) )
                    txt += "\r\n" + ret ;

                log.AppendText( txt + "\r\n" );
            }
        }

        //tukaj se preveri ali je uporabnik vnesel pravilen ukaz, in či je nekaj z njim naredimo
        private string handleCommand(string txt){
            string[] cmd = txt.Split( ' ' );
            string help = "".PadLeft( pad, ' ' );

            switch( cmd[0] ){
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
                           + "\r\nexit - quit the program (will first turn off server if server is running)" + help
                           + "\r\nhelp - displays all commands" + help
                           + "\r\nsize - change maximum size of incoming message" + help
                           + "\r\nstart - start the server" + help
                           + "\r\nstop - stop the server" + help
                           + "\r\ntoggle - toggle the server state" + help;
                case "size":
                    try{
                        int num = Int32.Parse( cmd[1] );

                        if ( num > 0 )
                            msg_size = num;
                        else
                            num = Int32.Parse( "" );
                    }
                    catch( Exception ){
                        return cmd[1] + " is not a valid number to be converted to a message size!" + error;
                    }

                    return "Changed message size to: " + cmd[1] + info;
                case "start":
                    if( sOn )
                        return "Server already running!" + alert;
                    else
                        serverToggle( sOn );

                    return "Starting server!" + info;
                case "stop":
                    if( !sOn )
                        return "Server already stopped!" + alert;
                    else
                        serverToggle( sOn );
                    
                    return "Stopping server!" + info;
                case "toggle":
                    return sOn ? handleCommand( "stop" ) : handleCommand( "start" );
                case "port":
                    if( sOn )
                        setText( log, handleCommand( "stop" ) );

                    try
                    {
                        int num = Int32.Parse( cmd[1] );

                        if( num < 65353 && num > 0 )
                            port = num;
                        else
                            num = Int32.Parse( "" );
                    }catch{
                        return cmd[1] + " is not a valid number to be converted to a port!" + error;
                    }

                    listener = new TcpListener( IPAddress.Parse( ip ), port );
                    return "Changed port to: " + cmd[1] + info;
                default:
                    return "Unknown command: \"" + cmd[0] + "\"! Try help to get all commands." + alert;
            }
        }
    }
}
