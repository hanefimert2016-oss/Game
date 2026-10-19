using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace ArcSquad
{
    // Dedicated player-host / LAN client menus. No pretend matchmaking or hidden cloud relay.
    public sealed class ArcLobby : MonoBehaviour
    {
        NetworkManager network;
        UnityTransport transport;
        [SerializeField] GameObject enemyPrefab;
        public void Configure(GameObject prefab) => enemyPrefab=prefab;
        string ip="127.0.0.1",portText="7777";
        GUIStyle heading,body,mini,button,field;
        Texture2D panelTexture;
        bool askedDisconnect;
        const int PlayerLimit=8;

        void Start()
        {
            network=FindFirstObjectByType<NetworkManager>();
            if (network) transport=network.GetComponent<UnityTransport>();
            if (network!=null)
            {
                network.NetworkConfig.ConnectionApproval=true;
                network.ConnectionApprovalCallback=ApproveConnection;
                network.OnClientDisconnectCallback+=ClientLeft;
                if(enemyPrefab) network.AddNetworkPrefab(enemyPrefab);
            }
            Application.targetFrameRate=90;
        }

        void ApproveConnection(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved=network.ConnectedClientsIds.Count<PlayerLimit;
            response.CreatePlayerObject=response.Approved;
            response.Pending=false;
            response.Reason=response.Approved?"":"Lobby full (max 8 players)";
        }

        void ClientLeft(ulong id)
        {
            if (network!=null && network.LocalClientId==id) askedDisconnect=true;
        }

        void OnDestroy()
        {
            if (network!=null)
            {
                network.OnClientDisconnectCallback-=ClientLeft;
                network.ConnectionApprovalCallback-=ApproveConnection;
            }
        }

        void Host()
        {
            if (network==null || transport==null) return;
            if(!int.TryParse(portText,out int port)||!SquadRules.ValidPort(port))return;
            transport.SetConnectionData("127.0.0.1",(ushort)port,"0.0.0.0");
            askedDisconnect=!network.StartHost();
            if(!askedDisconnect){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        }

        void Connect()
        {
            if(network==null||transport==null)return;
            if(!int.TryParse(portText,out int port)||!SquadRules.ValidPort(port))return;
            if(string.IsNullOrWhiteSpace(ip)||ip.Length>255)return;
            transport.SetConnectionData(ip.Trim(),(ushort)port);
            askedDisconnect=!network.StartClient();
            if(!askedDisconnect){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        }

        void Disconnect()
        {
            if(network&&network.IsListening)network.Shutdown();
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            askedDisconnect=false;
        }

        void Styles()
        {
            if(heading!=null)return;
            heading=new GUIStyle(GUI.skin.label){fontSize=31,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft};
            heading.normal.textColor=new Color(.95f,.98f,1f);
            body=new GUIStyle(GUI.skin.label){fontSize=17,fontStyle=FontStyle.Bold};
            body.normal.textColor=new Color(.63f,.93f,1f);
            mini=new GUIStyle(GUI.skin.label){fontSize=13,alignment=TextAnchor.MiddleLeft};
            mini.normal.textColor=new Color(.65f,.76f,.89f);
            button=new GUIStyle(GUI.skin.button){fontSize=18,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            button.normal.textColor=Color.white;
            field=new GUIStyle(GUI.skin.textField){fontSize=17,padding=new RectOffset(12,8,8,8)};
            panelTexture=new Texture2D(1,1);
            panelTexture.SetPixel(0,0,new Color(.018f,.055f,.12f,.88f));panelTexture.Apply();
        }

        void Panel(Rect rect){GUI.DrawTexture(rect,panelTexture);}
        void Label(Rect rect,string text,GUIStyle style){GUI.Label(rect,text,style);}

        void OnGUI()
        {
            Styles();
            if(network==null){Label(new Rect(20,20,700,50),"NetworkManager missing",heading);return;}
            float scale=Mathf.Clamp(Screen.height/800f,.62f,1.1f);
            Matrix4x4 old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float w=Screen.width/scale,h=Screen.height/scale;

            if (!network.IsListening)
            {
                var rect=new Rect((w-520)/2,(h-455)/2,520,455);
                Panel(rect);
                Label(new Rect(rect.x+35,rect.y+25,465,50),"ARC // SQUAD",heading);
                Label(new Rect(rect.x+35,rect.y+83,440,26),"NEON SKYLINE — CO-OP CITY DEFENSE",mini);
                Label(new Rect(rect.x+35,rect.y+119,440,30),"MULTIPLAYER · UP TO 8 PLAYERS",body);
                Label(new Rect(rect.x+35,rect.y+163,125,35),"Host / LAN IP",mini);
                ip=GUI.TextField(new Rect(rect.x+166,rect.y+162,290,34),ip,field);
                Label(new Rect(rect.x+35,rect.y+213,125,32),"UDP Port",mini);
                portText=GUI.TextField(new Rect(rect.x+166,rect.y+209,290,35),portText,field);
                if(GUI.Button(new Rect(rect.x+35,rect.y+270,205,55),"HOST GAME",button))Host();
                if(GUI.Button(new Rect(rect.x+253,rect.y+270,203,55),"JOIN GAME",button))Connect();
                string hint=askedDisconnect?"Connection failed or disconnected. Check IP/port.":
                    "Host on 0.0.0.0; clients join your LAN IP. Online requires UDP port forwarding or VPN.";
                Label(new Rect(rect.x+35,rect.y+342,460,48),hint,mini);
                Label(new Rect(rect.x+35,rect.y+405,455,23),"Original armored flyers • colorful mini houses • co-op drones",mini);
                GUI.matrix=old;return;
            }

            ArcPilot me=null;
            foreach(var pilot in FindObjectsByType<ArcPilot>(FindObjectsSortMode.None))
                if (pilot.IsSpawned && pilot.IsOwner){me=pilot;break;}
            ArcMission mission=FindFirstObjectByType<ArcMission>();
            Panel(new Rect(15,14,340,173));
            Label(new Rect(30,23,320,46),"ARC // SQUAD",heading);
            string netName=network.IsHost?"HOST":"CLIENT";
            Label(new Rect(30,73,315,26),netName+"  •  "+network.ConnectedClientsIds.Count+" / 8 ONLINE",mini);
            Label(new Rect(30,104,325,29),"SHIELD  "+Mathf.RoundToInt(me?me.Shield.Value:100)+"%   ENERGY "+Mathf.RoundToInt(me?me.Energy.Value:100)+"%",body);
            Label(new Rect(30,138,325,33),"SCORE "+(me?me.Points.Value:0)+"  •  KILLS "+(me?me.Eliminations.Value:0),mini);

            Panel(new Rect(w-250,18,225,113));
            Label(new Rect(w-236,26,220,29),"CITY DEFENSE",body);
            Label(new Rect(w-236,62,220,25),"WAVE "+(mission?mission.Wave.Value:1)+" · CO-OP",mini);
            Label(new Rect(w-236,88,220,25),"TARGETS DOWN "+(mission?mission.DronesDefeated.Value:0),mini);

            Label(new Rect(30,h-70,1050,27),"WASD MOVE  •  SPACE UP  •  C DOWN  •  SHIFT BOOST  •  F / LMB REPULSOR  •  R / RMB ROCKET  •  TAB CURSOR",mini);
            if(Cursor.lockState!=CursorLockMode.Locked)
            {
                if(GUI.Button(new Rect(w-200,h-63,173,40),"DISCONNECT",button))Disconnect();
            }
            GUI.matrix=old;
        }
    }
}
