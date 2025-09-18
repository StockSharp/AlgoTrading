using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the 3 Black Crows / 3 White Soldiers candle patterns with CCI confirmation.
/// The strategy looks for three consecutive strong candles and confirms the entry with CCI levels.
/// Positions are closed when CCI leaves extreme zones.
/// </summary>
public class AbcWsCciStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly CommodityChannelIndex _cciIndicator;
	private readonly SimpleMovingAverage _bodyAverage;

	private CandleSnapshot? _latestCandle;
	private CandleSnapshot? _previousCandle;
	private CandleSnapshot? _thirdCandle;

	private decimal? _latestCci;
	private decimal? _previousCci;

	/// <summary>
	/// CCI period used for confirmation.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles used to calculate the average body size.
	/// </summary>
	public int BodyAveragePeriod
	{
		get => _bodyAveragePeriod.Value;
		set => _bodyAveragePeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AbcWsCciStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 37)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Length of the CCI indicator", "Indicators");

		_bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Body Average Period", "Number of candles for average body size", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for calculations", "General");

		_cciIndicator = new CommodityChannelIndex();
		_bodyAverage = new SimpleMovingAverage();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cciIndicator.Length = CciPeriod;
		_bodyAverage.Length = BodyAveragePeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cciIndicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cciIndicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var averageValue = _bodyAverage.Process(new DecimalIndicatorValue(_bodyAverage, bodySize));
		if (!averageValue.IsFinal)
		{
			UpdateSnapshots(candle, cciValue);
			return;
		}

		var averageBody = averageValue.ToDecimal();

		UpdateSnapshots(candle, cciValue);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_latestCandle is null || _previousCandle is null || _thirdCandle is null)
			return;

		if (_latestCci is null || _previousCci is null)
			return;

		var latest = _latestCandle.Value;
		var previous = _previousCandle.Value;
		var third = _thirdCandle.Value;

		var latestCci = _latestCci.Value;
		var previousCci = _previousCci.Value;

		var isThreeWhiteSoldiers =
			(third.Close - third.Open > averageBody) &&
			(previous.Close - previous.Open > averageBody) &&
			(latest.Close - latest.Open > averageBody) &&
			(MidPoint(previous) > MidPoint(third)) &&
			(MidPoint(latest) > MidPoint(previous));

		var isThreeBlackCrows =
			(third.Open - third.Close > averageBody) &&
			(previous.Open - previous.Close > averageBody) &&
			(latest.Open - latest.Close > averageBody) &&
			(MidPoint(previous) < MidPoint(third)) &&
			(MidPoint(latest) < MidPoint(previous));

		if (isThreeWhiteSoldiers && latestCci < -50m && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (isThreeBlackCrows && latestCci > 50m && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0)
		{
			var shouldExitLong =
				(latestCci > -80m && previousCci < -80m) ||
				(latestCci < 80m && previousCci > 80m);

			if (shouldExitLong)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			var shouldExitShort =
				(latestCci < 80m && previousCci > 80m) ||
				(latestCci < -80m && previousCci > -80m);

			if (shouldExitShort)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}

	private void UpdateSnapshots(ICandleMessage candle, decimal cciValue)
	{
		_thirdCandle = _previousCandle;
		_previousCandle = _latestCandle;
		_latestCandle = new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);

		_previousCci = _latestCci;
		_latestCci = cciValue;
	}

	private static decimal MidPoint(CandleSnapshot candle)
	{
		return (candle.High + candle.Low) / 2m;
	}

	private readonly record struct CandleSnapshot(decimal Open, decimal High, decimal Low, decimal Close);
}
