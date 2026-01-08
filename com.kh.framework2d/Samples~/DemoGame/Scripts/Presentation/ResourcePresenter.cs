using KH.Framework2D.Base;
using KH.Framework2D.Samples.Demo.Domain;
using VContainer;

namespace KH.Framework2D.Samples.Demo.Presentation
{
    /// <summary>
    /// Presenter that connects ResourceView with PlayerResourceModel.
    /// Handles data binding and user input.
    /// </summary>
    public class ResourcePresenter : BasePresenter<ResourceView, PlayerResourceModel>
    {
        [Inject]
        public ResourcePresenter(ResourceView view, PlayerResourceModel model) 
            : base(view, model)
        {
        }
        
        protected override void OnBind()
        {
            // Model -> View (data changes update UI)
            Model.Gold.Subscribe(View.SetGold);
            Model.Actions.Subscribe(UpdateActionDisplay);
            Model.MaxActions.Subscribe(_ => UpdateActionDisplay(Model.Actions.Value));
            
            // View -> Model (user input triggers logic)
            if (View.AddGoldButton != null)
                View.AddGoldButton.onClick.AddListener(OnAddGoldClicked);
            
            if (View.UseActionButton != null)
                View.UseActionButton.onClick.AddListener(OnUseActionClicked);
        }
        
        protected override void OnUnbind()
        {
            // Unsubscribe to prevent memory leaks
            Model.Gold.Unsubscribe(View.SetGold);
            Model.Actions.Unsubscribe(UpdateActionDisplay);
            
            if (View.AddGoldButton != null)
                View.AddGoldButton.onClick.RemoveListener(OnAddGoldClicked);
            
            if (View.UseActionButton != null)
                View.UseActionButton.onClick.RemoveListener(OnUseActionClicked);
        }
        
        // Event handlers (separated for testability)
        private void OnAddGoldClicked()
        {
            Model.AddGold(10);
        }
        
        private void OnUseActionClicked()
        {
            Model.UseAction();
        }
        
        private void UpdateActionDisplay(int current)
        {
            View.SetActions(current, Model.MaxActions.Value);
        }
    }
}
