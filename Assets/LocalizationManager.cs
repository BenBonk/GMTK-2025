using UnityEngine;
using UnityEngine.Localization;

public class LocalizationManager : MonoBehaviour
{
    [Header("Localization")]
    public LocalizedString localPointsString;
    public LocalizedString localTimeString;
    public LocalizedString localCashString;
    public LocalizedString localReadySetLasso;
    public LocalizedString localCloseCall;
    public LocalizedString localTimesUp;
    public LocalizedString localDayComplete;
    public LocalizedString localPointsPopup;
    public LocalizedString localCashPopup;
    public LocalizedString localDeathRound;

    [HideInInspector] public string closeCall;
    [HideInInspector] public string timesUp;
    [HideInInspector] public string readySetLasso;
    [HideInInspector] public string dayComplete;
    [HideInInspector] public string pointsPopup;
    [HideInInspector] public string cashPopup;
    private void OnEnable()
    {
        localPointsString.Arguments = new object[] { 0, 85 };
        localPointsString.StringChanged += UpdatePoints;
        
        localTimeString.Arguments = new object[] { "45.0" };
        localTimeString.StringChanged += UpdateTime;
        
        localCashString.Arguments = new object[] { "0" };
        localCashString.StringChanged += UpdateCash;
        
        localDayComplete.Arguments = new object[] { "25" };
        localDayComplete.StringChanged += UpdateDayComplete;
        
        localPointsPopup.Arguments = new object[] { "0" };
        localPointsPopup.StringChanged += UpdatePointsPopup;
        
        localCashPopup.Arguments = new object[] { "0" };
        localCashPopup.StringChanged += UpdateCashPopup;

        localCloseCall.StringChanged += UpdateCloseCall;
        localTimesUp.StringChanged += UpdateTimesUp;
        localReadySetLasso.StringChanged += UpdateReadySetLasso;
    }

    private void OnDisable()
    {
        localPointsString.StringChanged -= UpdatePoints;
        localTimeString.StringChanged -= UpdateTime;
        localCashString.StringChanged -= UpdateCash;
        localCloseCall.StringChanged -= UpdateCloseCall;
        localTimesUp.StringChanged -= UpdateTimesUp;
        localReadySetLasso.StringChanged -= UpdateReadySetLasso;
        localPointsPopup.StringChanged -= UpdatePointsPopup;
        localCashPopup.StringChanged -= UpdateCashPopup;
    }
    void UpdatePoints(string value) => GameController.gameManager.scoreDisplay.text = value;
    void UpdateTime(string value) => GameController.gameManager.timerDisplay.text = value;
    void UpdateCash(string value) => GameController.gameManager.currencyDisplay.text = value;
    void UpdateCloseCall(string value) => closeCall = value;
    void UpdateTimesUp(string value) => timesUp = value;
    void UpdateReadySetLasso(string value) => readySetLasso = value;
    void UpdateDayComplete(string value) => dayComplete = value;
    void UpdatePointsPopup(string value) => pointsPopup = value;
    void UpdateCashPopup(string value) => cashPopup = value;
}
