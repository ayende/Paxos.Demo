namespace Paxos
{
	public interface ICommand
	{
		int Execute(int currentValue);
	}
}