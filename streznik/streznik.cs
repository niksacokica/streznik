using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
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

        private TcpListener listener = new TcpListener(IPAddress.Parse(statsD["IP"]), Int32.Parse(statsD["PORT"]));
        private static List<TcpClient> allClients = new List<TcpClient>();
        private Dictionary<string, string> aliases = new Dictionary<string, string>();

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
                    if( !isConnected( cl.Client ) )
                        try{ onClientDisconnect( cl, cl.Client.RemoteEndPoint.ToString() ); }catch{}
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

        private void appendText( TextBox type, string txt ){
            if( type.InvokeRequired )
                this.Invoke( new SetTextCallback( appendText ), new object[] { type, txt } );
            else
                type.AppendText( txt + ( type.Name.Equals( "log" ) ? "\r\n\r\n" : "\r\n" ) );
        }

        private void setText( TextBox type, string txt ){
            if( type.InvokeRequired )
                this.Invoke( new SetTextCallback( setText ), new object[] { type, txt } );
            else
                type.Text = txt;
        }

        private void removeText( TextBox type, string txt ){
            if( connected.InvokeRequired )
                this.Invoke( new SetTextCallback( removeText ), new object[] { type, txt } );
            else{
                string[] tmp = connected.Text.Split( '\n' );
                foreach( string s in tmp ){
                    if( s.Replace( "\r", "" ).StartsWith( txt ) ){
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
            allClients.Add( client );
            aliases.Add( cn, "" );

            appendText( log, info + cn + " has connected." );
            appendText( connected, cn );

            statsD["CONNECTED CLIENTS"] = ( Int32.Parse( statsD["CONNECTED CLIENTS"] ) + 1 ).ToString();
            setStats( stats, "" );

            sendToClient( client, "SERVER", "aliases", "sc", JsonConvert.SerializeObject( aliases ) );
            sendToClients( "SERVER", "update online", "sc", connected.Text );
        }

        private void onClientConnected( TcpClient cl, NetworkStream ns, string cn ){
            while( sOn ){
                byte[] buffer = new byte[Int32.Parse( statsD["MESSAGE SIZE"] )];
                string read = "";
                try{
                    read = Encoding.UTF8.GetString( buffer, 0, ns.Read( buffer, 0, buffer.Length ) );
                }catch{}

                if( !string.IsNullOrEmpty( read ) ){
                    Dictionary<string, string> msg = JsonConvert.DeserializeObject<Dictionary<string, string>>( @read );

                    handleMessage( cl, msg, cn );
                }
            }
        }

        private void onClientDisconnect( TcpClient client, string cn ){
            allClients.Remove( client );
            aliases.Remove( cn );

            if( sOn )
                appendText( log, info + cn + " has disconnected." );
            removeText( connected, cn );

            statsD["CONNECTED CLIENTS"] = ( Math.Max( Int32.Parse( statsD["CONNECTED CLIENTS"] ) - 1, 0 ) ).ToString();
            setStats( stats, "" );

            sendToClients( "SERVER", "update online", "sc", connected.Text );
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
            appendText( log, info + ( on ? "Stopping server." : "Starting server." ) );

            if ( !on ){
                listener.Start();
                listener.BeginAcceptTcpClient( new AsyncCallback( callback ), null);
                sOn = !on;
            }
            else{
                foreach (TcpClient cl in allClients.ToList())
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
            appendText(log, msg);

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

                string json = JsonConvert.SerializeObject( forJson );

                byte[] send = Encoding.UTF8.GetBytes( json.ToCharArray(), 0, json.Length );
                cls.Write( send, 0, send.Length );
            }catch{
                appendText( log, "Unable to execute: " + cmd + ". Client not responding!" );
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

                    string name = "";
                    if( aliases.ContainsKey( cmd[2] ) ){
                        name = cmd[2];
                    }
                    else if( aliases.ContainsValue( cmd[2] ) ){
                        name = aliases.First( k => k.Value == cmd[2] ).Key;
                    }
                    else
                        return alert + "Couldn't find nickname/ip: \"" + cmd[2] + "\"!";

                    foreach( TcpClient cl in allClients.ToList() ){
                        if( name.Equals( cl.Client.RemoteEndPoint.ToString() ) ){
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
                           + "\r\ndisconnect [sc/c] [ip:port/nickname] [reason] - disconnects a user from the server"
                           + "\r\nexit - quit the program (will first turn off server if server is running)"
                           + "\r\nhelp - displays all commands"
                           + "\r\nmessage [ip:port/\"all\"/nickname] [message] - send a message to the client"
                           + "\r\nnick [ip:port/nickname] [new nickname] - change/give user nickname"
                           + "\r\nport [port] - changes the servers port to a new one (will stop server if its running)"
                           + "\r\nrestart - restarts the server"
                           + "\r\nstart - start the server"
                           + "\r\nstop - stop the serve"
                           + "\r\ntoggle - toggle the server state";
                case "message":
                    if( !sOn )
                        return alert + "Server is not on!";
                    else if( cmd.Length < 3 )
                        return alert + "Not enough arguments!";

                    string msg = string.Join( " ", cmd.Where( w => w != cmd[1] && w != cmd[0] ).ToArray() );

                    if( cmd[1].Equals( "all" ) ){
                        sendToClients( "SERVER", "", "ma", msg );
                        return info + "Sent a message \"" + msg + "\" to \"" + cmd[1] + "\".";
                    }
                    else{
                        string rec = "";
                        if( aliases.ContainsKey( cmd[1] ) ){
                            rec = cmd[2];
                        }
                        else if( aliases.ContainsValue( cmd[1] ) ){
                            rec = aliases.First( k => k.Value == cmd[1] ).Key;
                        }
                        else
                            return alert + "Couldn't find nickname/ip: \"" + cmd[1] + "\"!";

                        foreach( TcpClient cl in allClients.ToList()){
                            if( rec.Equals( cl.Client.RemoteEndPoint.ToString() ) ) {
                                sendToClient( cl, "SERVER", "", "m", msg );

                                return info + "Sent a message \"" + msg + "\" to \"" + cmd[1] + "\".";
                            }
                        }
                    }

                    return alert + "Unable to find \"" + cmd[1] + "\"!";
                case "nick":
                    if( !sOn )
                        return alert + "Server is not on!";
                    else if( cmd.Length < 3 )
                        return alert + "Not enough arguments!";

                    string key = "";
                    string nick = string.Join( "_", cmd.Where(w => w != cmd[0] && w != cmd[1] ).ToArray() );
                    if( nick.Equals( "SERVER" ) || nick.Equals( "STREŽNIK" ) || nick.Equals( "all" ) || nick.Equals( "vsi" ) )
                        return alert + "Couldn't set " + cmd[1] + " nickname to: " + nick + "!";

                    if( aliases.ContainsKey( cmd[1] ) ){
                        key = cmd[1];
                        aliases[key] = nick;
                    }
                    else if( aliases.ContainsValue( cmd[1] ) ){
                        key = aliases.First(k => k.Value == cmd[1]).Key;
                        aliases[key] = nick;
                    }
                    else
                        return alert + "Couldn't find nickname/ip: \"" + cmd[1] + "\"!";

                    string[] users = connected.Text.Split( '\n' );
                    foreach( string user in users )
                        if( user.Replace( "\r", "" ).StartsWith( key ) ){
                            users[Array.IndexOf( users, user )] = key + " (" + nick + ")";
                            setText( connected, string.Join( "\r\n", users ) );
                        }
                    
                    sendToClients( "SERVER", "update online", "sc", connected.Text );
                    sendToClients( "SERVER", "", "ma", "Changed/gave " + key + " nickname to: \"" + nick + "\".");
                    sendToClients( "SERVER", "aliases", "sc", JsonConvert.SerializeObject( aliases ) );

                    return info + "Changed/gave " + cmd[1] + " the \"" + nick + "\" nickname!";
                case "port":
                    if( sOn )
                        appendText( log, handleInput( "stop" ) );

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

                    appendText( log, handleInput( "stop" ) );
                    appendText( log, handleInput( "start" ) );

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

                    return info + "Server stopped.";
                case "toggle":
                    return sOn ? handleInput( "stop" ) : handleInput( "start" );
                default:
                    return alert + "Unknown command: \"" + cmd[0] + "\"! Try help to get all commands.";
            }
        }

        private void handleMessage( TcpClient origin, Dictionary<string, string> msg, string sender ){
            switch( msg["command"] ){
                case "message":
                    if( msg["recepient"].Equals( "SERVER" ) || msg["recepient"].Equals( "STREŽNIK" ) || msg["recepient"].Equals( statsD["IP"] + ":" + statsD["PORT"] ) )
                        appendText( log, "[" + sender + "]\r\n" + ( msg["type"].Equals( "mc" ) ? decrypt( msg["message"], msg["command"] + msg["type"] ) : msg["message"] ) );
                    else if( msg["recepient"].Equals( "all" ) || msg["recepient"].Equals( "vsi" ) ){
                        foreach( TcpClient cl in allClients.ToList() )
                            if( !sender.Equals( cl.Client.RemoteEndPoint.ToString() ) )
                                sendToClient( cl, sender, msg["command"], msg["type"], msg["message"] );

                        appendText( log, "[" + sender + "] -> [" + msg["recepient"] + "]\r\n" + ( msg["type"].Equals( "mca" ) ? decrypt( msg["message"], msg["command"] + msg["type"] ) : msg["message"] ) );
                    }else{
                        foreach( TcpClient cl in allClients.ToList() )
                            if( msg["recepient"].Equals( cl.Client.RemoteEndPoint.ToString() ) ){
                                sendToClient( cl, sender, msg["command"], msg["type"], msg["message"] );

                                appendText( log, "[" + sender + "] -> [" + msg["recepient"] + "]\r\n" + msg["message"] );
                                break;
                            }
                    }

                    break;
                case "nick":
                    aliases[sender] = msg["message"];
                    
                    string[] users = connected.Text.Split( '\n' );
                    foreach( string user in users )
                        if( user.Replace( "\r", "" ).StartsWith( sender ) ){
                            users[Array.IndexOf( users, user )] = users[Array.IndexOf(users, user)].Split( ' ' )[0] + " (" + msg["message"] + ")";
                            setText( connected, string.Join( "\r\n", users ) );
                        }
                    
                    appendText( log, info + sender + " set their nickname to: \"" + msg["message"] + "\".");
                    sendToClients( "SERVER", "update online", "sc", connected.Text );
                    sendToClients( sender, "", "ma", sender + " set their nickname to: \"" + msg["message"] + "\"." );
                    sendToClients( "SERVER", "aliases", "sc", JsonConvert.SerializeObject( aliases ) );

                    break;
                //za nalogu
                case "čas":
                    appendText( log, info + sender + " je prosil sem da mu povem trenutni čas, pa sem mu povedal: \"" + DateTime.Now + "\".");
                    sendToClient( origin, "SERVER", "", "m", "Trenutni čas je: " + DateTime.Now + "." );

                    break;
                case "dir":
                    appendText( log, info + sender + " je vprašal za delovni direktorij, pa sem poslal: \"" + Directory.GetCurrentDirectory().ToString() + "\"." );
                    sendToClient( origin, "SERVER", "", "m", "Delovni direktorij je: " + Directory.GetCurrentDirectory().ToString() );

                    break;
                case "info":
                    string sys = "Ime sistema je: \"" + Environment.MachineName + "\" in OS je: \"" + Environment.OSVersion.VersionString.ToString() + "\".";
                    appendText( log, info + sender + " je vprašal za sistemske informacije, pa sem poslal: " + sys);
                    sendToClient( origin, "SERVER", "", "m", sys );

                    break;
                case "pozdravi":
                    appendText( log, info + sender + " je prosil sem da ga pozdravim, pa sem ga pozdravil.");
                    sendToClient( origin, "SERVER", "", "m", "Pozdravljen " + sender + "." );

                    break;
                case "šah":
                    string[] deli = msg["message"].Split( ' ' );
                    string[] polja = new string[8];
                    string fen = "\r\n_ _ _ _ _ _ _ _ \r\n";

                    polja = deli[0].Split( '/' );

                    for( int i=0; i<polja.Length; i++ ){
                        for( int j=0; j<polja[i].Length; j++ )
                            if( Char.IsDigit( polja[i][j] ) )
                                fen += new StringBuilder().Insert( 0, "| ", polja[i][j] - '0').ToString();
                            else
                                fen += "|" + polja[i][j];
                        fen += "|\r\n";
                    }

                    fen += "¯ ¯ ¯ ¯ ¯ ¯ ¯ ¯ \r\nNa vrsti je: " + ( deli[1].Equals( "w" ) ? "beli" : "črni" ) + "\r\n";

                    bool rokada = deli[2].Contains("K") || deli[2].Contains("Q") || deli[2].Contains("k") || deli[2].Contains("q");
                    fen += "\r\nMožnosti rokade:" + ( rokada ? "\r\n" + ( deli[2].Contains( "K" ) ? "Beli, kraljeva stran\r\n" : "" ) + ( deli[2].Contains( "Q" ) ? "Beli, damina stran\r\n" : "" ) + ( deli[2].Contains( "k" ) ? "Črni, kraljeva stran\r\n" : "" ) + ( deli[2].Contains( "q" ) ? "Črni, damina stran\r\n" : "" ) : " Ni možnosti!\r\n");

                    fen += "\r\nMožnost en passant: " + ( deli[3].Equals( "-" ) ? "noben" : deli[3] ) + "\r\n";

                    fen += "\r\nŠtevilo polpotez: " + deli[4] + "\r\n";

                    fen += "\r\n Številka trenutne poteze: " + deli[5];

                    appendText( log, info + sender + " je prosil da mu lepo izpišem FEN stanje: \"" + msg["message"] + "\". Pa sem mu poslal:" + fen );
                    sendToClient( origin, "SERVER", "", "m", "Lepo izpisano FEN stanje: \"" + msg["message"] + "\", zgleda ovak:" + fen );

                    break;
                case "šifriraj":
                    string emsg = encrypt( msg["message"], "šifrirano" + "c" );
                    appendText( log, info + sender + " je prosil da mu šifriram sporočilo: \"" + msg["message"] + "\", pa sem to naredil: \"" + emsg + "\".");
                    sendToClient( origin, "SERVER", "šifrirano", "c", emsg );

                    break;
                default:
                    break;
            }
        }

        private string encrypt( string txt, string key ){
            byte[] Bkey = new byte[16];
            for( int i = 0; i < 16; i += 2 ){
                byte[] B = BitConverter.GetBytes( key[i % key.Length] );
                Array.Copy( B, 0, Bkey, i, 2 );
            }

            TripleDESCryptoServiceProvider edes = new TripleDESCryptoServiceProvider();
            edes.Key = Bkey;
            edes.Mode = CipherMode.ECB;
            edes.Padding = PaddingMode.PKCS7;

            ICryptoTransform encrypt = edes.CreateEncryptor();
            byte[] byteTXT = UTF8Encoding.UTF8.GetBytes( txt );
            byte[] result = encrypt.TransformFinalBlock( byteTXT, 0 , byteTXT.Length );

            edes.Clear();
            return Convert.ToBase64String( result, 0, result.Length );
        }

        private string decrypt( string msg, string key){
            byte[] Bkey = new byte[16];
            for( int i = 0; i < 16; i += 2 ){
                byte[] B = BitConverter.GetBytes( key[i % key.Length] );
                Array.Copy( B, 0, Bkey, i, 2 );
            }

            TripleDESCryptoServiceProvider ddes = new TripleDESCryptoServiceProvider();
            ddes.Key = Bkey;
            ddes.Mode = CipherMode.ECB;
            ddes.Padding = PaddingMode.PKCS7;

            ICryptoTransform decrypt = ddes.CreateDecryptor();
            byte[] byteTXT = Convert.FromBase64String( msg );
            byte[] result = decrypt.TransformFinalBlock( byteTXT, 0, byteTXT.Length );
           
            ddes.Clear();
            return Encoding.UTF8.GetString( result, 0, result.Length );
        }
    }
}
