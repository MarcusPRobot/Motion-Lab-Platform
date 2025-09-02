using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class FixedUdpHub : MonoBehaviour
{
    public Datahandling dataHandler;
    public UIController UI;

    public bool receiveSimulink = false;
    public bool receivePLC = false;

    [Header("Addresses (set these manually)")]
    [Tooltip("Local address to bind on this PC (use this NIC IP or 0.0.0.0).")]
    public string localBindIP = "192.168.1.10";
    [Tooltip("Unity server listen port.")]
    public int listenPort = 15555;

    [Tooltip("Peer A remote IP and port (packets accepted only from here).")]
    public string peerAIP = "192.168.1.12";
    public int    peerAPort = 15556;

    [Tooltip("Peer B remote IP and port (optional second sender).")]
    public string peerBIP = "192.168.1.11";
    public int    peerBPort = 15557;

    // Optional helpers if you want to update at runtime
    public void ChangeLocal(string ip, int port) { localBindIP = ip; listenPort = port; }
    public void ChangePeerA(string ip, int port) { peerAIP = ip; peerAPort = port; }
    public void ChangePeerB(string ip, int port) { peerBIP = ip; peerBPort = port; }
    public void StartServerButton() => StartServer();

    [Header("Runtime (read-only)")]
    public string boundOn;
    public string peerA;
    public string peerB;

    public event Action<string, byte[]> OnDatagram;     // src = "A" or "B"
    public event Action<string, string> OnTextDatagram; // UTF8 convenience

    UdpClient udp;
    Thread rxThread;
    volatile bool running;

    IPEndPoint bindEP;
    IPEndPoint epA;
    IPEndPoint epB;

    readonly object mainLock = new object();
    readonly System.Collections.Generic.Queue<Action> mainQueue = new();

    void OnEnable()  => StartServer();
    void OnDisable() => StopServer();

    void Update()
    {
        lock (mainLock)
            while (mainQueue.Count > 0) mainQueue.Dequeue()?.Invoke();
    }

    public void StartServer()
    {
        if (udp != null) return;

        try
        {
            // Build endpoints
            var bindIP = IPAddress.Parse(localBindIP);
            bindEP = new IPEndPoint(bindIP, listenPort);

            epA = new IPEndPoint(IPAddress.Parse(peerAIP), peerAPort);
            epB = new IPEndPoint(IPAddress.Parse(peerBIP), peerBPort);

            // Bind
            udp = new UdpClient(bindEP);
            boundOn = ((IPEndPoint)udp.Client.LocalEndPoint).ToString();
            peerA = epA.ToString();
            peerB = epB.ToString();

            running = true;
            rxThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "FixedUdpHub_RX" };
            rxThread.Start();

            Debug.Log($"[FixedUdpHub] Listening on {boundOn} | A={peerA} | B={peerB}");
            UI.connectionSpinner.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FixedUdpHub] Start failed: {ex.Message}");
            StopServer();
            UI.connectionSpinner.SetActive(false);
        }
    }

    public void StopServer()
    {
        running = false;
        try { udp?.Close(); } catch { }
        udp = null;
        try { rxThread?.Join(50); } catch { }
        rxThread = null;
    }

    void ReceiveLoop()
    {
        var any = new IPEndPoint(IPAddress.Any, 0);

        while (running && udp != null)
        {
            try
            {
                byte[] data = udp.Receive(ref any); // blocking
                string src = null;

                // Accept only from configured remote endpoints (match IP + port)
                if (any.Address.Equals(epA.Address) && any.Port == epA.Port && receiveSimulink) src = "A";
                else if (any.Address.Equals(epB.Address) && any.Port == epB.Port && receivePLC) src = "B";
                else
                {

                    EnqueueMain(() => Debug.Log($"[UDP] Ignored {any.Address}:{any.Port} (not a configured peer, or not enabled receive toggle)"));
                    { /* log ignored */ continue; }
                }

                EnqueueMain(() =>
                {
                    OnDatagram?.Invoke(src, data);
                    try
                    {
                        // Trim at first NUL/newline to avoid padded zeros
                        int end = Array.IndexOf<byte>(data, 0);
                        if (end < 0) end = data.Length;
                        var s = Encoding.UTF8.GetString(data, 0, end).TrimEnd('\r', '\n', '\0', ' ');
                        OnTextDatagram?.Invoke(src, s);

                        lastMessage = s;
                        lastSource = src;

                        if (dataHandler) dataHandler.inputSorter(s);
                    }
                    catch { /* binary payload */ }
                });
            }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) { /* closed or transient */ }
            catch (Exception ex)
            {
                EnqueueMain(() => Debug.LogWarning($"[FixedUdpHub] RX error: {ex.Message}"));
            }
        }
    }

    void EnqueueMain(Action a)
    {
        lock (mainLock) mainQueue.Enqueue(a);
    }

    // --------- SEND API ---------
    public void SendToA(byte[] data) => SafeSend(epA, data);
    public void SendToB(byte[] data) => SafeSend(epB, data);
    public void SendToA(string text)  => SendToA(Encoding.UTF8.GetBytes(text ?? ""));
    public void SendToB(string text)  => SendToB(Encoding.UTF8.GetBytes(text ?? ""));
    public void SendToBoth(byte[] data) { SafeSend(epA, data); SafeSend(epB, data); }
    public void SendToBoth(string text) => SendToBoth(Encoding.UTF8.GetBytes(text ?? ""));

    void SafeSend(IPEndPoint ep, byte[] data)
    {
        if (udp == null || data == null || ep == null) return;
        try { udp.Send(data, data.Length, ep); }
        catch (Exception ex) { EnqueueMain(() => Debug.LogWarning($"[FixedUdpHub] Send error to {ep}: {ex.Message}")); }
    }

    // --------- LAST MESSAGE CACHE ---------
    [Header("Latest Data (for other scripts to read)")]
    [TextArea] public string lastMessage;
    public string lastSource; // "A" or "B"
    public string GetLastMessage() => lastMessage;
}
