﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using GamePlay.Utilities;
using Newtonsoft.Json;
using PokemonCore.Combat;
using PokemonCore.Utility;
using Unity.Networking.Transport;
using UnityEditor;
using UnityEngine.UI;

namespace PokemonCore.Network
{
    /// <summary>
    /// 通过NetworkLogic来组织游戏中的网络链接逻辑
    /// </summary>
    public static class NetworkLogic
    {
        public static float TimeToFlush = 5;

        public static Dictionary<IPAddress, NetworkBroadcastData> usersBroadcast =
            new Dictionary<IPAddress, NetworkBroadcastData>();

        public static void PairOnBattle(int trainersNum = 2, int pokemonsPerTrainer = 1, string password = "")
        {
            _password = password;
            _broadcastType = BroadcastType.SearchForBattle;
            _trainersNum = trainersNum;
            _pokemonsPerTrainer = pokemonsPerTrainer;
            _randomNum = Game.Random.Next(20000,30000);
            NetworkLocal.StartToDetect();
            NetworkLocal.OnDetectBroadcast = OnDetectedPair;
            NetworkLocal.StartToBroadCast(new NetworkBroadcastData(BroadcastType.SearchForBattle, _randomNum, password,
                Game.trainer, trainersNum, pokemonsPerTrainer));
        }

        private static string _password;
        private static BroadcastType _broadcastType;
        private static int _trainersNum;
        private static int _pokemonsPerTrainer;
        private static int _randomNum;

        public static void PairOff()
        {
            NetworkLocal.OnDetectBroadcast = null;
            NetworkLocal.StopBroadCast();
            NetworkLocal.StopDetect();
        }

        private static List<string> _pairedAddr;

        /// <summary>
        /// 对于初次收到
        /// </summary>
        /// <param name="result"></param>
        /// <param name="password"></param>
        static void OnDetectedPair(UdpReceiveResult result, string password)
        {
            if (result.RemoteEndPoint.Address.Equals(IPAddress.Parse(NetworkLocal.GetAddressIP()))) return;

            UnityEngine.Debug.Log($"Network Detected! from: {result.RemoteEndPoint.Address}");

            NetworkBroadcastData data = JsonConvert.DeserializeObject<NetworkBroadcastData>(password);
            if (data == null) throw new Exception("Unknown data from online");
            if (data.password != _password && data.broadCastType != _broadcastType) return;

            if (usersBroadcast.ContainsKey(result.RemoteEndPoint.Address))
                usersBroadcast[result.RemoteEndPoint.Address] = data;
            else
                usersBroadcast.Add(result.RemoteEndPoint.Address, data);

            //TODO: 加入对方取消链接时的处理情况
            if (usersBroadcast.Count == _trainersNum - 1)
            {
                UnityEngine.Debug.Log("Have matched, start battle");
                int maxRandom = (from d in usersBroadcast.Values select d.randomNum).Max();
                bool isHost = false;
                if (maxRandom < _randomNum)
                {
                    //这个时候是本机当主机
                    Game.Random = new Random(_randomNum);
                    isHost = true;
                    BecomeHost(_randomNum);
                    UnityEngine.Debug.Log("Become Host");
                }
                else
                {
                    //这个时候是maxRandom机当主机
                    Game.Random = new Random(maxRandom);
                    var arr = (from d in usersBroadcast where d.Value.randomNum == maxRandom select d.Key).ToArray();
                    if (arr.Length != 1) throw new Exception("Error occured");
                    isHost = false;
                    BecomeClient(arr[0], (ushort)maxRandom);
                    UnityEngine.Debug.Log("Become Client");
                }

                NetworkLocal.StopDetect();

                new Timer(new TimerCallback((o) =>
                    {
                        NetworkLocal.StopBroadCast();
                        UnityEngine.Debug.Log("Stop broadcast");
                    }),
                    null, 1000, Timeout.Infinite);

                StartBattle(isHost);
            }
        }


        public static Action<List<Trainer>, List<Trainer>, List<Trainer>, List<Trainer>, bool, int> OnStartBattle;

        static void StartBattle(bool isHost)
        {
            var trainers = (from d in usersBroadcast select d.Value).ToList();
            trainers.Sort((o1, o2) => o1.randomNum - o2.randomNum);

            List<Trainer> allies = new List<Trainer>();
            List<Trainer> oppos = new List<Trainer>();


            if (_randomNum < trainers[trainers.Count / 2].randomNum)
            {
                for (int i = 0; i < trainers.Count / 2; i++)
                {
                    allies.Add(trainers[i].Trainer);
                }

                for (int i = trainers.Count / 2; i < trainers.Count; i++)
                {
                    oppos.Add(trainers[i].Trainer);
                }
            }
            else
            {
                for (int i = 0; i <= trainers.Count / 2; i++)
                {
                    oppos.Add(trainers[i].Trainer);
                }

                for (int i = trainers.Count / 2 + 1; i < trainers.Count; i++)
                {
                    allies.Add(trainers[i].Trainer);
                }
            }

            OnStartBattle?.Invoke(allies, oppos, null, allies.Union(oppos).ToList(), isHost, _pokemonsPerTrainer);
            // Game.Instance.StartBattle(allies,oppos,null,allies.Union(oppos).ToList(),isHost,_pokemonsPerTrainer);
            if (isHost)
            {
                Battle.Instance.OnUserChooseInstruction += ServerSendInstruction;
            }
            else
            {
                Battle.Instance.OnUserChooseInstruction += ClientSendInstruction;
            }
        }

