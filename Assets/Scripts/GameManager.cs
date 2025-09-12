using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    private TransparentWindow transparentWindow;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get reference to TransparentWindow component
        transparentWindow = TransparentWindow.instance;
        if (transparentWindow == null)
        {
            Debug.LogWarning("TransparentWindow component not found! Window position saving/loading will not work.");
        }

        if (PlayerPrefs.HasKey("calibrationComplete"))
        {
            // Restore camera settings
            float cameraX = PlayerPrefs.GetFloat("cameraX");
            float cameraY = PlayerPrefs.GetFloat("cameraY");
            float cameraZ = PlayerPrefs.GetFloat("cameraZ");
            Camera.main.transform.position = new Vector3(cameraX, cameraY, cameraZ);
            Camera.main.orthographicSize = PlayerPrefs.GetFloat("cameraZoom");
            Camera.main.GetComponentInChildren<Camera>().orthographicSize = PlayerPrefs.GetFloat("cameraZoom");

            Camera overlayCam = GameObject.FindGameObjectsWithTag("OverlayCamera")[0].GetComponent<Camera>();
            if (overlayCam)
            {
                overlayCam.orthographicSize = PlayerPrefs.GetFloat("cameraZoom");
            }
            else
            {
                Debug.Log("OVERLAY CAM NOT FOUND");
            }
        }
        else
        {

            Debug.Log("no playerprefs found");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            // Save camera settings
            PlayerPrefs.SetFloat("cameraX", Camera.main.transform.position.x);
            PlayerPrefs.SetFloat("cameraY", Camera.main.transform.position.y);
            PlayerPrefs.SetFloat("cameraZ", Camera.main.transform.position.z);
            PlayerPrefs.SetFloat("cameraZoom", Camera.main.orthographicSize);

            // Save window position and dimensions through TransparentWindow
            if (transparentWindow != null)
            {
                transparentWindow.SaveWindowPosition();
            }
            else
            {
                Debug.LogWarning("TransparentWindow not found - window position not saved!");
            }

            // Mark calibration as complete
            PlayerPrefs.SetInt("calibrationComplete", 1);

            // Save to disk immediately
            PlayerPrefs.Save();

            Debug.Log("Calibration saved! Camera and window settings will be restored on next launch.");
        }

    }
}