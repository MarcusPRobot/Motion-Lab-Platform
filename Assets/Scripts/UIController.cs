using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] FixedUdpHub udpClient;

    [SerializeField] public GameObject settingsMenu;
    [SerializeField] public GameObject simulationMenu;
    [SerializeField] public GameObject oceanWorld;
    [SerializeField] public GameObject labWorld;

    [SerializeField] public GameObject connectionEstablished;
    [SerializeField] public GameObject connectionFailed;
    [SerializeField] public GameObject connectionSpinner;

    [SerializeField] public Toggle simulinkRecieveToggle;
    [SerializeField] public Toggle motionRecieveToggle;

    private bool bSettingsMenu = false;
    private bool bSimulationMenu = false;
    private bool bOceanicWorld = false;

    private bool stateSim = true;
    private bool stateMotion = true;

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

    public void connectToLab()
    {

        if (true)
            connectionEstablished.SetActive(true);
        else
        {
            //connectionFailed.SetActive(true);
        }

        connectionSpinner.SetActive(false);
    }

    public void simuRecieve()
    {
        if (!stateSim)
        {
            motionRecieveToggle.interactable = true;
            udpClient.receiveSimulink = false;
        }
        else
        {
            motionRecieveToggle.interactable = false;
            udpClient.receiveSimulink = true;
        }
        stateSim = !stateSim;
    }

    public void motionRecieve()
    {
        if (!stateMotion)
        {
            simulinkRecieveToggle.interactable = true;
            udpClient.receivePLC = false;
        }
        else
        {
            simulinkRecieveToggle.interactable = false;
            udpClient.receivePLC = true;
        }

        stateMotion = !stateMotion;
    }

    public void changeLocalIP(string ip)
    {
        udpClient.localBindIP = ip;
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

}
