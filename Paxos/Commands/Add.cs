using System;

namespace Paxos.Commands
{
	public class Add : ICommand
	{
		public int Value { get; set; }

		public int Execute(int currentValue)
		{
			return currentValue + Value;
		}
	}
}