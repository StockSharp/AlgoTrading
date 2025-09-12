using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend strategy with fixed take profit, optional price and time filters (MSK timezone).
/// </summary>
public class SupertrendFixedTpUnifiedWithTimeFilterMskStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<TradeMode> _tradeMode;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _usePriceFilter;
	private readonly StrategyParam<decimal> _priceFilter;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _timeFrom;
	private readonly StrategyParam<int> _timeTo;

	private bool? _lastUp;
	private decimal _tpLevel;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }
	public TradeMode Mode { get => _tradeMode.Value; set => _tradeMode.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public bool UsePriceFilter { get => _usePriceFilter.Value; set => _usePriceFilter.Value = value; }
	public decimal PriceFilter { get => _priceFilter.Value; set => _priceFilter.Value = value; }
	public bool UseTimeFilter { get => _useTimeFilter.Value; set => _useTimeFilter.Value = value; }
	public int TimeFrom { get => _timeFrom.Value; set => _timeFrom.Value = value; }
	public int TimeTo { get => _timeTo.Value; set => _timeTo.Value = value; }

	public SupertrendFixedTpUnifiedWithTimeFilterMskStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_atrPeriod = Param(nameof(AtrPeriod), 23)
			.SetDisplay("ATR Length", "ATR period", "Supertrend");
		_factor = Param(nameof(Factor), 1.8m)
			.SetDisplay("Factor", "ATR multiplier", "Supertrend");
		_tradeMode = Param(nameof(Mode), TradeMode.Both)
			.SetDisplay("Trade Mode", "Trading direction", "General");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1.5m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk");
		_usePriceFilter = Param(nameof(UsePriceFilter), false)
			.SetDisplay("Use Price Filter", "Enable price filter", "Filters");
		_priceFilter = Param(nameof(PriceFilter), 10000m)
			.SetDisplay("Price Filter", "Price threshold", "Filters");
		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Enable MSK time filter", "Filters");
		_timeFrom = Param(nameof(TimeFrom), 0)
			.SetDisplay("Time From", "Start hour MSK", "Filters");
		_timeTo = Param(nameof(TimeTo), 23)
			.SetDisplay("Time To", "End hour MSK", "Filters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_lastUp = null;
		_tpLevel = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var supertrend = new SuperTrend
		{
			Length = AtrPeriod,
			Multiplier = Factor
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(supertrend, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var st = (SuperTrendIndicatorValue)stValue;
		var isUp = st.IsUpTrend;
		var inTime = !UseTimeFilter || IsTimeInRange(candle.OpenTime);

		var longEntry = isUp && _lastUp == false && inTime && (!UsePriceFilter || candle.ClosePrice > PriceFilter);
		var shortEntry = !isUp && _lastUp == true && inTime && (!UsePriceFilter || candle.ClosePrice < PriceFilter);

		if (longEntry && Mode != TradeMode.ShortOnly)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_tpLevel = candle.ClosePrice * (1 + TakeProfitPercent / 100m);
		}
		else if (shortEntry && Mode != TradeMode.LongOnly)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_tpLevel = candle.ClosePrice * (1 - TakeProfitPercent / 100m);
		}

		if (Position > 0 && _tpLevel > 0m && candle.ClosePrice >= _tpLevel)
		{
			SellMarket(Position);
			_tpLevel = 0m;
		}
		else if (Position < 0 && _tpLevel > 0m && candle.ClosePrice <= _tpLevel)
		{
			BuyMarket(Math.Abs(Position));
			_tpLevel = 0m;
		}

		_lastUp = isUp;
	}

	private bool IsTimeInRange(DateTimeOffset time)
	{
		if (!UseTimeFilter)
			return true;

		var mskHour = time.ToOffset(TimeSpan.FromHours(3)).Hour;
		return TimeFrom <= TimeTo
			? mskHour >= TimeFrom && mskHour < TimeTo
			: mskHour >= TimeFrom || mskHour < TimeTo;
	}
}

public enum TradeMode
{
	Both,
	LongOnly,
	ShortOnly
}
