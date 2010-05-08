using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Paxos.Commands;
using Paxos.Messages;

namespace Paxos.Agents
{
	public class Learner : Agent
	{
		private readonly int acceptorsCount;

		private readonly HashSet<Agent> knownAcceptors = new HashSet<Agent>();
		private readonly Dictionary<int, LearnerState> learnerState = new Dictionary<int, LearnerState>();
		private DateTime lastRequestedTopProposal = DateTime.MinValue;

		public Learner(int acceptorsCount)
		{
			this.acceptorsCount = acceptorsCount;
			Commands = new List<ICommand>();
			Register<Accepted>(OnAccepted);
		}

		public IList<ICommand> Commands { get; set; }

		public int AppliedValue
		{
			get { return Commands.TakeWhile(command => command != null).Aggregate(0, (current, command) => command.Execute(current)); }
		}

		private void OnAccepted(Accepted accepted)
		{
			knownAcceptors.Add(accepted.Originator);
			LearnerState state;
			if (learnerState.TryGetValue(accepted.ProposalNumber, out state) == false)
			{
				learnerState[accepted.ProposalNumber] = new LearnerState
				{
					BallotNumber = accepted.BallotNumber,
					NumberOfAccepts = 1,
					ProposalNumber = accepted.ProposalNumber
				};
			}
			else if (state.BallotNumber < accepted.BallotNumber)
			{
				return;
			}
			else if (state.Accepted == false)
			{
				log.DebugFormat("Value accepted for {0}", state.ProposalNumber);
				state.NumberOfAccepts += 1;
				if (state.NumberOfAccepts < acceptorsCount / 2)
					return;
				while (Commands.Count < state.ProposalNumber)
					Commands.Add(null);
				state.Accepted = true;
				Commands[state.ProposalNumber - 1] = accepted.Value;
			}
		}

		public override bool ProcessTimeouts()
		{
			var requestedHoles = false;
			var missingProposals = new List<int>();
			for (var i = 0; i < Commands.Count; i++)
			{
				if (Commands[i] == null)
					missingProposals.Add(i + 1);
			}
			if (lastRequestedTopProposal.AddSeconds(Debugger.IsAttached ? 30 : 3) < DateTime.Now)
			{
				lastRequestedTopProposal = DateTime.Now;
				missingProposals.Add(Commands.Count+1); // in case we didn't get the latest proposal
			}
			foreach (var missingProposal in missingProposals)
			{
				log.DebugFormat("Querying accepted value for {0}", missingProposal);
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