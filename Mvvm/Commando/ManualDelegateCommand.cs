using System;
using System.Windows.Input;

namespace Where.Common.Mvvm.Commando
{
	public class ManualDelegateCommand : ICommand
	{
		private readonly Action<ICommandoInterface> _executor;
		private readonly Predicate<ICommandoInterface> _executorAvailability;


		public ManualDelegateCommand(Action<ICommandoInterface> executor, Predicate<ICommandoInterface> executorAvailability = null)
		{
			Utils.NotNullArgument(executor, "executor", "Cannot call a command if the command executor is null!");
			_executor = executor;
			_executorAvailability = executorAvailability;
		}

		#region Implementation of ICommand

		public bool CanExecute(object parameter)
		{
			Utils.IsValueValid(parameter is ICommandoInterface, "Objects must be an ICommandoInterface");

			if (_executorAvailability != null)
				return _executorAvailability(parameter as ICommandoInterface);

			return true;

		}

		public void Execute(object parameter)
		{
			Utils.IsValueValid(parameter is ICommandoInterface, "Objects must be an ICommandoInterface");

			if (CanExecute(parameter))
				_executor(parameter as ICommandoInterface);

		}

		public event EventHandler CanExecuteChanged;

		#endregion
	}

	public interface ICommandoInterface
	{
		object Parameter { get; }
		string ParentViewModel { get; }
		Action<object> OnOperationCompleted { get; }
	}

}
