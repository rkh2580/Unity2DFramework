using KH.Framework2D.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KH.Framework2D.Samples.Demo.Presentation
{
    /// <summary>
    /// UI view for displaying player resources.
    /// </summary>
    public class ResourceView : BaseView
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _actionText;
        
        [Header("Buttons (Optional - for testing)")]
        [SerializeField] private Button _addGoldButton;
        [SerializeField] private Button _useActionButton;
        
        // Expose buttons for presenter binding
        public Button AddGoldButton => _addGoldButton;
        public Button UseActionButton => _useActionButton;
        
        public void SetGold(int amount)
        {
            if (_goldText != null)
                _goldText.text = $"{amount:N0} G";
        }
        
        public void SetActions(int current, int max)
        {
            if (_actionText != null)
                _actionText.text = $"{current} / {max} AP";
        }
        
        // Simplified overload when max is not needed
        public void SetActions(int current)
        {
            if (_actionText != null)
                _actionText.text = $"{current} AP";
        }
    }
}
