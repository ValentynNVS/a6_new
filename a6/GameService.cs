using a6;
using System;
using System.ServiceProcess;
using System.Threading;
using TCPIPServer;
using System.Diagnostics;
namespace a6
{
    public partial class GameService : ServiceBase
    {

        private GameServer gameServer;
        private Thread serverThread;

        public GameService()
        {
            this.ServiceName = "GameServerService";
        }

        // OnStart method to start the server
        protected override void OnStart(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();
            base.OnStart(args);


            // Initialize your GameServer class here
            gameServer = new GameServer();

            // Start the server logic in a separate thread
            serverThread = new Thread(new ThreadStart(gameServer.StartServer));
            serverThread.Start();

            // Log service start
            Logger.Log("GameServer service started.");
        }

        // OnStop method to stop the server
        protected override void OnStop()
        {
            base.OnStop();

            // Gracefully stop the game server
            if (gameServer != null)
            {
                gameServer.shutDownServer(server); // Pass the server instance to shut it down
            }

            if (serverThread != null)
            {
                serverThread.Join();  // Wait for the server thread to finish
            }

            // Log service stop
            Logger.Log("GameServer service stopped.");
        }

    }
}
