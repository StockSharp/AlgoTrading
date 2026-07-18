using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified currency strength strategy based on RSI.
/// </summary>
public class Cspa143Strategy : Strategy
{
	private readonly StrategyParam<int> _strengthPeriod;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousRsi;
	private bool _isInitialized;
	private int _barsSinceTrade;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int StrengthPeriod
	{
		get => _strengthPeriod.Value;
		set => _strengthPeriod.Value = value;
	}

	/// <summary>
	/// RSI distance from 50.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Bars to wait after a completed trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public Cspa143Strategy()
	{
		_strengthPeriod = Param(nameof(StrengthPeriod), 14)
			.SetDisplay("Strength Period", "RSI period", "Parameters");

		_threshold = Param(nameof(Threshold), 18m)
			.SetDisplay("Threshold", "RSI distance from 50", "Parameters")
			.SetGreaterThanZero();

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousRsi = 0m;
		_isInitialized = false;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = StrengthPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;

		var upper = 50m + Threshold;
		var lower = 50m - Threshold;

		if (!_isInitialized)
		{
			_previousRsi = rsi;
			_isInitialized = true;
			return;
		}

		var longEntry = _previousRsi <= upper && rsi > upper;
		var shortEntry = _previousRsi >= lower && rsi < lower;
		var longExit = Position > 0 && _previousRsi >= 55m && rsi < 55m;
		var shortExit = Position < 0 && _previousRsi <= 45m && rsi > 45m;

		if (longExit)
		{
			SellMarket(Position);
			_barsSinceTrade = 0;
		}
		else if (shortExit)
		{
			BuyMarket(-Position);
			_barsSinceTrade = 0;
		}
		else if (_barsSinceTrade >= CooldownBars)
		{
			if (longEntry && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
			else if (shortEntry && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
		}

		_previousRsi = rsi;
	}
}
