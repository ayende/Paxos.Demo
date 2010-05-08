namespace Paxos.Messages
{
	public abstract class Message
	{
		public Agent Originator { get; set; }
	}
}