        static void FlushPairs()
        {
            Thread.Sleep((int) (TimeToFlush * 1000));
            _pairedAddr.Clear();
        }


        static void FlushUsers(object ipAddress)
        {
            IPAddress address = ipAddress as IPAddress;
            Thread.Sleep((int) (TimeToFlush * 1000));
            if (address != null)
                usersBroadcast.Remove(address);
        }


        /// <summary>
        /// 用来解决一次性传了两或多个个Json字符串，编程：{}{}的情况
        /// </summary>
        /// <returns></returns>
        // TODO
        private static List<string> ParseJson(string str)
        {
            str = string.Concat(str, StringBuffer);
            // TODO:解决一次性传两个json的问题
            var strs = str.Split('\n').ToArray();
            List<string> result = new List<string>();
            for (int i = 0; i < strs.Length; i++)
            {
                //一般来说最后一个元素要么是空要么是没有完全接收的包
                if (i == str.Length - 1)
                {
                    StringBuffer = strs[i];
                }
                else
                {
                    result.Add(strs[i]);
                }
            }

            // strs.RemoveAt(strs.Count - 1);
            UnityEngine.Debug.Log("Parse Json: " + result.ConverToString());
            return result;
        }

        private static string StringBuffer = "";

        /// <summary>
        /// 玩家无法通过游戏界面选择自己是Host还是Client,所有的配对都通过UDP广播来完成，然后由第一个UDP广播的作为Host
        /// </summary>
        static void BecomeHost(int port)
        {
            
            MonoSingleton<ServerBehaviour>.Instance.Init((ushort)port,OnReceiveMsg,OnDisconnect);

            void OnReceiveMsg(string msg, NetworkConnection connection)
            {
                if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) return;
                // UnityEngine.Debug.Log(msg);
                Instruction ins = JsonConvert.DeserializeObject<Instruction>(msg);
                Battle.Instance.ReceiveInstruction(ins, false);
                MonoSingleton<ServerBehaviour>.Instance.Broadcast(msg,connection);
            }

            void OnDisconnect()
            {
                Battle.Instance.InterruptAndEndBattle();
            }
            
            // NetworkLocal.OnServerReceiveMessage = (data, client) =>
            // {
            //     string str = Encoding.UTF8.GetString(data);
            //     IPAddress clientAddress = ((IPEndPoint) client.RemoteEndPoint).Address;
            //     UnityEngine.Debug.Log($"Server Receive Message from: {clientAddress.ToString()}");
            //     NetworkLocal.SendToClients(str, clientAddress);
            //     foreach (var s in ParseJson(str))
            //     {
            //         if (string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s)) continue;
            //         UnityEngine.Debug.Log(s);
            //         Instruction ins = JsonConvert.DeserializeObject<Instruction>(s);
            //         Battle.Instance.ReceiveInstruction(ins, false);
            //     }
            // };

            // NetworkLocal.BuildHost(_trainersNum, port);
        }


        static void BecomeClient(IPAddress ipAddress, ushort port = 30000)
        {
            
            MonoSingleton<ClientBehaviour>.Instance.Init(ipAddress.ToString(),port,OnRecevieMsg,OnDisconnect);

            void OnRecevieMsg(string msg)
            {
                if (string.IsNullOrWhiteSpace(msg) || string.IsNullOrEmpty(msg)) return;
                UnityEngine.Debug.Log(msg);
                Instruction ins = JsonConvert.DeserializeObject<Instruction>(msg);
                Battle.Instance.ReceiveInstruction(ins, false);
            }

            void OnDisconnect()
            {
                Battle.Instance.InterruptAndEndBattle();
            }

            // NetworkLocal.OnClientReceiveMessage = (data) =>
            // {
            //     string str = Encoding.UTF8.GetString(data);
            //     UnityEngine.Debug.Log("Client Receive Message");
            //     foreach (var s in ParseJson(str))
            //     {
            //         if (string.IsNullOrWhiteSpace(s) || string.IsNullOrEmpty(s)) continue;
            //         UnityEngine.Debug.Log(s);
            //         Instruction ins = JsonConvert.DeserializeObject<Instruction>(s);
            //         Battle.Instance.ReceiveInstruction(ins, false);
            //     }
            // };
            // NetworkLocal.StartClient(ipAddress, port);
            // new Thread().Start(ipAddress);
        }

        static void ServerSendInstruction(Instruction instruction)
        {
            // new Thread((o1) =>
            // {
            //     string str = JsonConvert.SerializeObject(instruction) + "\n";
            //     UnityEngine.Debug.Log($"Server send Instruction :{str}");
            //     NetworkLocal.SendToClients(str);
            // }).Start();
            string str = JsonConvert.SerializeObject(instruction);
            MonoSingleton<ServerBehaviour>.Instance.Broadcast(str);
        }

        //TODO：不知道用线程能不能解决卡顿的问题
        static void ClientSendInstruction(Instruction instruction)
        {
            // new Thread(() =>
            // {
            //     string str = JsonConvert.SerializeObject(instruction) + "\n";
            //     UnityEngine.Debug.Log($"Client send Instruction :{str}");
            //     NetworkLocal.SendToServer(str);
            // }).Start();
            string str = JsonConvert.SerializeObject(instruction);
            MonoSingleton<ClientBehaviour>.Instance.SendToServer(str);
        }
    }
}