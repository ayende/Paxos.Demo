namespace Paxos.Commands
{
	public class Add : ICommand
	{
		public int Value { get; set; }

		#region ICommand Members

		public int Execute(int currentValue)
		{
			return currentValue + Value;
		}

		#endregion
	}
}