using System.Collections.Generic;
using Paxos.Messages;
using System.Linq;

namespace Paxos
{
	public class Learner : Agent
	{
		private readonly int acceptorsCount;

		private readonly Dictionary<int, LearnerState> learnerState = new Dictionary<int, LearnerState>();
		private readonly HashSet<Agent> knownAcceptors = new HashSet<Agent>();
		public Learner(int acceptorsCount)
		{
			this.acceptorsCount = acceptorsCount;
			Commands = new List<ICommand>();
			Register<Accepted>(OnAccepted);
		}

		public IList<ICommand> Commands { get; set; }

		public int AppliedValue
		{
			get
			{
				return Commands.TakeWhile(command => command != null).Aggregate(0, (current, command) => command.Execute(current));
			}
		}

		private void OnAccepted(Accepted accepted)
		{
			knownAcceptors.Add(accepted.Originator);
			LearnerState state;
			if(learnerState.TryGetValue(accepted.ProposalNumber, out state) == false)
			{
				learnerState[accepted.ProposalNumber] = new LearnerState
				{
					BallotNumber = accepted.BallotNumber,
					NumberOfAccepts = 1,
					ProposalNumber = accepted.ProposalNumber
				};
			}
			else if(state.BallotNumber < accepted.BallotNumber)
			{
				return;
			}
			else if(state.Accepted == false)
			{
				state.NumberOfAccepts += 1;
				if (state.NumberOfAccepts < acceptorsCount / 2)
					return;
				while(Commands.Count < state.ProposalNumber)
					Commands.Add(null);
				state.Accepted = true;
				Commands[state.ProposalNumber - 1] = accepted.Value;
			}
		}

		public override bool ProcessTimeouts()
		{
			bool requestedHoles = false;
			var missingProposals = new List<int>();
			for (int i = 0; i < Commands.Count; i++)
			{
				if(Commands[i] == null)
					missingProposals.Add(i + 1);
			}
			foreach (var missingProposal in missingProposals)
			{
				requestedHoles = true;
				foreach (var acceptor in knownAcceptors)
				{
					acceptor.SendMessage(new AcceptedValueQuery
					{
						Originator = this,
						ProposalNumber = missingProposal
					});
				}
			}
			return requestedHoles;
		}

		#region Nested type: LearnerState

		private class LearnerState
		{
			public int ProposalNumber { get; set; }
			public bool Accepted { get; set; }
			public int BallotNumber { get; set; }
			public int NumberOfAccepts { get; set; }
		}

		#endregion
	}
}