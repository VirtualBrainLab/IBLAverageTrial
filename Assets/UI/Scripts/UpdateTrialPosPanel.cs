using UnityEngine;


// Should be attached to TrialPosPanel
public class UpdateTrialPosPanel : MonoBehaviour
{
    [SerializeField] private RectTransform trialPosLineT;
    private float trialPosPanelWidth;
    private IBLTask iblTask;

    #region UI elements
    [SerializeField] GameObject parentPanelGO;

    [SerializeField] private RectTransform stimOnT;
    [SerializeField] private RectTransform wheelT;
    [SerializeField] private RectTransform feedbackT;

    [SerializeField] private EventAverageManager eaManager;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        iblTask = GameObject.Find("main").GetComponent<ExperimentManager>().GetIBLTask();
        RectTransform trialPosPanelBody = parentPanelGO.GetComponent<RectTransform>();
        trialPosPanelWidth = trialPosPanelBody.rect.width;
    }

    // Update is called once per frame
    void Update()
    {
        int curIndex = iblTask.GetTimeIndex();
        float curX = trialPosPanelWidth * curIndex / (eaManager.trialDatasetType ? 250 : 100);
        //Debug.Log(curIndex + " " + curX);
        trialPosLineT.anchoredPosition = new Vector2(curX, trialPosLineT.anchoredPosition.y);
    }

    public void UpdateTextPositions(int stimOnIdx, int wheelIdx, int feedbackIdx, int scaledLength)
    {
        if (stimOnIdx > 0)
        {
            stimOnT.gameObject.SetActive(true);
            stimOnT.anchoredPosition = new Vector2(trialPosPanelWidth * stimOnIdx / scaledLength, 0f);
        }
        else
            stimOnT.gameObject.SetActive(false);
        if (wheelIdx > 0)
        {
            wheelT.gameObject.SetActive(true);
            wheelT.anchoredPosition = new Vector2(trialPosPanelWidth * wheelIdx / scaledLength, 0f);
        }
        else
            wheelT.gameObject.SetActive(false);
        if (feedbackIdx > 0)
        {
            feedbackT.gameObject.SetActive(true);
            feedbackT.anchoredPosition = new Vector2(trialPosPanelWidth * feedbackIdx / scaledLength, 0f);
        }
        else
            feedbackT.gameObject.SetActive(false);
    }
}
