namespace Paxos.Commands
{
	public class Multiply : ICommand
	{
		public int Value { get; set; }

		#region ICommand Members

		public int Execute(int currentValue)
		{
			return currentValue*Value;
		}

		#endregion
	}
}