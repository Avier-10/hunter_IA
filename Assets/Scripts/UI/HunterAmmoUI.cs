using UnityEngine;
using TMPro;

public class HunterAmmoUI : MonoBehaviour
{
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
            _ammoIcon.text = "●";
            _ammoText.text = "Bala lista";
        }
        else
        {
            _ammoIcon.text = "x";
            _ammoText.text = "Recargando bala";
        }
    }
}
