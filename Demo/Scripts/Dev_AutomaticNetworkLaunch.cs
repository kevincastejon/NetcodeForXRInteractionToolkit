using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace KevinCastejon.NetcodeForXRInteractionToolkit.Examples
{
    public class Dev_AutomaticNetworkLaunch : MonoBehaviour
    {
        [SerializeField] private AutoConnectionMode _autoConnectionMode;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        private void Start()
        {
            if (_autoConnectionMode.Value == AutoConnectionModes.CLIENT)
            {
                Debug.Log("Automatically joining from clone project");
                _joinButton.onClick.Invoke();
            }
            else
            {
                Debug.Log("Automatically hosting from original project");
                _hostButton.onClick.Invoke();
            }
        }
    }
}
