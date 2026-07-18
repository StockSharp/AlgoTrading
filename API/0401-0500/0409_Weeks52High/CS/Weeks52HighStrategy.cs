// Weeks52HighStrategy.cs
// -----------------------------------------------------------------------------
// 52-week high proximity strategy.
// Buys when price is near its highest level (close to 52-week high),
// sells when price drops significantly below the high.
// Uses Highest indicator to track the rolling high.
// Cooldown prevents excessive trading.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the 52-week high proximity effect.
/// </summary>
public class Weeks52HighStrategy : Strategy
{
	private readonly StrategyParam<int> _highPeriod;
	private readonly StrategyParam<decimal> _entryRatio;
	private readonly StrategyParam<decimal> _exitRatio;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Lookback period for highest high.
	/// </summary>
	public int HighPeriod
	{
		get => _highPeriod.Value;
		set => _highPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ratio of current price to high to enter (e.g. 0.95 = within 5% of high).
	/// </summary>
	public decimal EntryRatio
	{
		get => _entryRatio.Value;
		set => _entryRatio.Value = value;
	}

	/// <summary>
	/// Exit when price drops below this ratio of the high.
	/// </summary>
	public decimal ExitRatio
	{
		get => _exitRatio.Value;
		set => _exitRatio.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private Highest _highest;
	private int _cooldownRemaining;

	public Weeks52HighStrategy()
	{
		_highPeriod = Param(nameof(HighPeriod), 50)
			.SetDisplay("High Period", "Rolling high lookback period", "Parameters");

		_entryRatio = Param(nameof(EntryRatio), 0.97m)
			.SetDisplay("Entry Ratio", "Min price/high ratio to enter", "Parameters");

		_exitRatio = Param(nameof(ExitRatio), 0.92m)
			.SetDisplay("Exit Ratio", "Exit when price/high drops below this", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = HighPeriod };

		SubscribeCandles(CandleType)
			.Bind(_highest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		if (highestValue <= 0)
			return;

		var ratio = candle.ClosePrice / highestValue;

		// Price is near the high -> momentum effect -> buy
		if (ratio >= EntryRatio && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Price has dropped far from the high -> exit
		else if (ratio <= ExitRatio && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
