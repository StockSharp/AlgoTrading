using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes open positions when unrealized loss exceeds a volume-based threshold.
/// Positions are expected to be opened externally; this strategy only protects them.
/// </summary>
public class ZakryvatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _min001002;
	private readonly StrategyParam<decimal> _min002005;
	private readonly StrategyParam<decimal> _min00501;
	private readonly StrategyParam<decimal> _min0103;
	private readonly StrategyParam<decimal> _min0305;
	private readonly StrategyParam<decimal> _min051;
	private readonly StrategyParam<decimal> _minFrom1;

	/// <summary>Maximum loss for positions with volume ≤ 0.02 lots.</summary>
	public decimal Min001002 { get => _min001002.Value; set => _min001002.Value = value; }

	/// <summary>Maximum loss for positions with volume between 0.02 and 0.05 lots.</summary>
	public decimal Min002005 { get => _min002005.Value; set => _min002005.Value = value; }

	/// <summary>Maximum loss for positions with volume between 0.05 and 0.10 lots.</summary>
	public decimal Min00501 { get => _min00501.Value; set => _min00501.Value = value; }

	/// <summary>Maximum loss for positions with volume between 0.10 and 0.30 lots.</summary>
	public decimal Min0103 { get => _min0103.Value; set => _min0103.Value = value; }

	/// <summary>Maximum loss for positions with volume between 0.30 and 0.50 lots.</summary>
	public decimal Min0305 { get => _min0305.Value; set => _min0305.Value = value; }

	/// <summary>Maximum loss for positions with volume between 0.50 and 1 lot.</summary>
	public decimal Min051 { get => _min051.Value; set => _min051.Value = value; }

	/// <summary>Maximum loss for positions with volume greater than 1 lot.</summary>
	public decimal MinFrom1 { get => _minFrom1.Value; set => _minFrom1.Value = value; }

	/// <summary>Constructor.</summary>
	public ZakryvatorStrategy()
	{
		_min001002 = Param(nameof(Min001002), 4m)
			.SetDisplay("Loss ≤0.02", "Max loss for volume ≤0.02 lots", "Risk");
		_min002005 = Param(nameof(Min002005), 8m)
			.SetDisplay("Loss 0.02-0.05", "Max loss for volume 0.02-0.05 lots", "Risk");
		_min00501 = Param(nameof(Min00501), 10m)
			.SetDisplay("Loss 0.05-0.10", "Max loss for volume 0.05-0.10 lots", "Risk");
		_min0103 = Param(nameof(Min0103), 15m)
			.SetDisplay("Loss 0.10-0.30", "Max loss for volume 0.10-0.30 lots", "Risk");
		_min0305 = Param(nameof(Min0305), 20m)
			.SetDisplay("Loss 0.30-0.50", "Max loss for volume 0.30-0.50 lots", "Risk");
		_min051 = Param(nameof(Min051), 25m)
			.SetDisplay("Loss 0.50-1", "Max loss for volume 0.50-1 lots", "Risk");
		_minFrom1 = Param(nameof(MinFrom1), 30m)
			.SetDisplay("Loss >1", "Max loss for volume above 1 lot", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to trade ticks for real-time price updates.
		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		// Only act when there is an open position.
		if (Position == 0)
			return;

		var price = trade.TradePrice ?? 0m;
		// Calculate unrealized PnL based on current price and average entry price.
		var openPnL = Position * (price - PositionPrice);

		// Exit if there is no loss.
		if (openPnL >= 0m)
			return;

		var volume = Math.Abs(Position);

		// Select threshold according to position volume.
		var threshold = volume switch
		{
			<= 0.02m => Min001002,
			<= 0.05m => Min002005,
			<= 0.10m => Min00501,
			<= 0.30m => Min0103,
			<= 0.50m => Min0305,
			<= 1m => Min051,
			_ => MinFrom1,
		};

		// Close the position if loss exceeds the threshold.
		if (openPnL <= -threshold)
			ClosePosition();
	}
}

