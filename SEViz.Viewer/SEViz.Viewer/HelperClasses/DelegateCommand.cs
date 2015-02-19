using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace LoadingPanelSample.HelperClasses
{
    /// <summary>
    /// This class allows delegating the commanding logic to methods passed as parameters,
    /// and enables a View to bind commands to objects that are not part of the element tree.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        #region Fields

        private readonly Action _executeMethod;
        private readonly Func<bool> _canExecuteMethod;
        private List<WeakReference> _canExecuteChangedHandlers;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
        /// </summary>
        /// <param name="executeMethod">The execute method.</param>
        public DelegateCommand(Action executeMethod) : this(executeMethod, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
        /// </summary>
        /// <param name="executeMethod">The execute method.</param>
        /// <param name="canExecuteMethod">The can execute method.</param>
        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
        {
            if (executeMethod == null)
            {
                throw new ArgumentNullException("executeMethod");
            }

            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManagerHelper.AddWeakReferenceHandler(ref _canExecuteChangedHandlers, value);
            }
            remove
            {
                CommandManagerHelper.RemoveWeakReferenceHandler(_canExecuteChangedHandlers, value);
            }
        }

        #endregion

        #region ICommand Members

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns><c>true</c> if this command can be executed; otherwise, <c>false</c>.</returns>
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        void ICommand.Execute(object parameter)
        {
            Execute();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Method to determine if the command can be executed.
        /// </summary>
        /// <returns><c>true</c> if this instance can execute; otherwise, <c>false</c>.</returns>
        public bool CanExecute()
        {
            if (_canExecuteMethod != null)
            {
                return _canExecuteMethod();
            }

            return true;
        }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        public void Execute()
        {
            if (_executeMethod != null)
            {
                _executeMethod();
            }
        }

        /// <summary>
        /// Raises the CanExecuteChaged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        /// <summary>
        /// Protected virtual method to raise CanExecuteChanged event
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            CommandManagerHelper.CallWeakReferenceHandlers(_canExecuteChangedHandlers);
        }

        #endregion
    }
}