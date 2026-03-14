using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using System;
namespace Caskev.NetcodeForXRInteractionToolkitSamples.DemoScene
{

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private UnityTransport _transport;
        [Header("ClientSide Events")]
        [SerializeField] private UnityEvent<ulong> _onConnectionSuccess;
        [SerializeField] private UnityEvent _onConnectionFailed;
        [SerializeField] private UnityEvent _onDisconnected;
        [Header("ServerSide Events")]
        [SerializeField] private UnityEvent _onHostSuccess;
        [SerializeField] private UnityEvent _onHostFailed;
        [SerializeField] private UnityEvent _onHostStopped;
        [SerializeField] private UnityEvent<ulong> _onClientConnected;
        [SerializeField] private UnityEvent<ulong> _onClientDisconnected;
        [SerializeField] private bool _debugVerbose;
        private bool _isHosting;
        private bool _isJoining;
        public void Host()
        {
            _networkManager.OnClientDisconnectCallback += OnHostFailedDisconnect;
            _networkManager.OnServerStopped += OnHostFailedServerStopped;
            _networkManager.OnTransportFailure += OnHostFailed;
            _networkManager.OnClientConnectedCallback += OnHostSucces;
            _isHosting = true;
            try
            {
                if (!_networkManager.StartHost())
                {
                    OnHostFailed();
                }
            }
            catch (Exception e)
            {
                if (_isHosting)
                {
                    Debug.LogError(e);
                    OnHostFailed();
                }
            }
        }

        private void OnHostFailedServerStopped(bool obj)
        {
            OnHostFailed();
        }

        private void OnHostFailedDisconnect(ulong obj)
        {
            OnHostFailed();
        }

        private void OnHostFailed()
        {
            _isHosting = false;
            _networkManager.OnClientDisconnectCallback -= OnHostFailedDisconnect;
            _networkManager.OnServerStopped -= OnHostFailedServerStopped;
            _networkManager.OnTransportFailure -= OnHostFailed;
            _networkManager.OnClientConnectedCallback -= OnHostSucces;
            if (_debugVerbose) Debug.Log("HOST FAILED");
            _onHostFailed.Invoke();
        }

        private void OnHostSucces(ulong obj)
        {
            _networkManager.OnClientDisconnectCallback -= OnHostFailedDisconnect;
            _networkManager.OnServerStopped -= OnHostFailedServerStopped;
            _networkManager.OnTransportFailure -= OnHostFailed;
            _networkManager.OnClientConnectedCallback -= OnHostSucces;

            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            _networkManager.OnTransportFailure += OnHostStopped;
            _networkManager.OnServerStopped += OnHostStoppedServerStopped;
            if (_debugVerbose) Debug.Log("HOST SUCCESS");
            _onHostSuccess.Invoke();
        }

        private void OnHostStoppedServerStopped(bool obj)
        {
            OnHostStopped();
        }

        private void OnHostStopped()
        {
            _isHosting = false;
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _networkManager.OnTransportFailure -= OnHostStopped;
            _networkManager.OnServerStopped -= OnHostStoppedServerStopped;
            if (_debugVerbose) Debug.Log("HOST STOPPED");
            _onHostStopped.Invoke();
        }

        private void OnClientConnected(ulong clientId)
        {
            if (_debugVerbose) Debug.Log("CLIENT CONNECTED WITH ID " + clientId);
            _onClientConnected.Invoke(clientId);
        }
        private void OnClientDisconnected(ulong clientId)
        {
            if (_debugVerbose) Debug.Log("CLIENT " + clientId + " DISCONNECTED");
            _onClientDisconnected.Invoke(clientId);
        }
        public void Join()
        {
            _networkManager.OnClientConnectedCallback += OnConnected;
            _networkManager.OnClientDisconnectCallback += OnConnectionFailedDisconnect;
            _networkManager.OnTransportFailure += OnConnectionFailed;
            _isJoining = true;
            if (_debugVerbose) Debug.Log("CONNECTING TO HOST...");
            try
            {
                if (!_networkManager.StartClient())
                {
                    OnConnectionFailed();
                }
            }
            catch (Exception e)
            {
                if (_isJoining)
                {
                    Debug.LogError(e);
                    OnConnectionFailed();
                }
            }
        }
        private void OnConnectionFailedDisconnect(ulong obj)
        {
            OnConnectionFailed();
        }

        private void OnConnectionFailed()
        {
            _isJoining = false;
            _networkManager.OnClientConnectedCallback -= OnConnected;
            _networkManager.OnClientDisconnectCallback -= OnConnectionFailedDisconnect;
            _networkManager.OnTransportFailure -= OnConnectionFailed;
            _onConnectionFailed.Invoke();
            if (_debugVerbose) Debug.Log("CONNECTION FAILED");
        }


        private void OnConnected(ulong clientId)
        {
            _networkManager.OnClientConnectedCallback -= OnConnected;
            _networkManager.OnClientDisconnectCallback -= OnConnectionFailedDisconnect;
            _networkManager.OnTransportFailure -= OnConnectionFailed;

            _networkManager.OnClientDisconnectCallback += OnDisconnected;
            _onConnectionSuccess.Invoke(clientId);
            if (_debugVerbose) Debug.Log("CONNECTED TO HOST WITH ID " + clientId);
        }

        private void OnDisconnected(ulong obj)
        {
            _isJoining = false;
            _networkManager.OnClientDisconnectCallback -= OnDisconnected;
            _onDisconnected.Invoke();
            if (_debugVerbose) Debug.Log("DISCONNECTED FROM HOST");
        }

        public void Stop()
        {
            _isHosting = false;
            _isJoining = false;
            _networkManager.Shutdown();
            _networkManager.OnClientDisconnectCallback -= OnHostFailedDisconnect;
            _networkManager.OnServerStopped -= OnHostFailedServerStopped;
            _networkManager.OnTransportFailure -= OnHostFailed;
            _networkManager.OnClientConnectedCallback -= OnHostSucces;
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _networkManager.OnTransportFailure -= OnHostStopped;
            _networkManager.OnServerStopped -= OnHostStoppedServerStopped;
            _networkManager.OnClientConnectedCallback -= OnConnected;
            _networkManager.OnClientDisconnectCallback -= OnConnectionFailedDisconnect;
            _networkManager.OnTransportFailure -= OnConnectionFailed;
            _networkManager.OnClientDisconnectCallback -= OnDisconnected;
            if (_debugVerbose) Debug.Log("SHUTDOWN");
        }
    }
}