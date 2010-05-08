namespace Paxos.Commands
{
	public class Multiply : ICommand
	{
		public int Value { get; set; }

		public int Execute(int currentValue)
		{
			return currentValue*Value;
		}
	}
}