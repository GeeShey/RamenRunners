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
        //binding the onPrepStarted action to the onStartedNewPrep() method
        workerComponent.onPrepStarted += (prepText) => onStartedNewPrep(prepText);
        workerComponent.onMovementStarted += (destinationStation) => onFinishedNewPrep();

    }

    public void onStartedNewPrep(string _prepText)
    {
        prepTextCanvas.gameObject.SetActive(true);
        background.gameObject.transform.localScale = new Vector3(perCharacterScale * _prepText.Length, background.gameObject.transform.localScale.y, background.gameObject.transform.localScale.z) ;
        preText.text = _prepText;

    }

    public void onFinishedNewPrep()
    {
        prepTextCanvas.gameObject.SetActive(false);
    }

}
