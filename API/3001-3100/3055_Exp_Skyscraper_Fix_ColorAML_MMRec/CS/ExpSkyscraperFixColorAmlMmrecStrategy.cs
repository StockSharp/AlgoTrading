namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class ExpSkyscraperFixColorAmlMmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<int> _filterPeriod;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;

	private decimal? _previousTrend;
	private decimal? _previousClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int TrendPeriod { get => _trendPeriod.Value; set => _trendPeriod.Value = value; }
	public int FilterPeriod { get => _filterPeriod.Value; set => _filterPeriod.Value = value; }
	public decimal UpperLevel { get => _upperLevel.Value; set => _upperLevel.Value = value; }
	public decimal LowerLevel { get => _lowerLevel.Value; set => _lowerLevel.Value = value; }

	public ExpSkyscraperFixColorAmlMmrecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_trendPeriod = Param(nameof(TrendPeriod), 18).SetGreaterThanZero().SetDisplay("Trend Period", "Trend WMA period", "Indicators");
		_filterPeriod = Param(nameof(FilterPeriod), 12).SetGreaterThanZero().SetDisplay("Filter Period", "DeMarker period", "Indicators");
		_upperLevel = Param(nameof(UpperLevel), 0.55m).SetDisplay("Upper Level", "Buy filter level", "Signals");
		_lowerLevel = Param(nameof(LowerLevel), 0.45m).SetDisplay("Lower Level", "Sell filter level", "Signals");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousTrend = null;
		_previousClose = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousTrend = null;
		_previousClose = null;

		var trend = new WeightedMovingAverage { Length = TrendPeriod };
		var filter = new DeMarker { Length = FilterPeriod };
		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(trend, filter, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal trendValue, decimal filterValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previousClose = _previousClose;
		var previousTrend = _previousTrend;

		_previousClose = candle.ClosePrice;
		_previousTrend = trendValue;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (previousClose is null || previousTrend is null)
			return;

		var crossedUp = previousClose.Value <= previousTrend.Value && candle.ClosePrice > trendValue;
		var crossedDown = previousClose.Value >= previousTrend.Value && candle.ClosePrice < trendValue;
		var buySignal = crossedUp && filterValue >= UpperLevel;
		var sellSignal = crossedDown && filterValue <= LowerLevel;

		if (buySignal && Position <= 0)
		{
			if (Position < 0)
			{
				BuyMarket();
				return;
			}

			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0)
			{
				SellMarket();
				return;
			}

			SellMarket();
		}
	}
}
