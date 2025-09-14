using System;
using System.Collections.Generic;

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
	private decimal _prevHighBand;
	private decimal _prevLowBand;
	private int _prevTrend;
	private bool _initialized;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

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
		_shiftPercent = Param(nameof(ShiftPercent), 0m).SetDisplay("Shift Percent", "Percentage offset for bands", "Indicator");
		_atrPeriod = Param(nameof(AtrPeriod), 15).SetGreaterThanZero().SetDisplay("ATR Period", "ATR calculation period", "Indicator");
		_atrSensitivity = Param(nameof(AtrSensitivity), 1.5m).SetGreaterThanZero().SetDisplay("ATR Sensitivity", "Multiplier for ATR shift", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		_wmaLow.Reset();
		_prevHighBand = _prevLowBand = 0m;
		_prevTrend = 0;
		_initialized = false;
		_entryPrice = _stopPrice = _takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_wmaHigh.Length = MaPeriod;
		_wmaLow.Length = MaPeriod;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var highMa = _wmaHigh.Process(candle.HighPrice).ToDecimal();
		var lowMa = _wmaLow.Process(candle.LowPrice).ToDecimal();

		if (!_wmaHigh.IsFormed || !_wmaLow.IsFormed)
			return;

		var highBand = highMa * (1 + ShiftPercent / 100m) + atrValue * AtrSensitivity;
		var lowBand = lowMa * (1 - ShiftPercent / 100m) - atrValue * AtrSensitivity;

		if (!_initialized)
		{
			_prevHighBand = highBand;
			_prevLowBand = lowBand;
			_initialized = true;
			return;
		}

		var trend = _prevTrend;

		if (candle.ClosePrice > _prevHighBand)
			trend = 1;
		else if (candle.ClosePrice < _prevLowBand)
			trend = -1;

		var upSignal = false;
		var downSignal = false;
		var haveUpTrend = false;
		var haveDownTrend = false;

		if (trend > 0)
		{
			lowBand = Math.Max(lowBand, _prevLowBand);
			haveUpTrend = true;
			if (_prevTrend <= 0)
				upSignal = true;
		}
		else if (trend < 0)
		{
			highBand = Math.Min(highBand, _prevHighBand);
			haveDownTrend = true;
			if (_prevTrend >= 0)
				downSignal = true;
		}

		_prevHighBand = highBand;
		_prevLowBand = lowBand;
		_prevTrend = trend;

		var step = Security.PriceStep ?? 1m;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || (TakeProfitPips > 0 && candle.HighPrice >= _takeProfitPrice))
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || (TakeProfitPips > 0 && candle.LowPrice <= _takeProfitPrice))
				BuyMarket(-Position);
		}

		var buyClose = BuyPosClose && (downSignal || haveDownTrend);
		var sellClose = SellPosClose && (upSignal || haveUpTrend);
		var buyOpen = BuyPosOpen && upSignal;
		var sellOpen = SellPosOpen && downSignal;

		if (buyClose && Position > 0)
			SellMarket(Position);

		if (sellClose && Position < 0)
			BuyMarket(-Position);

		if (buyOpen && Position <= 0)
		{
			var vol = Volume + (Position < 0 ? -Position : 0);
			BuyMarket(vol);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - StopLossPips * step;
			_takeProfitPrice = _entryPrice + TakeProfitPips * step;
		}
		else if (sellOpen && Position >= 0)
		{
			var vol = Volume + (Position > 0 ? Position : 0);
			SellMarket(vol);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + StopLossPips * step;
			_takeProfitPrice = _entryPrice - TakeProfitPips * step;
		}
	}
}
