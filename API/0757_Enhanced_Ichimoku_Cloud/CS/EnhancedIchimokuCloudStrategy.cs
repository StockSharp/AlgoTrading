using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enhanced Ichimoku Cloud strategy with 171-day EMA filter.
/// Enters long when span A is above span B, price breaks above
/// the high 25 bars ago, Tenkan-sen above Kijun-sen and close above EMA.
/// Exits when Tenkan-sen crosses below Kijun-sen.
/// </summary>
public enum TradeMode
{
	Ichi,
	Cloud
}

public class EnhancedIchimokuCloudStrategy : Strategy
{
	private readonly StrategyParam<int> _conversionPeriods;
	private readonly StrategyParam<int> _basePeriods;
	private readonly StrategyParam<int> _laggingSpan2Periods;
	private readonly StrategyParam<int> _displacement;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<TradeMode> _modeParam;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _spanAValues = [];
	private readonly List<decimal> _spanBValues = [];
	private readonly List<decimal> _highValues = [];

	private decimal _prevSpanA;
	private decimal _prevSpanB;
	private bool _buyMem;
	private bool _sellMem;

	public int ConversionPeriods { get => _conversionPeriods.Value; set => _conversionPeriods.Value = value; }
	public int BasePeriods { get => _basePeriods.Value; set => _basePeriods.Value = value; }
	public int LaggingSpan2Periods { get => _laggingSpan2Periods.Value; set => _laggingSpan2Periods.Value = value; }
	public int Displacement { get => _displacement.Value; set => _displacement.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public TradeMode Mode { get => _modeParam.Value; set => _modeParam.Value = value; }
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EnhancedIchimokuCloudStrategy()
	{
		_conversionPeriods = Param(nameof(ConversionPeriods), 7)
			.SetDisplay("Conversion Line Periods", "Tenkan-sen period", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(5, 9, 1);

		_basePeriods = Param(nameof(BasePeriods), 211)
			.SetDisplay("Base Line Periods", "Kijun-sen period", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(180, 240, 10);

		_laggingSpan2Periods = Param(nameof(LaggingSpan2Periods), 120)
			.SetDisplay("Lagging Span 2 Periods", "Senkou Span B period", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(100, 140, 10);

		_displacement = Param(nameof(Displacement), 41)
			.SetDisplay("Displacement", "Forward shift", "Ichimoku");

		_emaPeriod = Param(nameof(EmaPeriod), 171)
			.SetDisplay("EMA Period", "EMA filter period", "EMA")
			.SetCanOptimize(true)
			.SetOptimize(150, 200, 5);

		_modeParam = Param(nameof(Mode), TradeMode.Ichi)
			.SetDisplay("Trade Setup", "Trading logic", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Start", "Date Range");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(2069, 12, 31, 23, 59, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "End", "Date Range");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_spanAValues.Clear();
		_spanBValues.Clear();
		_highValues.Clear();
		_prevSpanA = default;
		_prevSpanB = default;
		_buyMem = default;
		_sellMem = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = ConversionPeriods },
			Kijun = { Length = BasePeriods },
			SenkouB = { Length = LaggingSpan2Periods }
		};

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		if (time < StartDate || time > EndDate)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ich = (IchimokuValue)ichimokuValue;
		if (ich.Tenkan is not decimal conversion ||
			ich.Kijun is not decimal baseLine ||
			ich.SenkouA is not decimal spanA ||
			ich.SenkouB is not decimal spanB)
			return;

		if (!emaValue.IsFinal)
			return;

		var ema = emaValue.GetValue<decimal>();

		var maxLag = Math.Max(Displacement, 26);

		_spanAValues.Add(spanA);
		if (_spanAValues.Count > maxLag)
			_spanAValues.RemoveAt(0);

		_spanBValues.Add(spanB);
		if (_spanBValues.Count > maxLag)
			_spanBValues.RemoveAt(0);

		_highValues.Add(candle.HighPrice);
		if (_highValues.Count > 26)
			_highValues.RemoveAt(0);

		var spanAOffsetIdx = _spanAValues.Count - Displacement;
		var spanAOffset = spanAOffsetIdx >= 0 ? _spanAValues[spanAOffsetIdx] : 0m;

		var spanBOffsetIdx = _spanBValues.Count - Displacement;
		var spanBOffset = spanBOffsetIdx >= 0 ? _spanBValues[spanBOffsetIdx] : 0m;

		var highIdx = _highValues.Count - 26;
		var highLag = highIdx >= 0 ? _highValues[highIdx] : 0m;

		var idealbuy = spanAOffset > spanBOffset &&
			candle.ClosePrice > highLag &&
			conversion > baseLine &&
			candle.ClosePrice > ema;

		var idealsell = conversion < baseLine;

		var prevBuyMem = _buyMem;
		var prevSellMem = _sellMem;

		if (idealbuy)
			_buyMem = true;
		else if (idealsell)
			_buyMem = false;

		if (idealsell)
			_sellMem = true;
		else if (idealbuy)
			_sellMem = false;

		var longCond = idealbuy && !prevBuyMem;
		var shortCond = idealsell && !prevSellMem;

		bool buySignal;
		bool sellSignal;

		if (Mode == TradeMode.Ichi)
		{
			buySignal = longCond;
			sellSignal = shortCond;
		}
		else
		{
			var spanA25Idx = _spanAValues.Count - 26;
			var spanA25 = spanA25Idx >= 0 ? _spanAValues[spanA25Idx] : 0m;

			var spanB25Idx = _spanBValues.Count - 26;
			var spanB25 = spanB25Idx >= 0 ? _spanBValues[spanB25Idx] : 0m;

			var crossUp = _prevSpanA <= _prevSpanB && spanA > spanB;
			var crossDown = _prevSpanA >= _prevSpanB && spanA < spanB;

			buySignal = crossUp && candle.LowPrice > spanA25 && candle.LowPrice > spanB25 && candle.ClosePrice > ema;
			sellSignal = crossDown && candle.HighPrice < spanA25 && candle.HighPrice < spanB25 && candle.ClosePrice < ema;
		}

		_prevSpanA = spanA;
		_prevSpanB = spanB;

		if (Position == 0 && buySignal)
		{
			BuyMarket();
		}
		else if (Position > 0 && sellSignal)
		{
			SellMarket(Position);
		}
	}
}
