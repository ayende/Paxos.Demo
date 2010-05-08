namespace Paxos.Messages
{
	public class ProposalSubsumed : Message
	{
		public int BallotNumber { get; set; }
		public int ProposalNumber { get; set; }

		public override string ToString()
		{
			return string.Format("BallotNumber: {0}, ProposalNumber: {1}", BallotNumber, ProposalNumber);
		}
	}
}