namespace Paxos.Messages
{
	public class AcceptedValueQuery : Message
	{
		public override string ToString()
		{
			return string.Format("ProposalNumber: {0}", ProposalNumber);
		}

		public int ProposalNumber { get; set; }
	}
}