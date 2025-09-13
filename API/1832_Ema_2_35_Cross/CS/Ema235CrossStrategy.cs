using System;
using System.Collections.Generic;

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
		_stopLoss = Param(nameof(StopLoss), 50m).SetGreaterThan(0m).SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 150m).SetGreaterThan(0m).SetDisplay("Take Profit", "Take-profit in price steps", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 50m).SetGreaterThanOrEqual(0m).SetDisplay("Trailing Stop", "Trailing stop in price steps", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
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
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var fastEma = new EMA { Length = FastLength };
		var slowEma = new EMA { Length = SlowLength };

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			if (!double.IsNaN((double)fastValue) && !double.IsNaN((double)slowValue))
			{
				_wasFastBelowSlow = fastValue < slowValue;
				_isInitialized = true;
			}
			return;
		}

		var isFastBelowSlow = fastValue < slowValue;

		if (_wasFastBelowSlow && !isFastBelowSlow)
		{
			if (Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss * Security.PriceStep;
				_takePrice = _entryPrice + TakeProfit * Security.PriceStep;
			}
		}
		else if (!_wasFastBelowSlow && isFastBelowSlow)
		{
			if (Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss * Security.PriceStep;
				_takePrice = _entryPrice - TakeProfit * Security.PriceStep;
			}
		}

		if (Position > 0)
		{
			if (TrailingStop > 0m)
			{
				var newStop = candle.ClosePrice - TrailingStop * Security.PriceStep;
				if (newStop > _stopPrice)
					_stopPrice = newStop;
			}

			if (_stopPrice != 0m && candle.ClosePrice <= _stopPrice)
				SellMarket(Math.Abs(Position));
			else if (_takePrice != 0m && candle.ClosePrice >= _takePrice)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (TrailingStop > 0m)
			{
				var newStop = candle.ClosePrice + TrailingStop * Security.PriceStep;
				if (_stopPrice == 0m || newStop < _stopPrice)
					_stopPrice = newStop;
			}

			if (_stopPrice != 0m && candle.ClosePrice >= _stopPrice)
				BuyMarket(Math.Abs(Position));
			else if (_takePrice != 0m && candle.ClosePrice <= _takePrice)
				BuyMarket(Math.Abs(Position));
		}

		_wasFastBelowSlow = isFastBelowSlow;
	}
}
