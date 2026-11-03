using UnityEngine;
using TMPro;

public class HunterAmmoUI : MonoBehaviour
{
    private const string ReadyIcon = "●";
    private const string ReloadingIcon = "x";
    private const string ReadyText = "Bullet ready";
    private const string ReloadingText = "Reloading";

    [SerializeField] private FSMAgentHunter _hunter;

    [Header("UI")]
    [SerializeField] private TMP_Text _ammoIcon;
    [SerializeField] private TMP_Text _ammoText;

    private void Update()
    {
        if (_hunter == null)
            return;

        if (_hunter.HasBullet)
        {
            _ammoIcon.text = ReadyIcon;
            _ammoText.text = ReadyText;
        }
        else
        {
            _ammoIcon.text = ReloadingIcon;
            _ammoText.text = ReloadingText;
        }
    }
}
