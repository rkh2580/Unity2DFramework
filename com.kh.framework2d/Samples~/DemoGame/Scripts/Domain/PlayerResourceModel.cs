using KH.Framework2D.Utils;

namespace KH.Framework2D.Samples.Demo.Domain
{
    /// <summary>
    /// Player resources data model.
    /// Contains observable properties that auto-notify UI when changed.
    /// </summary>
    public class PlayerResourceModel
    {
        public ObservableProperty<int> Gold { get; } = new(0);
        public ObservableProperty<int> Actions { get; } = new(3);
        public ObservableProperty<int> MaxActions { get; } = new(3);
        
        // Computed property
        public bool HasActions => Actions.Value > 0;
        
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold.Value += amount;
        }
        
        public bool SpendGold(int amount)
        {
            if (amount <= 0 || Gold.Value < amount) return false;
            Gold.Value -= amount;
            return true;
        }
        
        public bool UseAction()
        {
            if (Actions.Value <= 0) return false;
            Actions.Value--;
            return true;
        }
        
        public void RefillActions()
        {
            Actions.Value = MaxActions.Value;
        }
        
        public void Reset()
        {
            Gold.SetSilently(0);
            Actions.SetSilently(MaxActions.Value);
            
            // Notify all at once
            Gold.NotifySubscribers();
            Actions.NotifySubscribers();
        }
    }
}
