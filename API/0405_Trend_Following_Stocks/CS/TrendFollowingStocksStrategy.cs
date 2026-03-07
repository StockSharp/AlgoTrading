// TrendFollowingStocksStrategy.cs
// -----------------------------------------------------------------------------
// Breakout trend following: enters on new high (Highest indicator),
// exits using ATR-based trailing stop.
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
/// Breakout trend-following strategy with ATR trailing stop.
/// </summary>
public class TrendFollowingStocksStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLen;
	private readonly StrategyParam<int> _highestLen;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// ATR period length.
	/// </summary>
	public int AtrLen
	{
		get => _atrLen.Value;
		set => _atrLen.Value = value;
	}

	/// <summary>
	/// Highest high lookback period.
	/// </summary>
	public int HighestLen
	{
		get => _highestLen.Value;
		set => _highestLen.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing stop distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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

	private AverageTrueRange _atr;
	private Highest _highest;
	private decimal _trailStop;
	private decimal _entryPrice;
	private int _cooldownRemaining;

	public TrendFollowingStocksStrategy()
	{
		_atrLen = Param(nameof(AtrLen), 14)
			.SetDisplay("ATR Length", "ATR period length", "Parameters");

		_highestLen = Param(nameof(HighestLen), 40)
			.SetDisplay("Highest Length", "Lookback period for highest high", "Parameters");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.5m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for trailing stop", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 10)
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

		_atr = null;
		_highest = null;
		_trailStop = 0;
		_entryPrice = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr = new AverageTrueRange { Length = AtrLen };
		_highest = new Highest { Length = HighestLen };

		SubscribeCandles(CandleType)
			.Bind(_atr, _highest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal highestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed || !_highest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
		}

		var close = candle.ClosePrice;

		// If in position, manage trailing stop
		if (Position > 0)
		{
			// Update trailing stop upward
			var candidate = close - atrValue * AtrMultiplier;
			if (candidate > _trailStop)
				_trailStop = candidate;

			// Exit if price falls below trailing stop
			if (close <= _trailStop)
			{
				SellMarket(Math.Abs(Position));
				_trailStop = 0;
				_entryPrice = 0;
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (_cooldownRemaining <= 0)
		{
			// Entry: breakout to new high
			if (close >= highestValue)
			{
				BuyMarket(Volume);
				_entryPrice = close;
				_trailStop = close - atrValue * AtrMultiplier;
				_cooldownRemaining = CooldownBars;
			}
		}
	}
}
