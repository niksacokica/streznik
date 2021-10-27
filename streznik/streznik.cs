using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace streznik{
    public partial class Strežnik : Form{
        private const string ip = "127.0.0.1";
        private static int port = 1507;
        private TcpListener listener = new TcpListener( IPAddress.Parse( ip ), port );
        private int msg_size = 1024;

        private bool sOn = false;

        private static int pad = 25;
        private string info = "[INFO]".PadLeft( pad, ' ' );

        delegate void SetTextCallback( string text );

        public Strežnik(){
            InitializeComponent();

            //Thread SocketThread = new Thread( ServerOn );
            //SocketThread.Start();
        }

        private void setText( string txt ){
            if( log.InvokeRequired ){
                SetTextCallback stc = new SetTextCallback( setText );
                this.Invoke( stc, new object[] { txt } );
            }
            else
                log.AppendText( txt + "\r\n" );
        }

        public static bool IsConnected( Socket socket ){
            try{
                return !( socket.Poll( 1, SelectMode.SelectRead ) && socket.Available == 0 );
            }catch{
                return false;
            }
        }

        private void closingInvoker( string dummy ){
            if( InvokeRequired ){
                this.Invoke( new Action<string>( closingInvoker ), new object[] { dummy } );
                return;
            }
            listener.Stop();
        }

        private void ServerOn(){
            while( true ){
                while( sOn ){
                    TcpClient client = listener.AcceptTcpClient();
                    NetworkStream ns = client.GetStream();

                    string cn = client.Client.RemoteEndPoint.ToString();

                    setText( cn + " has connected." + info );

                    while( sOn ){
                        if( !IsConnected(client.Client))
                            break;

                        byte[] buffer = new byte[msg_size];
                        string read = Encoding.UTF8.GetString( buffer, 0, ns.Read( buffer, 0, buffer.Length ) );

                        if( !string.IsNullOrEmpty( read ) )
                            setText( read + ( "[" + cn + "]" ).PadLeft( pad, ' ' ) );
                    }

                    setText( cn + " has disconnected." + info );
                }
            }
        }

        private void serverToggle( bool on ){
            if( !on ){
                listener.Start();
                sOn = !on;
            }
            else{
                sOn = !on;
                closingInvoker( "" );
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
            string error = "[ERROR]".PadLeft( pad, ' ' );
            string alert = "[ALERT]".PadLeft( pad, ' ' );

            switch ( cmd[0] ){
                case "exit":
                    System.Windows.Forms.Timer cls = new System.Windows.Forms.Timer();
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
                        msg_size = Int32.Parse( cmd[1] );
                    }catch( Exception ){
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
                    try{
                        //65353
                        port = Int32.Parse( cmd[1] );
                    }catch{
                        return cmd[1] + " is not a valid number to be converted to a port!" + error;
                    }

                    return "Changed port to: " + cmd[1] + info;
                default:
                    return "Unknown command: \"" + cmd[0] + "\"! Try help to get all commands." + alert;
            }
        }
    }
}
