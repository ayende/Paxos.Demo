namespace Paxos.Messages
{
	public class AcceptedValueQuery : Message
	{
		public int ProposalNumber { get; set; }

		public override string ToString()
		{
			return string.Format("ProposalNumber: {0}", ProposalNumber);
		}
	}
}