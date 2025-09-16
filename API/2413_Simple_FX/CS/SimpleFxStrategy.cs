using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple trend following strategy based on two exponential moving averages.
/// Opens a long position when the short EMA crosses above the long EMA and a short position when it crosses below.
/// Optional stop loss and take profit are specified in price steps.
/// </summary>
public class SimpleFxStrategy : Strategy
{
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private int _lastTrend; // 1 for bull, -1 for bear, 0 for none

	/// <summary>
	/// Period of the long EMA.
	/// </summary>
	public int LongMaPeriod
	{
		get => _longMaPeriod.Value;
		set => _longMaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the short EMA.
	/// </summary>
	public int ShortMaPeriod
	{
		get => _shortMaPeriod.Value;
		set => _shortMaPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public SimpleFxStrategy()
	{
		_longMaPeriod = Param(nameof(LongMaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Long MA Period", "Period of the long EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_shortMaPeriod = Param(nameof(ShortMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Short MA Period", "Period of the short EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_stopLoss = Param(nameof(StopLoss), 30)
			.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 50)
			.SetDisplay("Take Profit", "Take profit in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_lastTrend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var longMa = new EMA { Length = LongMaPeriod };
		var shortMa = new EMA { Length = ShortMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(longMa, shortMa, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, longMa);
			DrawIndicator(area, shortMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal longValue, decimal shortValue)
	{
		// Only process finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure indicators are formed and trading is allowed
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trend = 0;
		if (shortValue > longValue)
			trend = 1;
		else if (shortValue < longValue)
			trend = -1;

		if (trend == 0)
			return;

		if (trend != _lastTrend)
		{
			if (trend == 1)
			{
				// Bullish trend - open long
				BuyMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
			}
			else
			{
				// Bearish trend - open short
				SellMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
			}

			_lastTrend = trend;
			return;
		}

		if (Position == 0 || _entryPrice == 0m)
			return;

		var stopDelta = StopLoss * Security.PriceStep;
		var takeDelta = TakeProfit * Security.PriceStep;

		if (Position > 0)
		{
			// Manage long position
			if (StopLoss > 0 && candle.ClosePrice <= _entryPrice - stopDelta)
			{
				SellMarket(Math.Abs(Position));
				_lastTrend = 0;
				_entryPrice = 0m;
				return;
			}

			if (TakeProfit > 0 && candle.ClosePrice >= _entryPrice + takeDelta)
			{
				SellMarket(Math.Abs(Position));
				_lastTrend = 0;
				_entryPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			// Manage short position
			if (StopLoss > 0 && candle.ClosePrice >= _entryPrice + stopDelta)
			{
				BuyMarket(Math.Abs(Position));
				_lastTrend = 0;
				_entryPrice = 0m;
				return;
			}

			if (TakeProfit > 0 && candle.ClosePrice <= _entryPrice - takeDelta)
			{
				BuyMarket(Math.Abs(Position));
				_lastTrend = 0;
				_entryPrice = 0m;
			}
		}
	}
}
