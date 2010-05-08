namespace Paxos.Messages
{
	public class Propose : Message
	{
		public int ProposalNumber { get; set; }
		public int BallotNumber { get; set; }

		public override string ToString()
		{
			return string.Format("ProposalNumber: {0}, BallotNumber: {1}", ProposalNumber, BallotNumber);
		}
	}
}