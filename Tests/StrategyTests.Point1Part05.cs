namespace StockSharp.Tests;

using System;
using System.Threading.Tasks;
using Ecng.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StockSharp.Messages;

public abstract partial class StrategyTests
{
	[TestMethod]
	[TestCategory("Point1Part05")]
	public async Task Point1Part05_ColorZeroLagMa_CloseOnlyModeProducesActualExit()
	{
		var sellClose = 0;
		var fills = 0;
		await Replay("1995_Color_ZeroLag_MA", (strategy, _) =>
		{
			foreach (var flag in new[] { "OpenBuy", "OpenSell", "CloseBuy", "CloseSell" })
				AreEqual(true, strategy.Parameters[flag].Value, $"{flag} should default to true.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Length", 5);
			SetParam(strategy, "OpenSell", false);
			strategy.OrderRegistering += order =>
			{
				AreEqual(OrderTypes.Market, order.Type);
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Sell)
				{
					IsTrue(strategy.Position > 0m, "OpenSell=false must not open shorts.");
					AreEqual(strategy.Position, order.Volume, "Close must match full current position.");
					sellClose++;
				}
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(sellClose > 0 && fills > 1, "Require actual opening and closing fills.");
	}

	[TestMethod]
	[TestCategory("Point1Part05")]
	public async Task Point1Part05_HeikenAshiNoWick_ActualContrarianFillAndPositionSize()
	{
		var registered = 0;
		var fills = 0;
		await Replay("1994_Heiken_Ashi_No_Wick", (strategy, _) =>
		{
			strategy.OrderRegistering += order =>
			{
				AreEqual(OrderTypes.Market, order.Type);
				IsTrue(order.Volume > 0m);
				if (strategy.Position != 0m && (strategy.Position > 0m) != (order.Side == Sides.Buy))
					IsTrue(order.Volume >= Math.Abs(strategy.Position),
						"An opposite no-wick candle must at least close the actual position.");
				registered++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(registered > 0 && fills > 0, "Archive must show an actual no-wick contrarian fill.");
	}

}
