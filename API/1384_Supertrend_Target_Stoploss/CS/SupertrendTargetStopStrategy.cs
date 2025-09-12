using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend strategy with fixed percentage target and stop loss.
/// </summary>
public class SupertrendTargetStopStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _targetPct;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevIsPriceAboveSupertrend;
	private decimal _prevSupertrendValue;
	private decimal _entryPrice;

	/// <summary>
	/// ATR period for Supertrend calculation.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Multiplier for Supertrend calculation.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Target percentage from entry price.
	/// </summary>
	public decimal TargetPct
	{
		get => _targetPct.Value;
		set => _targetPct.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopPct
	{
		get => _stopPct.Value;
		set => _stopPct.Value = value;
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
	/// Initialize the strategy.
	/// </summary>
	public SupertrendTargetStopStrategy()
	{
		_period = Param(nameof(Period), 14)
			.SetDisplay("ATR Length", "ATR length for Supertrend", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_multiplier = Param(nameof(Multiplier), 3m)
			.SetDisplay("Multiplier", "Multiplier for Supertrend", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2m, 4m, 0.5m);

		_targetPct = Param(nameof(TargetPct), 0.01m)
			.SetDisplay("Target %", "Target percentage", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.005m, 0.02m, 0.005m);

		_stopPct = Param(nameof(StopPct), 0.01m)
			.SetDisplay("Stop %", "Stop loss percentage", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.005m, 0.02m, 0.005m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevIsPriceAboveSupertrend = false;
		_prevSupertrendValue = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = Period };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2;
		var basicUpperBand = medianPrice + Multiplier * atrValue;
		var basicLowerBand = medianPrice - Multiplier * atrValue;
		decimal supertrendValue;

		if (_prevSupertrendValue == 0m)
		{
			supertrendValue = candle.ClosePrice > medianPrice ? basicLowerBand : basicUpperBand;
			_prevSupertrendValue = supertrendValue;
			_prevIsPriceAboveSupertrend = candle.ClosePrice > supertrendValue;
			return;
		}

		if (_prevSupertrendValue <= candle.HighPrice)
			supertrendValue = Math.Max(basicLowerBand, _prevSupertrendValue);
		else if (_prevSupertrendValue >= candle.LowPrice)
			supertrendValue = Math.Min(basicUpperBand, _prevSupertrendValue);
		else
			supertrendValue = candle.ClosePrice > _prevSupertrendValue ? basicLowerBand : basicUpperBand;

		var isPriceAboveSupertrend = candle.ClosePrice > supertrendValue;
		var isCrossedAbove = isPriceAboveSupertrend && !_prevIsPriceAboveSupertrend;
		var isCrossedBelow = !isPriceAboveSupertrend && _prevIsPriceAboveSupertrend;

		if (isCrossedAbove && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
		}
		else if (isCrossedBelow && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
		}

		if (Position > 0 && _entryPrice > 0m)
		{
			var target = _entryPrice * (1 + TargetPct);
			var stop = _entryPrice * (1 - StopPct);

			if (candle.HighPrice >= target)
				SellMarket(Position);
			else if (candle.LowPrice <= stop)
				SellMarket(Position);
		}
		else if (Position < 0 && _entryPrice > 0m)
		{
			var target = _entryPrice * (1 - TargetPct);
			var stop = _entryPrice * (1 + StopPct);

			if (candle.LowPrice <= target)
				BuyMarket(Math.Abs(Position));
			else if (candle.HighPrice >= stop)
				BuyMarket(Math.Abs(Position));
		}

		_prevSupertrendValue = supertrendValue;
		_prevIsPriceAboveSupertrend = isPriceAboveSupertrend;
	}
}
