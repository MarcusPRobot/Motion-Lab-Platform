using System.Data.Common;
using System.Net.Sockets;
using Assets.SimpleSpinner;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] FixedUdpHub udpClient;

    [Header("Main Menu")]
    [SerializeField] public GameObject settingsMenu;
    [SerializeField] public GameObject simulationMenu;
    [SerializeField] public GameObject oceanWorld;
    [SerializeField] public GameObject labWorld;

    [Header("Server Connection")]
    [SerializeField] public GameObject connectionEstablished;
    [SerializeField] public GameObject connectionFailed;
    [SerializeField] public GameObject connectionSpinner;
    [SerializeField] public Toggle simulinkRecieveToggle;
    [SerializeField] public Toggle motionRecieveToggle;

    [Header("Rexroth simulation")]
    [SerializeField] StewartIK_DeltaAB smallStewart;
    [SerializeField] Toggle demoToggle;


    private bool bSettingsMenu = false;
    private bool bSimulationMenu = false;
    private bool bOceanicWorld = false;

    private bool stateSim = true;
    private bool stateMotion = true;

    private int sliderValue = 0;


    void Start()
    {
        oceanWorld.SetActive(false);
        labWorld.SetActive(true);

        settingsMenu.SetActive(bSettingsMenu);
        simulationMenu.SetActive(bSimulationMenu);
    }

    public void closeMenus()
    {
        bSettingsMenu = false;
        bSimulationMenu = false;

        settingsMenu.SetActive(bSettingsMenu);
        simulationMenu.SetActive(bSimulationMenu);
    }

    public void ToggleSettingsMenu()
    {
        bSettingsMenu = !bSettingsMenu;

        settingsMenu.SetActive(bSettingsMenu);
    }

    public void ToggleSimulationMenu()
    {
        bSimulationMenu = !bSimulationMenu;

        simulationMenu.SetActive(bSimulationMenu);
    }

    public void SwitchWorld()
    {
        bOceanicWorld = !bOceanicWorld;
        oceanWorld.SetActive(bOceanicWorld);
        labWorld.SetActive(!bOceanicWorld);
    }

    public void restartConnection()
    {
        connectionSpinner.SetActive(true);
        udpClient.StopServer();
        udpClient.StartServer();
    }

    public void simuRecieve()
    {
        if (!stateSim)
        {
            motionRecieveToggle.interactable = true;
            udpClient.receiveSimulink = false;
            demoToggle.interactable = true;
        }
        else
        {
            motionRecieveToggle.interactable = false;
            udpClient.receiveSimulink = true;
            demoToggle.interactable = false;
        }
        stateSim = !stateSim;
    }

    public void motionRecieve()
    {
        if (!stateMotion)
        {
            simulinkRecieveToggle.interactable = true;
            udpClient.receivePLC = false;
            demoToggle.interactable = true;
        }
        else
        {
            simulinkRecieveToggle.interactable = false;
            udpClient.receivePLC = true;
            demoToggle.interactable = false;
        }

        stateMotion = !stateMotion;
    }

    public void changeLocalIP(string ip)
    {
        udpClient.localBindIP = ip;
        Debug.Log(udpClient.localBindIP);
    }
    public void changeLocalPort(string port)
    {
        if (int.TryParse(port, out int p)) udpClient.listenPort = p;
    }
    public void changeSimulinkIP(string ip)
    {
        udpClient.peerAIP = ip;
    }
    public void changePLCIP(string ip)
    {
        udpClient.peerBIP = ip;
    }
    public void changeSimulinkPort(string port)
    {
        if (int.TryParse(port, out int p)) udpClient.peerAPort = p;
    }
    public void changePLCPort(string port)
    {
        if (int.TryParse(port, out int p)) udpClient.peerBPort = p;
    }

    public void sliderChoice(int id)
    {
        sliderValue = id;
    }

    public void changeSimVal(float val)
    {
        if (sliderValue < 6)
        {
            smallStewart.changeAMP(val, sliderValue);
        }
        else if (sliderValue < 12)
        {
            smallStewart.changeFreq(val, sliderValue - 6);
        }
    }

    public void enableDemo(bool enable)
    {
        smallStewart.demoEnabled = enable;
    }
}
