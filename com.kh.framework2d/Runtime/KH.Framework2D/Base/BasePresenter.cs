using System;
using VContainer.Unity;

namespace KH.Framework2D.Base
{
    /// <summary>
    /// Base presenter without model dependency.
    /// Use for pure UI logic (settings, menus, etc.)
    /// </summary>
    /// <typeparam name="TView">View type</typeparam>
    public abstract class BasePresenter<TView> : IStartable, IDisposable 
        where TView : BaseView
    {
        protected readonly TView View;
        
        protected BasePresenter(TView view)
        {
            View = view;
        }
        
        /// <summary>
        /// Called by VContainer when the scope starts.
        /// </summary>
        public void Start()
        {
            OnBind();
        }
        
        /// <summary>
        /// Called by VContainer when the scope is disposed.
        /// </summary>
        public void Dispose()
        {
            OnUnbind();
        }
        
        /// <summary>
        /// Bind events here (Model->View, View->Model).
        /// Called on Start.
        /// </summary>
        protected abstract void OnBind();
        
        /// <summary>
        /// Unbind events here to prevent memory leaks.
        /// Called on Dispose.
        /// </summary>
        protected abstract void OnUnbind();
    }
    
    /// <summary>
    /// Base presenter with model dependency.
    /// Use when presenter needs to sync data between Model and View.
    /// </summary>
    /// <typeparam name="TView">View type</typeparam>
    /// <typeparam name="TModel">Model type</typeparam>
    public abstract class BasePresenter<TView, TModel> : BasePresenter<TView>
        where TView : BaseView
    {
        protected readonly TModel Model;
        
        protected BasePresenter(TView view, TModel model) : base(view)
        {
            Model = model;
        }
    }
}
