using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PrepTextUpdater : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Canvas prepTextCanvas;
    public RawImage background;
    public TextMeshProUGUI preText;
    public Worker workerComponent;

    public float perCharacterScale = 0.02f;
    void Start()
    {
        prepTextCanvas.gameObject.SetActive(false);
        //binding the OnPrepStarted action to the OnStartedNewPrep() method
        workerComponent.OnPrepStarted += (prepText) => OnStartedNewPrep(prepText);
        workerComponent.OnMovementStarted += (destinationStation) => OnFinishedNewPrep();

    }

    public void OnStartedNewPrep(string prepText)
    {
        prepTextCanvas.gameObject.SetActive(true);
        background.gameObject.transform.localScale = new Vector3(perCharacterScale * prepText.Length, background.gameObject.transform.localScale.y, background.gameObject.transform.localScale.z) ;
        preText.text = prepText;

    }

    public void OnFinishedNewPrep()
    {
        prepTextCanvas.gameObject.SetActive(false);
    }

}
