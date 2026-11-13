using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using PEAKNucleus.Helpers;
using PEAKNucleus.Objects;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace stenlan.PEAKNucleus;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static Plugin Instance { get; private set; }
    internal static new ConfigFile Config { get; private set; }

    private static readonly string NAMED_PIPE_NAME = "PEAK-Nucleus-IPC";

    private static int numClients = 1; // ourselves

    private static int myClientIndex = 0;


    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    private static void ShowMessage(string text)
    {
        var _ = MessageBox(IntPtr.Zero, text, "PEAK Nucleus Handler", 0);
    }

    private void Awake()
    {
        Instance = this;

        Log = Logger;
        Debug.s_DefaultLogger.logEnabled = true;
        Debug.s_Logger.logEnabled = true;

        InitIPC();

        // Redirect Photon to local Luxon server
        AppSettings appSettings = PhotonNetwork.PhotonServerSettings.AppSettings;
        appSettings.UseNameServer = true;
        appSettings.Server = "localhost";

        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private static void InitIPC()
    {
        Log.LogInfo($"In InitIPC");
        try
        {
            NamedPipeServerStream pipeServer =
                new NamedPipeServerStream(NAMED_PIPE_NAME, PipeDirection.InOut, 1);
            BecomeIPCHost(pipeServer);
        }
        catch (IOException e) // max number of servers reached, this is good, means other instance is server pipe
        {
            BecomeIPCClient();
        }
        catch (Exception e)
        {
            ShowMessage($"Unknown error opening pipe: {e.Message}");
        }        
    }

    private static void BecomeIPCHost(NamedPipeServerStream pipeServer)
    {
        // Start luxon server first, synchronously, to not race "SetPhotonServerSettings"
        ProcessHelper.StartDependentProcess(Path.GetFullPath($"..\\..\\..\\luxon\\luxon_server.msvc.release.exe", Assembly.GetExecutingAssembly().Location));

        var serverThread = new Thread(StartIPCServer);
        serverThread.Start(pipeServer);
    }

    private static void StartIPCServer(object? pipeServerObj)
    {
        Log.LogInfo($"In StartServer");
        var pipeServer = (NamedPipeServerStream) pipeServerObj!;
        var streamInt = new StreamInt(pipeServer);

        while (true)
        {
            pipeServer.WaitForConnection();

            Log.LogInfo($"IPC client {numClients} connected");

            try
            {
                streamInt.WriteInt(numClients++);
                pipeServer.Disconnect();
            }
            catch (Exception e)
            {
                ShowMessage($"Unknown IPC pipe error in server: {e.Message}");
            }
        }
    }

    private static void BecomeIPCClient()
    {
        var pipeClient =
                    new NamedPipeClientStream(".", NAMED_PIPE_NAME,
                        PipeDirection.InOut, PipeOptions.None);

        Log.LogInfo("Connecting to IPC server...");
        try
        {
            pipeClient.Connect(10000);
            myClientIndex = new StreamInt(pipeClient).ReadInt();
            pipeClient.Close();
        }
        catch (Exception e)
        {
            ShowMessage($"Failed to connect to IPC server: {e.Message}. Click OK to exit.");
            Environment.Exit(1);
        }

        Log.LogInfo($"Received client index {myClientIndex} from IPC server.");
    }
}
