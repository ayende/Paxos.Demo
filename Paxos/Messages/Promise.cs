using Paxos.Commands;

namespace Paxos.Messages
{
	public class Promise : Message
	{
		public ICommand AcceptedValue { get; set; }
		public int ProposalNumber { get; set; }
		public int BallotNumber { get; set; }

		public override string ToString()
		{
			return string.Format("AcceptedValue: {0}, ProposalNumber: {1}, BallotNumber: {2}", AcceptedValue, ProposalNumber,
			                     BallotNumber);
		}
	}
}