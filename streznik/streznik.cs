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
        private static Dictionary<string, string> statsD = new Dictionary<string, string>(){ //dictionary ki se uporablja za statistiko strežnika
            { "IP", "127.0.0.1" },
            { "PORT", "1507" },
            { "MESSAGE SIZE", "1024" },
            { "SERVER STATUS", "OFF" },
            { "CONNECTED CLIENTS", "0" }
        };

        private TcpListener listener = new TcpListener( IPAddress.Parse( statsD["IP"] ), Int32.Parse( statsD["PORT"] ) );
        private static List<TcpClient> allClients = new List<TcpClient>(); //list ki vsebuje vse povezane uporabnike
        private Dictionary<string, string> aliases = new Dictionary<string, string>(); //dictionary ki vsebuje vse vzdevke od uporabnikov

        private bool sOn = false; //bool ki kotrolira ali je strežnik vkloljen ali ne

        //besedila ki se uporabljajo za različne vrsti sporočila
        private string info = "[INFO]\r\n";
        private string error = "[ERROR]\r\n";
        private string alert = "[ALERT]\r\n";

        delegate void SetTextCallback( TextBox type, string text ); //text callback ki se uporablja za invoke funckije

        //spremenljivke za igro ugani besedo
        private Dictionary<string, string> besede = new Dictionary<string, string>(){
            { "angel", "a__e_" },
            { "oko", "_k_" },
            { "ognjemet", "o___e_e_" },
            { "buče", "_u_e" },
            { "dojenček", "_o_e__e_" },
            { "cvet", "__e_" },
            { "brada", "__a_a" },
            { "mavrica", "_a__i_a" },
            { "žirafa", "_i_a_a" },
            { "očala", "o_a_a" }
        };
        private Dictionary<string, int> statsG = new Dictionary<string, int>();
        private string trenutna = "";
        private bool gameOn = false;

        //glavna fukncija katera pokrene cel program
        public Strežnik(){
            InitializeComponent();

            setStats( stats, "" );

            //timer ki vsako sekundo preveri ali se je slučajno kateri odjemalec odpspojil na način da ni poslal sporočila da se je odspojil (interneta je zmanjkalo,...)
            Timer cc = new Timer();
            cc.Tick += delegate {
                foreach( TcpClient cl in allClients.ToList() ){
                    if( !isConnected( cl.Client ) )
                        onClientDisconnect( cl, cl.Client.RemoteEndPoint.ToString() );
                }
            };
            cc.Interval = 1000;
            cc.Start();
        }

        //funkcija ki zamenja text v textbox ki jo kliče z ukazi ki so v statsD
        //uporablja se zato ker rabimo invoke zato ker različni thready nemorejo klicat textbox
        private void setStats( TextBox type, string txt ){
            if( type.InvokeRequired )
                this.Invoke( new SetTextCallback( setStats ), new object[] { type, txt });
            else{
                type.Text = "";
                foreach( KeyValuePair<string, string> stat in statsD )
                    type.AppendText( stat.Key + ":" + stat.Value.PadLeft( 15, ' ' ) + "\r\n" );
            }
        }

        //funkcija ki doda text v textbox ki ga kliče
        //uporablja se zato ker rabimo invoke zato ker različni thready nemorejo klicat textbox
        private void appendText( TextBox type, string txt ){
            if( type.InvokeRequired )
                this.Invoke( new SetTextCallback( appendText ), new object[] { type, txt } );
            else
                type.AppendText( txt + ( type.Name.Equals( "log" ) ? "\r\n\r\n" : "\r\n" ) );
        }

        //funkcija ki zamenja text v textbox ki ga kliče
        //uporablja se zato ker rabimo invoke zato ker različni thready nemorejo klicat textbox
        private void setText( TextBox type, string txt ){
            if( type.InvokeRequired )
                this.Invoke( new SetTextCallback( setText ), new object[] { type, txt } );
            else
                type.Text = txt;
        }

        //funkcija ki umakne ip iz "connected" texbox kdaj se negdo disconnecta
        //uporablja se zato ker rabimo invoke zato ker različni thready nemorejo klicat textbox
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

        //fukncija ki preveri če je uporabnik še vedno povezan
        public static bool isConnected( Socket socket ){
            try{
                return !( socket.Poll( 1, SelectMode.SelectRead ) && socket.Available == 0 );
            }catch{
                return false;
            }
        }


        //funkcija ki se kliče kdaj se uporabnik poveže
        private void onClientConnect( TcpClient client, string cn ){
            sendToClients("SERVER", "", "ma", cn + " se je povezal na strežnik!"); //sporočilo da se je nov odjemalec povezal
            aliases.Add( cn, "" );

            allClients.Add( client );
            appendText( log, info + cn + " has connected." );
            appendText( connected, cn );
            
            statsD["CONNECTED CLIENTS"] = ( Int32.Parse( statsD["CONNECTED CLIENTS"] ) + 1 ).ToString();
            setStats( stats, "" );

            if( gameOn )
                statsG[cn] = 0;
            sendToClient( client, "SERVER", "aliases", "sc", JsonConvert.SerializeObject( aliases ) );
            sendToClient( client, "SERVER", "gameStatus", "sc", gameOn.ToString() );
        }

        //funkcija ki se kliče kdaj je uporabnik povezan
        private void onClientConnected( TcpClient cl, NetworkStream ns, string cn ){
            sendToClients( "SERVER", "update online", "sc", connected.Text );

            while( sOn && cl.Connected ){
                byte[] buffer = new byte[Int32.Parse( statsD["MESSAGE SIZE"] )];
                string read = "";
                try{
                    read = Encoding.UTF8.GetString( buffer, 0, ns.Read( buffer, 0, buffer.Length ) );
                }catch{
                    if( cl.Connected )
                        appendText( log, error + "Couldn't read message!" );
                }

                 if( !string.IsNullOrEmpty( read ) ){
                    Dictionary<string, string> msg = JsonConvert.DeserializeObject<Dictionary<string, string>>( @read ); //sporočilo prejeto od uporabnika, ki se šalje kot json, se nazaj da v obliko dictioanry
                    msg["message"] = decrypt( msg["message"], msg["command"] + msg["type"] + statsD["IP"] + ":" + statsD["PORT"] );

                    handleMessage( cl, msg, cn );
                }
            }
        }

        //funkcija ki se kliče kdaj se uporabnik odspoji
        private void onClientDisconnect( TcpClient client, string cn ){
            allClients.Remove( client );

            if( sOn )
                appendText( log, info + cn + ( !string.IsNullOrEmpty( aliases[cn] ) ? " (" + aliases[cn] + ")" : "" ) + " has disconnected." );
            removeText( connected, cn );
            aliases.Remove( cn );

            statsD["CONNECTED CLIENTS"] = ( Math.Max( Int32.Parse( statsD["CONNECTED CLIENTS"] ) - 1, 0 ) ).ToString();
            statsG.Remove( cn );
            setStats( stats, "" );

            sendToClients( "SERVER", "update online", "sc", connected.Text );
        }

        //funkcija ki nam omogoča da se spoje različni uporabniki skupa
        private void callback( IAsyncResult iar ){
            TcpClient client = listener.EndAcceptTcpClient( iar );
            if( !sOn ){
                listener.Stop();
                return;
            }

            listener.BeginAcceptTcpClient( new AsyncCallback( callback ), null ); //po uspešni povezavi enega uporabnika, začne slišati uza novega
            NetworkStream ns = client.GetStream();
            string cn = client.Client.RemoteEndPoint.ToString();

            onClientConnect( client, cn );
            onClientConnected( client, ns, cn );
            onClientDisconnect( client, cn );
        }

        //funkcija ki menja stanje serverja ( on/off )
        private void serverToggle( bool on ){
            appendText( log, info + ( on ? "Stopping server." : "Starting server." ) );

            if ( !on ){
                listener.Start();
                listener.BeginAcceptTcpClient( new AsyncCallback( callback ), null);
                sOn = !on;
            }
            else{
                foreach( TcpClient cl in allClients.ToList() ) //kdaj bi se strežnik izklopil, pošlje to informacijo vsim odjemalcom trenutno povezanim
                    sendToClient( cl, "SERVER", "disconnect", "sc", "" );

                sOn = !on;
                //poveže se na sebe zato ker listener še vedno čaka eno povezavo
                TcpClient stop = new TcpClient( statsD["IP"], Int32.Parse( statsD["PORT"] ) );
                stop.GetStream().Close();
                stop.Close();

                gameOn = !gameOn;
            }

            statsD["SERVER STATUS"] = sOn ? "ON" : "OFF" ;
            setStats( stats, "" );
        }

        //funkcija ki pošilja sporočilo vsem uporabnikima trenuto povazanim
        private void sendToClients( string who, string cmd, string type, string msg ){
            foreach( TcpClient cl in allClients.ToList() )
                sendToClient( cl, who, cmd, type, msg );
        }

        //funkcija ki pošlje sporočilo določenem uporabniku
        private void sendToClient( TcpClient cl, string who, string cmd, string type, string msg ){
            try{
                NetworkStream cls = cl.GetStream();

                //sporočilo se da v dictionary zato ker odjemalcu ga šaljemo kot json
                Dictionary<string, string> forJson = new Dictionary<string, string>(){
                    { "sender", who },
                    { "command", cmd },
                    { "type", type },
                    { "message", encrypt( msg, cmd + type + statsD["IP"] + ":" + statsD["PORT"] ) }
                };
                string json = JsonConvert.SerializeObject( forJson );

                byte[] send = Encoding.UTF8.GetBytes( json.ToCharArray(), 0, json.Length );
                cls.Write( send, 0, send.Length );
            }catch{
                appendText( log, "Unable to execute: " + cmd + " - " + type + " - " + msg + ". Client not responding!" );
            }
        }

        //tukaj dobimo besedilo z chat boxa kdaj uporabnik pritisne enter
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

        //tukaj se preveri ali je strežnikov uporabnik vnesel pravilen ukaz, in či je nekaj z njim naredimo
        private string handleInput( string txt ){
            string[] cmd = txt.Split( ' ' );

            switch( cmd[0] ){
                case "disconnect": //ukaz ki odklopi enega izbranega uporabika
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
                case "exit": //ukaz ki iklopi program, ampak prvo preveri če je strežnik "on" in či je ga izklopi
                    Timer cls = new Timer();
                    cls.Tick += delegate{
                        this.Close();
                    };
                    cls.Interval = 1000;
                    cls.Start();

                    return sOn ? handleInput( "stop" ) : "";
                case "help": //ukaz ki nam prikaže pomoč
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
                case "message": //ukaz zs slanje sporočila eniem odjemalcu ali vsim povezanim
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

                        foreach( TcpClient cl in allClients.ToList() ){
                            if( rec.Equals( cl.Client.RemoteEndPoint.ToString() ) ) {
                                sendToClient( cl, "SERVER", "", "m", msg );

                                return info + "Sent a message \"" + msg + "\" to \"" + cmd[1] + "\".";
                            }
                        }
                    }

                    return alert + "Unable to find \"" + cmd[1] + "\"!";
                case "nick": //ukaz ki nam dovoli da odjemalcu spremenimo ali dodamo vzdevek
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
                case "port": //ukaz ki nam dovoli spremembo porta, ampak prvo izklopi strežnik če je "on"
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
                case "restart": //ukaz ki ponovno zažene strežnik
                    if( !sOn )
                        return alert + "Server is not on!";

                    appendText( log, handleInput( "stop" ) );
                    appendText( log, handleInput( "start" ) );

                    return info + "Server has been restarted.";
                case "start": //ukaz ki zažene strežnik
                    if( sOn )
                        return alert + "Server already running!";
                    else
                        serverToggle( sOn );

                    return info + "Server started.";
                case "stop": //ukaz ki ustavi strežnik
                    if( !sOn )
                        return alert + "Server already stopped!";
                    else
                        serverToggle( sOn );

                    return info + "Server stopped.";
                case "toggle": //ukaz ki zamenja stanje strežnika
                    return sOn ? handleInput( "stop" ) : handleInput( "start" );
                default:
                    return alert + "Unknown command: \"" + cmd[0] + "\"! Try help to get all commands.";
            }
        }

        //tukaj obdelavamo z besedilima ki smo jih prejeli od uporabnika
        private void handleMessage( TcpClient origin, Dictionary<string, string> msg, string sender ){
            switch( msg["command"] ){
                case "msg": //obdelava sporočilo tipa message in ga samo prikaže če je za strežnik, pošlje našrej vsem, ali samo določenem odjemalcu
                    if( msg["recepient"].Equals( "SERVER" ) || msg["recepient"].Equals( "STREŽNIK" ) || msg["recepient"].Equals( statsD["IP"] + ":" + statsD["PORT"] ) )
                        appendText( log, "[" + sender + "]" + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")\r\n" : "\r\n") + msg["message"] );
                    else if( msg["recepient"].Equals( "all" ) ){
                        foreach( TcpClient cl in allClients.ToList() )
                            if( !sender.Equals( cl.Client.RemoteEndPoint.ToString() ) )
                                sendToClient( cl, sender, msg["command"], msg["type"], msg["message"] );

                        appendText( log, "[" + sender + "]" + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " -> [all]" + "\r\n" + msg["message"] );
                    }else{
                        foreach( TcpClient cl in allClients.ToList() )
                            if( msg["recepient"].Equals( cl.Client.RemoteEndPoint.ToString() ) ){
                                sendToClient( cl, sender, msg["command"], msg["type"], msg["message"] );

                                appendText( log, "[" + sender + "]" + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " -> [" + msg["recepient"] + "]" + ( !string.IsNullOrEmpty( aliases[msg["recepient"]] ) ? " (" + aliases[msg["recepient"]] + ")\r\n" : "\r\n" ) + msg["message"] );
                                break;
                            }
                    }

                    break;
                case "nick": //obdelava sporočilo tipa nick in potem spremeni odjemalcu vzdevek
                    aliases[sender] = msg["message"];
                    
                    string[] users = connected.Text.Split( '\n' );
                    foreach( string user in users )
                        if( user.Replace( "\r", "" ).StartsWith( sender ) ){
                            users[Array.IndexOf( users, user )] = users[Array.IndexOf(users, user)].Split( ' ' )[0] + " (" + msg["message"] + ")";
                            setText( connected, string.Join( "\r\n", users ) );
                        }
                    
                    sendToClients( "SERVER", "update online", "sc", connected.Text );
                    sendToClients( "SERVER", "", "ma", sender + " set their nickname to: \"" + msg["message"] + "\"." );
                    sendToClients( "SERVER", "aliases", "sc", JsonConvert.SerializeObject( aliases ) );

                    break;
                //za nalogu
                case "čas": //obdelava sporočilo tipa čas in vrne odjemalcu trenutni čas
                    appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je prosil sem da mu povem trenutni čas, pa sem mu povedal: \"" + DateTime.Now + "\".");
                    sendToClient( origin, "SERVER", "", "m", "Trenutni čas je: " + DateTime.Now + "." );

                    break;
                case "dir": //obdelava sporočilo tipa dir in vrne odjemalcu trenutni delovni direktorij
                    appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je vprašal za delovni direktorij, pa sem poslal: \"" + Directory.GetCurrentDirectory().ToString() + "\"." );
                    sendToClient( origin, "SERVER", "", "m", "Delovni direktorij je: " + Directory.GetCurrentDirectory().ToString() );

                    break;
                case "info": //obdelava sporočilo tipa info in vrne odjemalcu sistemske informacije
                    string sys = "Ime sistema je: \"" + Environment.MachineName + "\" in OS je: \"" + Environment.OSVersion.VersionString.ToString() + "\".";
                    appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je vprašal za sistemske informacije, pa sem poslal: " + sys);
                    sendToClient( origin, "SERVER", "", "m", sys );

                    break;
                case "ponovi": //obdelava sporočilo tipa ponovi in vrne odjemalcu nazaj njegovo sporočilo
                    appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je vprašal da mu ponovim nazaj sporočilo: \"" + msg["message"] + "\". Pa sem tudi to naredil." );
                    sendToClient( origin, "SERVER", "", "m", msg["message"] );

                    break;
                case "pozdravi": //obdelava sporočilo tipa pozdravi in pozdravi odjemalca
                    appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je prosil sem da ga pozdravim, pa sem ga pozdravil.");
                    sendToClient( origin, "SERVER", "", "m", "Pozdravljen " + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + "." );

                    break;
                case "šah": //obdelava sporočilo tipa šah in lepo izpiše odjemalcu fen notaciju
                    string[] deli = msg["message"].Split( ' ' );
                    string[] polja;
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

                    appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je prosil da mu lepo izpišem FEN stanje: \"" + msg["message"] + "\". Pa sem mu poslal:" + fen );
                    sendToClient( origin, "SERVER", "", "m", "Lepo izpisano FEN stanje: \"" + msg["message"] + "\", zgleda ovak:" + fen );

                    break;
                case "šifriraj": //obdelava sporočilo tipa šifriraj in pošlje nazaj odjemalcu šifrirano sporočilo
                    string emsg = encrypt( msg["message"], "šifrirano" + "c" );
                    appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je prosil da mu šifriram sporočilo: \"" + msg["message"] + "\", pa sem to naredil: \"" + emsg + "\".");
                    sendToClient( origin, "SERVER", "šifrirano", "c", emsg );

                    break;
                //za drugo nalogu
                case "startGame":
                case "stopGame": //obdelava sporočilo tipa startGame/stopGame ki ali začne ali konča igro
                    gameOn = !gameOn;
                    sendToClients( "SERVER", "gameStatus", "sc", gameOn.ToString() );

                    if( msg["command"].Equals( "startGame" ) ){
                        appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je začel novo igro." );
                        sendToClients( "SERVER", "", "ma", sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je začel novo igro." );
                        novaBeseda();
                    }
                    else{
                        appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je končal igro.");
                        sendToClients( "SERVER", "", "ma", sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je končal igro." );
                        trenutna = "";
                        
                        string stanje = "Končni rezultati igre:\r\n" + string.Join( "\r\n", statsG.Select( kvp => kvp.Key + ( !string.IsNullOrEmpty( aliases[kvp.Key] ) ? " (" + aliases[kvp.Key] + "): " : ": " ) + kvp.Value ) );
                        appendText( log, info + stanje );
                        sendToClients( "SERVER", "", "ma", stanje );
                    }


                    break;
                case "zadeni": //obdelava sporočilo tipa zadeni ki preveri če je uporabnik zadenil besedilo
                    if( msg["message"].Equals( trenutna ) ){
                        sendToClients( "SERVER", "", "ma", sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? "(" + aliases[sender] + ")" : "" ) + " je uganul zagonetno besedo: \"" + msg["message"] + "\"." );
                        appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je uganul zagonetno besedo (" + msg["message"] + " == " + trenutna + ")!" );
                        
                        statsG[sender]++;
                        statsG = ( from val in statsG orderby val.Value descending select val ).ToDictionary( key => key.Key, val => val.Value );

                        novaBeseda();
                        break;
                    }
                    
                    appendText( log, info + sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " ni uganul zagonetno besedo (" + msg["message"] + " != " + trenutna + ")!" );
                    sendToClients( "SERVER", "", "ma", sender + ( !string.IsNullOrEmpty( aliases[sender] ) ? " (" + aliases[sender] + ")" : "" ) + " je poskusil uganit zagonetno besedo: \"" + msg["message"] + "\", pa mu ni uspelo." );

                    break;
                default:
                    break;
            }
        }

        private void novaBeseda(){ //funkcija ki poda novo besedo za igro
            Random rnd = new Random();
            trenutna = besede.ElementAt( rnd.Next( 0, besede.Count ) ).Key;

            if( statsG.Count == 0 )
                foreach( TcpClient cl in allClients.ToList() )
                    statsG[cl.Client.RemoteEndPoint.ToString()] = 0;

            string stanje = "Trenutni rezultati igre:\r\n" + string.Join( "\r\n", statsG.Select( kvp => kvp.Key + ( !string.IsNullOrEmpty( aliases[kvp.Key] ) ? " (" + aliases[kvp.Key] + "): " : ": " ) + kvp.Value ) );

            appendText( log, info + "Nova beseda je izbrana \"" + trenutna + "\", namig je: \"" + besede[trenutna] + "\"." );
            sendToClients( "SERVER", "", "ma", "Nova beseda je izbrana, namig: \"" + besede[trenutna] + "\"." );

            appendText( log, info + stanje );
            sendToClients( "SERVER", "", "ma", stanje );
        }

        //funkcija ki šifrira besedilo z ključem ki ga pošljemo
        private string encrypt( string txt, string key ){
            byte[] Bkey = new byte[16];
            for( int i = 0; i < 16; i += 2 ){
                byte[] B = BitConverter.GetBytes( key[i % key.Length] );
                Array.Copy( B, 0, Bkey, i, 2 );
            }

            TripleDESCryptoServiceProvider edes = new TripleDESCryptoServiceProvider{
                Key = Bkey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform encrypt = edes.CreateEncryptor();
            byte[] byteTXT = UTF8Encoding.UTF8.GetBytes( txt );
            byte[] result = encrypt.TransformFinalBlock( byteTXT, 0 , byteTXT.Length );

            edes.Clear();
            return Convert.ToBase64String( result, 0, result.Length );
        }

        //funkcija ki dešefrira besedilo
        private string decrypt( string msg, string key ){
            byte[] Bkey = new byte[16];
            for( int i = 0; i < 16; i += 2 ){
                byte[] B = BitConverter.GetBytes( key[i % key.Length] );
                Array.Copy( B, 0, Bkey, i, 2 );
            }

            TripleDESCryptoServiceProvider ddes = new TripleDESCryptoServiceProvider{
                Key = Bkey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform decrypt = ddes.CreateDecryptor();
            byte[] byteTXT = Convert.FromBase64String( msg );
            byte[] result = decrypt.TransformFinalBlock( byteTXT, 0, byteTXT.Length );
           
            ddes.Clear();
            return Encoding.UTF8.GetString( result, 0, result.Length );
        }
    }
}
