namespace Paxos.Messages
{
	public class Propose : Message
	{
		public override string ToString()
		{
			return string.Format("ProposalNumber: {0}, BallotNumber: {1}", ProposalNumber, BallotNumber);
		}

		public int ProposalNumber { get; set; }
		public int BallotNumber { get; set; }
	}
}