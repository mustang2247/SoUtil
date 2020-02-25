using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.UI.Core
{
    public class VMFactory
    {
        private IUIViewLocator _viewLocator;
        private Dictionary<ViewModel, View> _vm2View = new Dictionary<ViewModel, View>();

        public VMFactory(IUIViewLocator viewLocator)
        {    
            SetViewLocator(viewLocator);
        }
        
        public void SetViewLocator(IUIViewLocator viewLocator)
        {
            _viewLocator = viewLocator;
        }
        
        public T Create<T>() where T : ViewModel,new()
        {
            T vm = new T();
            vm.IsShow.AddListener(show =>
            {
                switch (show)
                {
                    case ViewState.Show:
                        ShowView(vm);
                        break;
                    case ViewState.Hide:
                        HideView(vm);
                        break;
                    case ViewState.Destroy:
                        DestroyView(vm);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(show), show, null);
                }
            });
            
            return vm;
        }

        public void ChangeVM(ViewModel viewModel, View newView)
        {
            if (_vm2View.ContainsKey(viewModel))
            {
                _vm2View[viewModel] = newView;
                return;
            }
            _vm2View.Add(viewModel, newView);
            newView.SetVM(viewModel);
        }

        private View ShowView(ViewModel viewModel)
        {
            if (!_vm2View.TryGetValue(viewModel, out var view))
            {
                view = _viewLocator.Load(viewModel.ViewPath, viewModel);
                _vm2View.Add(viewModel, view);
            }
            view.Show();
            return view;
        }

        private void HideView(ViewModel viewModel)
        {
            if (_vm2View.TryGetValue(viewModel, out var view))
            {
                view.Hide();
            }
            Debugger.Warning($"{viewModel.ViewPath} window not show!");
        }

        private void DestroyView(ViewModel viewModel)
        {
            if (_vm2View.TryGetValue(viewModel, out var view))
            {
                UnityEngine.Object.Destroy(view.gameObject);
            }
            Debugger.Warning($"{viewModel.ViewPath} window not show!");
        }

        public View GetView(ViewModel viewModel)
        {
            return _vm2View.TryGetValue(viewModel, out var view) ? view : null;
        }
        
    }
}