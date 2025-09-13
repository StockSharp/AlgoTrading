using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Charles strategy: EMA crossover with RSI filter and trailing stop.
/// </summary>
public class CharlesStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailStart;
	private readonly StrategyParam<decimal> _trailOffset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _peakPrice;
	private decimal _valleyPrice;
	private bool _wasFastBelowSlow;
	private bool _isInitialized;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Stop-loss in absolute price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take-profit in absolute price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Profit distance to start trailing.
	/// </summary>
	public decimal TrailStart { get => _trailStart.Value; set => _trailStart.Value = value; }

	/// <summary>
	/// Trailing stop distance.
	/// </summary>
	public decimal TrailOffset { get => _trailOffset.Value; set => _trailOffset.Value = value; }

	/// <summary>
	/// Candle type used for indicators.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public CharlesStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Period of the fast EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_slowPeriod = Param(nameof(SlowPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Period of the slow EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 100, 5);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_stopLoss = Param(nameof(StopLoss), 0.008m)
			.SetGreaterThan(0m)
			.SetDisplay("Stop Loss", "Absolute stop-loss value", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.004m, 0.02m, 0.004m);

		_takeProfit = Param(nameof(TakeProfit), 0.02m)
			.SetGreaterThan(0m)
			.SetDisplay("Take Profit", "Absolute take-profit value", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_trailStart = Param(nameof(TrailStart), 0.006m)
			.SetGreaterThan(0m)
			.SetDisplay("Trail Start", "Profit to activate trailing", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.004m, 0.02m, 0.004m);

		_trailOffset = Param(nameof(TrailOffset), 0.003m)
			.SetGreaterThan(0m)
			.SetDisplay("Trail Offset", "Distance of trailing stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.002m, 0.01m, 0.002m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
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
		_peakPrice = 0m;
		_valleyPrice = 0m;
		_wasFastBelowSlow = false;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));

		var fast = new EMA { Length = FastPeriod };
		var slow = new EMA { Length = SlowPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_wasFastBelowSlow = fastValue < slowValue;
			_isInitialized = true;
			return;
		}

		var isFastBelowSlow = fastValue < slowValue;

		// Long signal
		if (_wasFastBelowSlow && !isFastBelowSlow && rsiValue > 55 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_peakPrice = candle.ClosePrice;
		}
		// Short signal
		else if (!_wasFastBelowSlow && isFastBelowSlow && rsiValue < 45 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_valleyPrice = candle.ClosePrice;
		}

		_wasFastBelowSlow = isFastBelowSlow;

		// Trailing stop management
		if (Position > 0)
		{
			if (candle.ClosePrice > _peakPrice)
				_peakPrice = candle.ClosePrice;

			if (candle.ClosePrice >= _entryPrice + TrailStart &&
				candle.ClosePrice <= _peakPrice - TrailOffset)
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice < _valleyPrice)
				_valleyPrice = candle.ClosePrice;

			if (candle.ClosePrice <= _entryPrice - TrailStart &&
				candle.ClosePrice >= _valleyPrice + TrailOffset)
			{
				BuyMarket();
			}
		}
	}
}

