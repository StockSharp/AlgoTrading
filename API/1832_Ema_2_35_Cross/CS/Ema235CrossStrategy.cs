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
/// Strategy based on crossover of EMA(2) and EMA(35).
/// Buys when the fast EMA crosses above the slow EMA and sells on opposite cross.
/// Includes fixed stop-loss, take-profit and trailing stop measured in price steps.
/// </summary>
public class Ema235CrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _wasFastBelowSlow;
	private bool _isInitialized;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Stop-loss in price steps.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take-profit in price steps.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Trailing stop in price steps.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes parameters with default values.
	/// </summary>
	public Ema235CrossStrategy()
	{
		_fastLength = Param(nameof(FastLength), 2).SetGreaterThanZero().SetDisplay("Fast EMA", "Length of the fast EMA", "Parameters");
		_slowLength = Param(nameof(SlowLength), 35).SetGreaterThanZero().SetDisplay("Slow EMA", "Length of the slow EMA", "Parameters");
		_stopLoss = Param(nameof(StopLoss), 50m).SetGreaterThanZero().SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 150m).SetGreaterThanZero().SetDisplay("Take Profit", "Take-profit in price steps", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 50m).SetNotNegative().SetDisplay("Trailing Stop", "Trailing stop in price steps", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
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
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(fastEma, slowEma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_wasFastBelowSlow = fastValue < slowValue;
			_isInitialized = true;
			return;
		}

		var step = Security.PriceStep ?? 1m;
		var isFastBelowSlow = fastValue < slowValue;

		// Check exits first
		if (Position > 0)
		{
			if (TrailingStop > 0m)
			{
				var newStop = candle.ClosePrice - TrailingStop * step;
				if (newStop > _stopPrice)
					_stopPrice = newStop;
			}

			if (_stopPrice != 0m && candle.ClosePrice <= _stopPrice)
			{
				SellMarket();
				_wasFastBelowSlow = isFastBelowSlow;
				return;
			}
			if (_takePrice != 0m && candle.ClosePrice >= _takePrice)
			{
				SellMarket();
				_wasFastBelowSlow = isFastBelowSlow;
				return;
			}
		}
		else if (Position < 0)
		{
			if (TrailingStop > 0m)
			{
				var newStop = candle.ClosePrice + TrailingStop * step;
				if (_stopPrice == 0m || newStop < _stopPrice)
					_stopPrice = newStop;
			}

			if (_stopPrice != 0m && candle.ClosePrice >= _stopPrice)
			{
				BuyMarket();
				_wasFastBelowSlow = isFastBelowSlow;
				return;
			}
			if (_takePrice != 0m && candle.ClosePrice <= _takePrice)
			{
				BuyMarket();
				_wasFastBelowSlow = isFastBelowSlow;
				return;
			}
		}

		// Entry signals
		if (_wasFastBelowSlow && !isFastBelowSlow && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - StopLoss * step;
			_takePrice = _entryPrice + TakeProfit * step;
		}
		else if (!_wasFastBelowSlow && isFastBelowSlow && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + StopLoss * step;
			_takePrice = _entryPrice - TakeProfit * step;
		}

		_wasFastBelowSlow = isFastBelowSlow;
	}
}
