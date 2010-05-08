namespace Paxos.Messages
{
	public class Accept : Message
	{
		public override string ToString()
		{
			return string.Format("ProposalNumber: {0}, BallotNumber: {1}, Value: {2}", ProposalNumber, BallotNumber, Value);
		}

		public int ProposalNumber { get; set; }
		public int BallotNumber { get; set; }
		public ICommand Value { get; set; }
	}
}