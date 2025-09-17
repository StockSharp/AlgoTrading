using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades Morning/Evening Star candlestick reversals confirmed by CCI.
/// Based on the MetaTrader 5 Expert Advisor "Expert_AMS_ES_CCI".
/// </summary>
public class MorningEveningStarCciStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _neutralThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;
	private SMA _bodyAverage = null!;
	private ICandleMessage? _olderCandle;
	private ICandleMessage? _middleCandle;
	private ICandleMessage? _latestCandle;
	private decimal? _previousCci;
	private decimal? _averageBodySize;

	/// <summary>
	/// Period for the CCI indicator.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Period for the candle body averaging window.
	/// </summary>
	public int BodyAveragePeriod
	{
		get => _bodyAveragePeriod.Value;
		set => _bodyAveragePeriod.Value = value;
	}

	/// <summary>
	/// Absolute CCI value required to confirm a new position.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Neutral zone threshold used to close open positions when CCI re-enters the range.
	/// </summary>
	public decimal NeutralThreshold
	{
		get => _neutralThreshold.Value;
		set => _neutralThreshold.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public MorningEveningStarCciStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 25)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Number of bars used in the CCI calculation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);

		_bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Body Average Period", "Number of candles used to measure average body size", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(3, 10, 1);

		_entryThreshold = Param(nameof(EntryThreshold), 50m)
		.SetGreaterThanZero()
		.SetDisplay("CCI Entry Threshold", "Absolute CCI value required for a new trade", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(30m, 80m, 10m);

		_neutralThreshold = Param(nameof(NeutralThreshold), 80m)
		.SetGreaterThanZero()
		.SetDisplay("CCI Neutral Threshold", "Absolute CCI level defining overbought/oversold exit zone", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(60m, 120m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series used by the strategy", "General");
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

		_olderCandle = null;
		_middleCandle = null;
		_latestCandle = null;
		_previousCci = null;
		_averageBodySize = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_bodyAverage = new SMA { Length = BodyAveragePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_cci, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Update average body size with the latest candle body length.
		var bodyLength = Math.Abs(candle.OpenPrice - candle.ClosePrice);
		var bodyValue = _bodyAverage.Process(new DecimalIndicatorValue(_bodyAverage, bodyLength, candle.OpenTime, true));
		_averageBodySize = bodyValue is DecimalIndicatorValue { IsFinal: true, Value: var avg } ? avg : null;

		// Shift stored candles to keep the last three finished bars.
		_olderCandle = _middleCandle;
		_middleCandle = _latestCandle;
		_latestCandle = candle;

		var previousCci = _previousCci;
		_previousCci = cciValue;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_cci.IsFormed || _averageBodySize is not { } averageBody || averageBody <= 0)
		return;

		if (_olderCandle == null || _middleCandle == null || _latestCandle == null)
		return;

		var hasMorningStar = CheckMorningStar(_olderCandle, _middleCandle, _latestCandle, averageBody);
		var hasEveningStar = CheckEveningStar(_olderCandle, _middleCandle, _latestCandle, averageBody);

		if (Position < 0 && previousCci.HasValue)
		{
			var exitShort =
			(cciValue > -NeutralThreshold && previousCci.Value < -NeutralThreshold) ||
			(cciValue < NeutralThreshold && previousCci.Value > NeutralThreshold);

			if (exitShort)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}

		if (Position > 0 && previousCci.HasValue)
		{
			var exitLong =
			(cciValue < NeutralThreshold && previousCci.Value > NeutralThreshold) ||
			(cciValue < -NeutralThreshold && previousCci.Value > -NeutralThreshold);

			if (exitLong)
			{
				SellMarket(Position);
				return;
			}
		}

		if (hasMorningStar && cciValue < -EntryThreshold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			return;
		}

		if (hasEveningStar && cciValue > EntryThreshold && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}

	private static bool CheckMorningStar(ICandleMessage first, ICandleMessage second, ICandleMessage third, decimal averageBody)
	{
		// First candle should be a strong bearish body.
		var firstBody = first.OpenPrice - first.ClosePrice;
		if (firstBody <= averageBody)
		return false;

		// Second candle is a short-bodied continuation lower.
		var secondBody = Math.Abs(second.ClosePrice - second.OpenPrice);
		if (secondBody >= averageBody * 0.5m)
		return false;

		if (!(second.ClosePrice < first.ClosePrice && second.OpenPrice < first.OpenPrice))
		return false;

		// Third candle must close above the midpoint of the first candle.
		var firstMidpoint = (first.OpenPrice + first.ClosePrice) / 2m;
		return third.ClosePrice > firstMidpoint;
	}

	private static bool CheckEveningStar(ICandleMessage first, ICandleMessage second, ICandleMessage third, decimal averageBody)
	{
		// First candle should be a strong bullish body.
		var firstBody = first.ClosePrice - first.OpenPrice;
		if (firstBody <= averageBody)
		return false;

		// Second candle is a small indecision bar after the up thrust.
		var secondBody = Math.Abs(second.ClosePrice - second.OpenPrice);
		if (secondBody >= averageBody * 0.5m)
		return false;

		if (!(second.ClosePrice > first.ClosePrice && second.OpenPrice > first.OpenPrice))
		return false;

		// Third candle must close below the midpoint of the first candle.
		var firstMidpoint = (first.OpenPrice + first.ClosePrice) / 2m;
		return third.ClosePrice < firstMidpoint;
	}
}
