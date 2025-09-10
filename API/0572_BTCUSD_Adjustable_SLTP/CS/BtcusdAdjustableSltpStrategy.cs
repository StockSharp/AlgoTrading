using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTCUSD strategy with adjustable take profit, stop loss and break-even.
/// SMA(10) crossing above SMA(25) activates a long pullback entry that triggers
/// when price retraces by a configurable percentage and then crosses above the retracement level.
/// Short entries occur immediately on SMA crossunder when price is below EMA(150).
/// </summary>
public class BtcusdAdjustableSltpStrategy : Strategy
{
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _emaFilterLength;
	private readonly StrategyParam<decimal> _takeProfitDistance;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _breakEvenTrigger;
	private readonly StrategyParam<decimal> _retracementPercentage;
	private readonly StrategyParam<DataType> _candleType;

	private bool _initialized;
	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevClose;

	private bool _longSignalActive;
	private decimal? _pullHigh;
	private decimal? _retraceLevel;

	private bool _beLong;
	private bool _beShort;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastSmaLength { get => _fastSmaLength.Value; set => _fastSmaLength.Value = value; }

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowSmaLength { get => _slowSmaLength.Value; set => _slowSmaLength.Value = value; }

	/// <summary>
	/// EMA filter length.
	/// </summary>
	public int EmaFilterLength { get => _emaFilterLength.Value; set => _emaFilterLength.Value = value; }

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfitDistance { get => _takeProfitDistance.Value; set => _takeProfitDistance.Value = value; }

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLossDistance { get => _stopLossDistance.Value; set => _stopLossDistance.Value = value; }

	/// <summary>
	/// Break-even trigger distance in points.
	/// </summary>
	public decimal BreakEvenTrigger { get => _breakEvenTrigger.Value; set => _breakEvenTrigger.Value = value; }

	/// <summary>
	/// Retracement percentage for long pullback entry.
	/// </summary>
	public decimal RetracementPercentage { get => _retracementPercentage.Value; set => _retracementPercentage.Value = value; }

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BtcusdAdjustableSltpStrategy()
	{
		_fastSmaLength = Param(nameof(FastSmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Length of fast SMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_slowSmaLength = Param(nameof(SlowSmaLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Length of slow SMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 1);

		_emaFilterLength = Param(nameof(EmaFilterLength), 150)
			.SetGreaterThanZero()
			.SetDisplay("EMA Filter", "Length of EMA filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(100, 200, 10);

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("TP Distance", "Take profit distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(200m, 2000m, 100m);

		_stopLossDistance = Param(nameof(StopLossDistance), 250m)
			.SetGreaterThanZero()
			.SetDisplay("SL Distance", "Stop loss distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 1000m, 50m);

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 500m)
			.SetGreaterThanZero()
			.SetDisplay("BE Trigger", "Break-even trigger distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 50m);

		_retracementPercentage = Param(nameof(RetracementPercentage), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Retracement %", "Retracement percentage for long entry", "Entries")
			.SetCanOptimize(true)
			.SetOptimize(0.005m, 0.05m, 0.005m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_initialized = false;
		_prevFast = 0m;
		_prevSlow = 0m;
		_prevClose = 0m;
		_longSignalActive = false;
		_pullHigh = null;
		_retraceLevel = null;
		_beLong = false;
		_beShort = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastSma = new SimpleMovingAverage { Length = FastSmaLength };
		var slowSma = new SimpleMovingAverage { Length = SlowSmaLength };
		var ema = new ExponentialMovingAverage { Length = EmaFilterLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var high = candle.HighPrice;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_prevClose = close;
			_initialized = true;
			return;
		}

		var longCondition = _prevFast <= _prevSlow && fast > slow;
		var shortCondition = _prevFast >= _prevSlow && fast < slow;
		var shortValid = close < ema;

		if (Position == 0)
		{
			if (longCondition)
			{
				_longSignalActive = true;
				_pullHigh = high;
			}

			if (_longSignalActive)
			{
				_pullHigh = Math.Max(_pullHigh ?? high, high);
				_retraceLevel = _pullHigh.Value * (1m - RetracementPercentage);

				if (close > _retraceLevel && _prevClose <= _retraceLevel)
				{
					BuyMarket();
					_longSignalActive = false;
				}
			}

			if (shortCondition && shortValid)
				SellMarket();
		}
		else if (Position > 0)
		{
			var entry = PositionPrice;

			if (shortCondition && shortValid)
			{
				RegisterSell(Position);
			}
			else
			{
				if (close >= entry + BreakEvenTrigger)
					_beLong = true;

				var effectiveLongStop = _beLong ? entry : entry - StopLossDistance;
				if (close <= effectiveLongStop)
				{
					RegisterSell(Position);
				}
				else if (close >= entry + TakeProfitDistance)
				{
					RegisterSell(Position);
				}
			}
		}
		else if (Position < 0)
		{
			var entry = PositionPrice;

			if (close <= entry - BreakEvenTrigger)
				_beShort = true;

			var effectiveShortStop = _beShort ? entry : entry + StopLossDistance;
			if (close >= effectiveShortStop)
			{
				RegisterBuy(Math.Abs(Position));
			}
			else if (close <= entry - TakeProfitDistance)
			{
				RegisterBuy(Math.Abs(Position));
			}
		}

		if (Position == 0)
		{
			_beLong = false;
			_beShort = false;

			if (!_longSignalActive)
			{
				_pullHigh = null;
				_retraceLevel = null;
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevClose = close;
	}
}
