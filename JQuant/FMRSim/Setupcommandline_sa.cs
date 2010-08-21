
using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;

using FMRShell;
//using Algo;

#if USEFMRSIM
using TaskBarLibSim;
#else
using TaskBarLib;
#endif


namespace JQuant
{
	partial class Program
	{
//		protected Algo.Base algoMachine = null;
		RezefOrderFSM rezefOrderFSM = null;

		/// <summary>
		/// Create Algo object and connect it to the data feed
		/// Data feed can be a playback or real-time data feed
		/// </summary>
		protected void algoStart(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
		}

		protected void algoStop(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
		}

		protected void algoStat(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
		}

		protected void algoStopUrgent(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
		}

		protected void algoPaper(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
		}

		protected void algoReal(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
		}

		protected void algoSkip(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
		}

		protected void algoSetMode(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
		}

		protected void LoadCommandLineInterface_sa()
		{
			Menu menuAlgo = cli.RootMenu.AddMenu("algo", "Algo debug",
								   "Semiautomatic traidng routines");
			menuAlgo.AddCommand("start",
				"Create algo",
				"Create algo, connect to the data feed - DataCollector.rezefProducer. Args: " +
#if USEFMRSIM
				"<logFile> [speedup]",
#else			                  
				"",
#endif
				algoStart);

			menuAlgo.AddCommand("stop",
							  "Graceful stop",
							  "Stop the FSM gracefuly - takes care to close all pending orders",
							   algoStop);
			menuAlgo.AddCommand("stopu",
							  "Immediate stop",
							  "Gets out immediately. Does not clean up the pending orders if any",
							   algoStop);
			menuAlgo.AddCommand("stat",
							  "Show algo statistics counters",
							  "Show traffic related and trades related statistics",
							   algoStat);
			menuAlgo.AddCommand("s",
							  "Skip trigger",
							  "Skip the trigger, go on with looking for triggers",
							algoSkip);
			menuAlgo.AddCommand("p",
							  "Launch paper trade",
							  "Simulated paper trading using either real time data or playback log",
							algoPaper);
			menuAlgo.AddCommand("o",
							  "Launch real trade",
							  "Real cash order using based on real-time data feed",
							algoReal);
			menuAlgo.AddCommand("sm",
							  "Toggle Algo FSM mode. Usage: sm a|s ",
							  "Set mode: semiautomatic or automatic",
							algoSetMode);
		}
	}
}
