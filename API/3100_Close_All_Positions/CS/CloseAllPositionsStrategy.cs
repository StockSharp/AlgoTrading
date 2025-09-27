using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Flattens all open positions once the floating profit reaches the configured threshold.
/// Replicates the behaviour of the "Close all positions" MQL utility expert.
/// </summary>
public class CloseAllPositionsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private bool _closeAllRequested;

	/// <summary>
	/// Minimum floating profit that triggers the closing routine.
	/// </summary>
	public decimal ProfitThreshold
	{
		get => _profitThreshold.Value;
		set => _profitThreshold.Value = value;
	}

	/// <summary>
	/// Candle type used to detect new bars.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CloseAllPositionsStrategy()
	{
		_profitThreshold = Param(nameof(ProfitThreshold), 10m)
			.SetDisplay("Profit Threshold", "Floating profit required to close every position", "General")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for periodic checks", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closeAllRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with completed candles to emulate the new-bar trigger from the EA.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Keep sending exit orders until all positions are closed.
		if (_closeAllRequested)
		{
			CloseAllPositions();

			if (!HasAnyOpenPosition())
				_closeAllRequested = false;

			return;
		}

		var totalProfit = CalculateTotalProfit();

		if (totalProfit < ProfitThreshold)
			return;

		LogInfo($"Floating profit {totalProfit:0.##} reached threshold {ProfitThreshold:0.##}. Closing all positions.");

		_closeAllRequested = true;
		CloseAllPositions();
	}

	private decimal CalculateTotalProfit()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		// Prefer the aggregated floating profit provided by the portfolio when available.
		if (portfolio.CurrentProfit is decimal currentProfit)
			return currentProfit;

		decimal total = 0m;

		// Fallback: accumulate the reported PnL of each open position.
		foreach (var position in portfolio.Positions)
			total += position.PnL ?? 0m;

		return total;
	}

	private void CloseAllPositions()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var securities = new HashSet<Security>();

		if (Security != null)
			securities.Add(Security);

		// Include securities from child strategies that might hold independent positions.
		foreach (var position in Positions)
		{
			if (position.Security != null)
				securities.Add(position.Security);
		}

		// Include all securities that have positions inside the portfolio.
		foreach (var position in portfolio.Positions)
		{
			if (position.Security != null)
				securities.Add(position.Security);
		}

		foreach (var security in securities)
		{
			var volume = GetPositionValue(security, portfolio) ?? 0m;
			if (volume > 0m)
			{
				// Send a sell market order to flatten long exposure.
				SellMarket(volume, security);
			}
			else if (volume < 0m)
			{
				// Send a buy market order to offset short exposure.
				BuyMarket(-volume, security);
			}
		}
	}

	private bool HasAnyOpenPosition()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return false;

		if (Position != 0m)
			return true;

		foreach (var position in portfolio.Positions)
		{
			var volume = position.CurrentValue ?? 0m;
			if (volume != 0m)
				return true;
		}

		return false;
	}
}

