using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Karacatica strategy uses ADX to determine trend direction and compares
/// current close price with the close price from a specified period ago.
/// It goes long when an uptrend is detected and price is rising, and
/// goes short when a downtrend is detected and price is falling.
/// </summary>
public class KaracaticaStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;
	private readonly Queue<decimal> _closeQueue = new();
	private int _lastSignal;

	/// <summary>
	/// Indicator period used for ADX and price comparison.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Take-profit percentage parameter.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage parameter.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public KaracaticaStrategy()
	{
		_period = Param(nameof(Period), 70)
			.SetGreaterThanZero()
			.SetDisplay("Period", "ADX period and lookback for close comparison", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 100, 10);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetDisplay("Take Profit %", "Take-profit as percentage of entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop-loss as percentage of entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_adx = null;
		_closeQueue.Clear();
		_lastSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_adx = new AverageDirectionalIndex { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closeQueue.Enqueue(candle.ClosePrice);
		if (_closeQueue.Count > Period + 1)
			_closeQueue.Dequeue();

		if (_closeQueue.Count <= Period)
			return;

		var pastClose = _closeQueue.Peek();

		var typedAdx = (AverageDirectionalIndexValue)adxValue;
		var plusDi = typedAdx.PlusDI;
		var minusDi = typedAdx.MinusDI;

		var buySignal = candle.ClosePrice > pastClose && plusDi > minusDi && _lastSignal != 1;
		var sellSignal = candle.ClosePrice < pastClose && minusDi > plusDi && _lastSignal != -1;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (buySignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_lastSignal = 1;
		}
		else if (sellSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_lastSignal = -1;
		}
	}
}
