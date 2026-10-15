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
/// StockSharp port of the MetaTrader "SimpleTrade" expert advisor.
/// Compares the opening price of the current bar with the bar from several periods ago and flips the position accordingly.
/// </summary>
public class SimpleTradeFlipStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<int> _lookbackBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _openHistory = new();
	private int _cooldown;

	/// <summary>
	/// Initializes a new instance of the <see cref="SimpleTradeFlipStrategy"/> class.
	/// </summary>
	public SimpleTradeFlipStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order size in lots", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 120m)
			.SetNotNegative()
			.SetDisplay("Stop-Loss Points", "Protective stop distance expressed in instrument points", "Risk");

		_lookbackBars = Param(nameof(LookbackBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Bars", "Number of bars used for the open price comparison", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for signal calculations", "General");
	}

	/// <summary>
	/// Order size submitted with each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Number of historical bars used for the open price comparison.
	/// </summary>
	public int LookbackBars
	{
		get => _lookbackBars.Value;
		set => _lookbackBars.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Candle type that defines the working timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openHistory.Clear();
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 0m;
		Unit stopLossUnit = null;

		if (StopLossPoints > 0m && step > 0m)
			stopLossUnit = new Unit(StopLossPoints * step, UnitTypes.Absolute);

		StartProtection(null, stopLossUnit);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the open price for future comparisons.
		_openHistory.Add(candle.OpenPrice);

		var maxHistory = Math.Max(LookbackBars + 5, 5);
		if (_openHistory.Count > maxHistory)
			_openHistory.RemoveRange(0, _openHistory.Count - maxHistory);

		var lookback = LookbackBars;
		if (_openHistory.Count <= lookback)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		var currentOpen = candle.OpenPrice;
		var referenceOpen = _openHistory[^(lookback + 1)];

		// Only trade on clear directional difference
		var diff = currentOpen - referenceOpen;
		if (Math.Abs(diff) < currentOpen * 0.001m)
			return;

		if (diff > 0 && Position <= 0)
		{
			if (Position < 0m)
				BuyMarket(Math.Abs(Position));
			BuyMarket(volume);
			_cooldown = 5;
		}
		else if (diff < 0 && Position >= 0)
		{
			if (Position > 0m)
				SellMarket(Position);
			SellMarket(volume);
			_cooldown = 5;
		}
	}
}
