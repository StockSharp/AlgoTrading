using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractal AMA MBK crossover strategy.
/// Uses FRAMA and a signal EMA to generate trade signals.
/// </summary>
public class FractalAmaMbkStrategy : Strategy
{
	private readonly StrategyParam<int> _framaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLongPosition;
	private decimal _prevFrama;
	private decimal _prevSignal;
	private bool _isInitialized;

	/// <summary>
	/// FRAMA calculation period.
	/// </summary>
	public int FramaPeriod
	{
		get => _framaPeriod.Value;
		set => _framaPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FractalAmaMbkStrategy()
	{
		_framaPeriod = Param(nameof(FramaPeriod), 16)
			.SetGreaterThanZero()
			.SetDisplay("FRAMA Period", "Period for Fractal Adaptive Moving Average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(8, 32, 4);

		_signalPeriod = Param(nameof(SignalPeriod), 16)
			.SetGreaterThanZero()
			.SetDisplay("Signal EMA Period", "Period for signal EMA", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(8, 32, 4);

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_entryPrice = 0;
		_isLongPosition = false;
		_prevFrama = 0;
		_prevSignal = 0;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var frama = new FractalAdaptiveMovingAverage
		{
			Length = FramaPeriod
		};

		var signal = new ExponentialMovingAverage
		{
			Length = SignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(frama, signal, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, frama);
			DrawIndicator(area, signal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal framaValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevFrama = framaValue;
			_prevSignal = signalValue;
			_isInitialized = true;
			return;
		}

		var wasFramaAbove = _prevFrama > _prevSignal;
		var isFramaAbove = framaValue > signalValue;

		if (wasFramaAbove != isFramaAbove)
		{
			_entryPrice = candle.ClosePrice;
			if (isFramaAbove)
			{
				if (Position <= 0)
				{
					_isLongPosition = true;
					BuyMarket(Volume + Math.Abs(Position));
				}
			}
			else
			{
				if (Position >= 0)
				{
					_isLongPosition = false;
					SellMarket(Volume + Math.Abs(Position));
				}
			}
		}

		if (Position != 0 && _entryPrice != 0)
			CheckStops(candle.ClosePrice);

		_prevFrama = framaValue;
		_prevSignal = signalValue;
	}

	private void CheckStops(decimal currentPrice)
	{
		if (StopLoss > 0)
		{
			if (_isLongPosition && currentPrice <= _entryPrice - StopLoss)
				SellMarket(Math.Abs(Position));
			else if (!_isLongPosition && currentPrice >= _entryPrice + StopLoss)
				BuyMarket(Math.Abs(Position));
		}

		if (TakeProfit > 0)
		{
			if (_isLongPosition && currentPrice >= _entryPrice + TakeProfit)
				SellMarket(Math.Abs(Position));
			else if (!_isLongPosition && currentPrice <= _entryPrice - TakeProfit)
				BuyMarket(Math.Abs(Position));
		}
	}
}
