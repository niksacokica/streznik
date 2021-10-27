using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace streznik{
    public partial class Strežnik : Form{
        Timer timer = new Timer();
        private const string ip = "127.0.0.1";
        private static int port = 1507;
        private TcpListener listener = new TcpListener( IPAddress.Parse( ip ), port);

        public Strežnik(){
            InitializeComponent();

            timer.Tick += new EventHandler(Tick);
            timer.Interval = 10;
        }

        //timer ki se kliče vsakih 0.01s
        private void Tick( object source, EventArgs e ){
            using( TcpClient client = listener.AcceptTcpClient() );
        }

        //tukaj dobimo text z chat boxa kdaj uporabnik pritisne enter
        private void chat_KeyPress( object sender, KeyPressEventArgs e ){
            if( e.KeyChar == ( char )13 && !string.IsNullOrEmpty( ( sender as TextBox ).Text ) ){
                string txt = (sender as TextBox).Text;
                ( sender as TextBox ).Text = "";

                e.Handled = true;

                string ret = handleCommand( txt );

                txt += "       [USER]";
                if ( !string.IsNullOrEmpty( ret ) )
                    txt += "\r\n" + ret ;

                log.AppendText( txt + "\r\n" );
            }
        }

        //tukaj se preveri ali je uporabnik vnesel pravilen ukaz, in či je nekaj z njim naredimo
        private string handleCommand(string txt){
            string[] cmd = txt.Split( ' ' );
            string hlpSpc = "                     ";
            string infoSpc = "           ";
            string str = "";

            switch ( cmd[0] ){
                case "help":
                    return "Available commands are:" + infoSpc + "[INFO]\r\nhelp - shows help" + hlpSpc
                           + "\r\nstart - start the server" + hlpSpc
                           + "\r\nstop - stop the server" + hlpSpc
                           + "\r\ntoggle - toggle the server state" + hlpSpc
                           + "\r\nexit - quit the program (will first turn off server if server is running)" + hlpSpc;
                case "start":
                    timer.Start();

                    return "Starting server!" + infoSpc + "[INFO]";
                case "stop":
                    timer.Stop();
                    
                    return "Stopping server!" + infoSpc + "[INFO]";
                case "toggle":
                    if( timer.Enabled )
                        str = handleCommand( "stop" );
                    else
                         str = handleCommand( "start" );

                    return str;
                case "port":
                    try{
                        port = Int32.Parse( cmd[1] );
                    }
                    catch( Exception ){
                        return cmd[1] + " is not a valid number to be converted to a port!" + "     [ERROR]";
                    }

                    return "Changed port to: " + cmd[1] + infoSpc + "[INFO]";
                case "exit":
                    if( timer.Enabled )
                        str = handleCommand("stop");

                    Timer cls = new Timer();
                    cls.Tick += delegate{
                        this.Close();
                    };
                    cls.Interval = 1000;
                    cls.Start();

                    return str;
                default:
                    return "Unknown command: \"" + cmd[0] + "\"! Try help to get all commands.       [ALERT]";
            }
        }
    }
}
