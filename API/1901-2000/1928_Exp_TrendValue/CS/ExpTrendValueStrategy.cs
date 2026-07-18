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
/// Strategy based on the TrendValue indicator combining weighted moving averages and ATR.
/// </summary>
public class ExpTrendValueStrategy : Strategy
{
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _shiftPercent;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrSensitivity;
	private readonly StrategyParam<DataType> _candleType;

	private readonly WeightedMovingAverage _wmaHigh = new();
	private readonly WeightedMovingAverage _wmaLow = new();
	private readonly SimpleMovingAverage _rangeAverage = new();
	private decimal _prevHighBand;
	private decimal _prevLowBand;
	private int _prevTrend;
	private bool _initialized;



	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	/// <summary>
	/// Stop loss size in points.
	/// </summary>
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	/// <summary>
	/// Take profit size in points.
	/// </summary>
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	/// <summary>
	/// Weighted moving average period.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Percentage offset applied to moving averages.
	/// </summary>
	public decimal ShiftPercent { get => _shiftPercent.Value; set => _shiftPercent.Value = value; }

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Multiplier for ATR shift.
	/// </summary>
	public decimal AtrSensitivity { get => _atrSensitivity.Value; set => _atrSensitivity.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ExpTrendValueStrategy"/>.
	/// </summary>
	public ExpTrendValueStrategy()
	{
		_buyPosOpen = Param(nameof(BuyPosOpen), true).SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading");
		_sellPosOpen = Param(nameof(SellPosOpen), true).SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading");
		_buyPosClose = Param(nameof(BuyPosClose), true).SetDisplay("Allow Long Exit", "Allow closing long positions", "Trading");
		_sellPosClose = Param(nameof(SellPosClose), true).SetDisplay("Allow Short Exit", "Allow closing short positions", "Trading");
		_stopLossPips = Param(nameof(StopLossPips), 1000).SetGreaterThanZero().SetDisplay("Stop Loss", "Stop loss in points", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 2000).SetGreaterThanZero().SetDisplay("Take Profit", "Take profit in points", "Risk");
		_maPeriod = Param(nameof(MaPeriod), 13).SetGreaterThanZero().SetDisplay("MA Period", "Weighted moving average period", "Indicator");
		_shiftPercent = Param(nameof(ShiftPercent), 0.05m).SetDisplay("Shift Percent", "Percentage offset for bands", "Indicator");
		_atrPeriod = Param(nameof(AtrPeriod), 15).SetGreaterThanZero().SetDisplay("ATR Period", "Range average period", "Indicator");
		_atrSensitivity = Param(nameof(AtrSensitivity), 0.6m).SetGreaterThanZero().SetDisplay("ATR Sensitivity", "Multiplier for range shift", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe for calculations", "General");
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

		_wmaHigh.Reset();
		_wmaHigh.Length = 1;
		_wmaLow.Reset();
		_wmaLow.Length = 1;
		_rangeAverage.Reset();
		_rangeAverage.Length = 1;
		_prevHighBand = _prevLowBand = 0m;
		_prevTrend = 0;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_wmaHigh.Length = MaPeriod;
		_wmaLow.Length = MaPeriod;
		_rangeAverage.Length = AtrPeriod;
		var closeTrigger = new SimpleMovingAverage { Length = 1 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(closeTrigger, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highMa = _wmaHigh.Process(new DecimalIndicatorValue(_wmaHigh, candle.HighPrice, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var lowMa = _wmaLow.Process(new DecimalIndicatorValue(_wmaLow, candle.LowPrice, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_wmaHigh.IsFormed || !_wmaLow.IsFormed)
			return;

		var rangeValue = _rangeAverage.Process(new DecimalIndicatorValue(_rangeAverage, candle.HighPrice - candle.LowPrice, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_rangeAverage.IsFormed)
			return;

		var percentOffset = closeValue * ShiftPercent / 100m;
		var rangeOffset = rangeValue * AtrSensitivity * 0.25m;
		var highBand = highMa - rangeOffset + percentOffset;
		var lowBand = lowMa + rangeOffset - percentOffset;

		if (!_initialized)
		{
			_prevHighBand = highBand;
			_prevLowBand = lowBand;
			_initialized = true;
			return;
		}

		var centerLine = (highBand + lowBand) / 2m;
		var trend = candle.ClosePrice >= centerLine ? 1 : -1;
		var upSignal = trend > 0 && _prevTrend <= 0;
		var downSignal = trend < 0 && _prevTrend >= 0;
		var haveUpTrend = trend > 0;
		var haveDownTrend = trend < 0;

		_prevHighBand = highBand;
		_prevLowBand = lowBand;
		_prevTrend = trend;

		if (upSignal && Position == 0)
			BuyMarket();
		else if (downSignal && Position == 0)
			SellMarket();
	}
}
