namespace Paxos.Commands
{
	public interface ICommand
	{
		int Execute(int currentValue);
	}
}