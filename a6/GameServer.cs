using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Text.RegularExpressions;
using a6;

namespace TCPIPServer
{
    internal class GameServer
    {
        // To hold the details of a session
        public struct SessionVariables
        {
            public string sessionId;
            public string scrambledString;
            public string[] wordsList;
            public int numOfWords;
        }

        public static List<SessionVariables> playerSessions = new List<SessionVariables>(); // List of active sessions

        /* Constants */
        const int kMaxMessageLength = 2000;
        const int port = 63416;
        const string ipv4Address = "10.144.109.1";
        volatile bool running = true; // Running flag to control server status

        /*
        *  Method  : StartServer()
        *  Summary : Initialize the server and start listening for client requests.
        *  Params  : None.
        *  Return  : None.
        */
        internal void StartServer()
        {
            TcpListener server = null;

            try
            {
                // Initialize IP address
                IPAddress ipAddress = IPAddress.Parse(ipv4Address);

                // Establish endpoint of connection (socket)
                server = new TcpListener(ipAddress, port);
                server.Start();

                // Task to gracefully shut down
                Action shutDownWorker = () => shutDownServer(server); // Create shutdown task
                Task shutDownTask = Task.Factory.StartNew(shutDownWorker);

                // Enter listening loop
                while (running)
                {
                    // Wait for a connection
                    //Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();

                    // Connection occurred; fire off a task to handle it
                    //Console.WriteLine("Connection happened!");
                    Action<object> gameWorker = GuessingGame;
                    Task gameTask = Task.Factory.StartNew(gameWorker, client);
                    //Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine("Error: " + e + e.Message);
            }
            finally
            {
                server?.Stop();
            }
        }

        /*
        *  Method  : GuessingGame()
        *  Summary : Handle client requests and provide appropriate response based on game logic.
        *  Params  : 
        *     object o = the TcpClient object to communicate with client.
        *  Return  :  
        *     None.
        */
        public void GuessingGame(object o)
        {
            /* Buffer for reading request message */
            TcpClient client = (TcpClient)o;
            NetworkStream stream = client.GetStream();
            byte[] request = new byte[kMaxMessageLength];
            string message = string.Empty;
            int i;

            try
            {
                /* Read request and handle it */
                while (stream.DataAvailable && (i = stream.Read(request, 0, request.Length)) != 0)
                {
                    message = Encoding.ASCII.GetString(request, 0, i);
                    //Console.WriteLine("Received: " + message);

                    Regex startGame = new Regex(@"^CreatePlayerSession$");
                    Regex wordGuess = new Regex(@"^MakeGuess\|\S{1,30}\|\S{36}$");
                    Regex endGame = new Regex(@"^EndPlayerSession\|.{36}$");

                    /* Call appropriate method based on request */
                    if (startGame.IsMatch(message))
                    {
                        message = CreateSession();
                    }
                    else if (wordGuess.IsMatch(message))
                    {
                        message = TakeGuess(message);
                    }
                    else if (endGame.IsMatch(message))
                    {
                        message = EndSession(message);
                    }
                    else // Request is invalid
                    {
                        message = "BadRequest";
                    }

                    /* Send response back to client */
                    byte[] response = Encoding.ASCII.GetBytes(message);
                    stream.Write(response, 0, response.Length);
                    //Console.WriteLine("Sent: " + message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e + e.Message);
            }
            finally
            {
                if (client.Connected)
                {
                    client.Close();
                }
            }
        }

        /*
        *  Method  : CreateSession()
        *  Summary : Create a new game session by parsing a string file and using its contents to create a new session instance.  
        *  Params  : None.
        *  Return  :  
        *     string response = a string made up of the scrambled string, number of words to be found, and the session id.
        *     e.g. thisawh|6|NBIO-8346.
        */
        public string CreateSession()
        {
            string sessionId = Guid.NewGuid().ToString();
            string[] stringFiles = { "1.txt", "2.txt", "3.txt" };

            /* Randomly choose a scrambled string file */
            Random randomNumber = new Random();
            int randomIndex = randomNumber.Next(0, stringFiles.Length);
            StreamReader reader = new StreamReader(ConfigurationManager.AppSettings["stringsPath"] + stringFiles[randomIndex]);

            /* Parse info from the file */
            string scrambledString = reader.ReadLine();
            int numOfWords = int.Parse(reader.ReadLine());
            string[] wordsList = new string[numOfWords];
            int i = 0;

            while ((!reader.EndOfStream) && (i < numOfWords))
            {
                wordsList[i] = reader.ReadLine();
                i++;
            }

            /* Create a session for the client */
            SessionVariables playerSession = new SessionVariables
            {
                sessionId = sessionId,
                scrambledString = scrambledString,
                wordsList = wordsList,
                numOfWords = numOfWords
            };

            playerSessions.Add(playerSession); // Add session to list

            string response = scrambledString + "|" + numOfWords.ToString() + "|" + sessionId;
            return response;
        }

        /*
        *  Method  : TakeGuess()
        *  Summary : Handle a client guess and update session variables accordingly.  
        *  Params  : 
        *     string message = the message from the client containing the command, guess, and session id.
        *  Return  :  
        *     string response = a string made up of: a word indicating whether the guess was found in the string or not, the 
        *     number of words left to guess.
        */
        public string TakeGuess(string message)
        {
            string[] messageComponents = message.Split('|');
            SessionVariables tempSession = new SessionVariables();
            int sessionNumber = 0;
            string response = string.Empty;

            if (!SessionsActive()) { return "SessionNotFound"; }

            /* Search for player's session */
            for (int i = 0; i < playerSessions.Count; i++)
            {
                if (messageComponents[2] == playerSessions[i].sessionId)
                {
                    tempSession = playerSessions[i];
                    sessionNumber = i;
                    break;
                }
                else if (i == playerSessions.Count - 1) // Session does not exist
                {
                    response = "SessionNotFound";
                    return response;
                }
            }

            /* Determine if the guess was valid or not */
            for (int i = 0; i < tempSession.wordsList.Length; i++)
            {
                if (messageComponents[1] == tempSession.wordsList[i])
                {
                    /* Update session variables */
                    tempSession.wordsList[i] = null;
                    tempSession.numOfWords--;
                    playerSessions[sessionNumber] = tempSession;

                    response = "Valid|" + tempSession.numOfWords.ToString();
                    break;
                }
                else if (i == tempSession.wordsList.Length - 1) // Guess was wrong
                {
                    response = "Invalid|" + tempSession.numOfWords.ToString();
                }
            }

            return response;
        }

        /*
        *  Method  : EndSession()
        *  Summary : Enable user to end active session.  
        *  Params  : 
        *     string message = the message from the client containing the command and session id.
        *  Return  :  
        *     string response = a response informing the user that the session was ended successfully.
        */
        public string EndSession(string message)
        {
            string[] messageComponents = message.Split('|');
            string response = null;

            if (!SessionsActive()) { return "SessionNotFound"; }

            /* Search for player's session */
            for (int i = 0; i < playerSessions.Count; i++)
            {
                if (playerSessions[i].sessionId == messageComponents[1])
                {
                    playerSessions.Remove(playerSessions[i]);
                    response = "SessionDeleted";
                    break;
                }
                else if (i == playerSessions.Count - 1)
                {
                    response = "SessionNotFound";
                }
            }

            return response;
        }


        // Tells you if there are any active sessions or not
        public bool SessionsActive()
        {
            return playerSessions.Count > 0;
        }

        // Shut down server gracefully
        public void shutDownServer(TcpListener server)
        {
            if (server != null)
            {
                running = false; // Stop the server loop
                server.Stop();   // Stop the TCP listener
                Logger.Log("Server has been stopped."); // Log the shutdown
            }
        }

    }
}
