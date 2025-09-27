using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple timeframe strategy with optional ADX filter and session control.
/// </summary>
public class TimeshifterTripleTimeframeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _lowerCandleType;
	private readonly StrategyParam<int> _higherMaLength;
	private readonly StrategyParam<int> _mediumMaLength;
	private readonly StrategyParam<int> _lowerMaLength;
	private readonly StrategyParam<bool> _useAdx;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<int> _adxThreshold;
	private readonly StrategyParam<Sides?> _tradeDirection;
	private readonly StrategyParam<bool> _useLondon;
	private readonly StrategyParam<bool> _useNewYork;
	private readonly StrategyParam<bool> _useTokyo;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _higherClose;
	private decimal _higherMa;
	private decimal _lowerClose;
	private decimal _lowerMa;
	private decimal _prevMediumClose;
	private decimal _prevMediumMa;
	private decimal _prevLowerClose;
	private decimal _prevLowerMa;

	public DataType HigherCandleType { get => _higherCandleType.Value; set => _higherCandleType.Value = value; }
	public DataType LowerCandleType { get => _lowerCandleType.Value; set => _lowerCandleType.Value = value; }
	public int HigherMaLength { get => _higherMaLength.Value; set => _higherMaLength.Value = value; }
	public int MediumMaLength { get => _mediumMaLength.Value; set => _mediumMaLength.Value = value; }
	public int LowerMaLength { get => _lowerMaLength.Value; set => _lowerMaLength.Value = value; }
	public bool UseAdx { get => _useAdx.Value; set => _useAdx.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public int AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public Sides? Direction { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }
	public bool UseLondon { get => _useLondon.Value; set => _useLondon.Value = value; }
	public bool UseNewYork { get => _useNewYork.Value; set => _useNewYork.Value = value; }
	public bool UseTokyo { get => _useTokyo.Value; set => _useTokyo.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TimeshifterTripleTimeframeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Medium Timeframe", "Chart timeframe", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Higher Timeframe", "Higher timeframe for trend", "General");

		_lowerCandleType = Param(nameof(LowerCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Lower Timeframe", "Lower timeframe for exits", "General");

		_higherMaLength = Param(nameof(HigherMaLength), 50)
			.SetDisplay("Higher MA Length", "Length for higher timeframe SMA", "Indicators");

		_mediumMaLength = Param(nameof(MediumMaLength), 20)
			.SetDisplay("Medium MA Length", "Length for medium timeframe SMA", "Indicators");

		_lowerMaLength = Param(nameof(LowerMaLength), 10)
			.SetDisplay("Lower MA Length", "Length for lower timeframe SMA", "Indicators");

		_useAdx = Param(nameof(UseAdx), false)
			.SetDisplay("Use ADX", "Enable ADX filter", "Filters");

		_adxLength = Param(nameof(AdxLength), 14)
			.SetDisplay("ADX Length", "Length for ADX", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25)
			.SetDisplay("ADX Threshold", "Minimum ADX for entries", "Filters");

		_tradeDirection = Param(nameof(Direction), (Sides?)null)
			.SetDisplay("Trade Direction", "Direction of trades", "General");

		_useLondon = Param(nameof(UseLondon), true)
			.SetDisplay("London Session", "Enable London session", "Sessions");

		_useNewYork = Param(nameof(UseNewYork), true)
			.SetDisplay("New York Session", "Enable New York session", "Sessions");

		_useTokyo = Param(nameof(UseTokyo), true)
			.SetDisplay("Tokyo Session", "Enable Tokyo session", "Sessions");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherCandleType), (Security, LowerCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_higherClose = _higherMa = _lowerClose = _lowerMa = 0m;
		_prevMediumClose = _prevMediumMa = 0m;
		_prevLowerClose = _prevLowerMa = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var higherMa = new SimpleMovingAverage { Length = HigherMaLength };
		var lowerMa = new SimpleMovingAverage { Length = LowerMaLength };
		var mediumMa = new SimpleMovingAverage { Length = MediumMaLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };

		var higherSub = SubscribeCandles(HigherCandleType);
		higherSub.BindEx(higherMa, ProcessHigher).Start();

		var lowerSub = SubscribeCandles(LowerCandleType);
		lowerSub.BindEx(lowerMa, ProcessLower).Start();

		var mediumSub = SubscribeCandles(CandleType);
		mediumSub.BindEx(mediumMa, adx, ProcessMedium).Start();
	}

	private void ProcessHigher(ICandleMessage candle, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (!maValue.IsFinal)
			return;
		_higherClose = candle.ClosePrice;
		_higherMa = maValue.ToDecimal();
	}

	private void ProcessLower(ICandleMessage candle, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (!maValue.IsFinal)
			return;
		_prevLowerClose = _lowerClose;
		_prevLowerMa = _lowerMa;
		_lowerClose = candle.ClosePrice;
		_lowerMa = maValue.ToDecimal();
	}

	private void ProcessMedium(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (!maValue.IsFinal || !adxValue.IsFinal)
			return;

		var mediumClose = candle.ClosePrice;
		var mediumMa = maValue.ToDecimal();

		var higherUptrend = _higherClose > _higherMa;
		var higherDowntrend = _higherClose < _higherMa;

		var entryLong = _prevMediumClose <= _prevMediumMa && mediumClose > mediumMa;
		var entryShort = _prevMediumClose >= _prevMediumMa && mediumClose < mediumMa;

		_prevMediumClose = mediumClose;
		_prevMediumMa = mediumMa;

		var exitLong = _prevLowerClose >= _prevLowerMa && _lowerClose < _lowerMa;
		var exitShort = _prevLowerClose <= _prevLowerMa && _lowerClose > _lowerMa;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adxMa)
			return;
		var adxCondition = !UseAdx || adxMa > AdxThreshold;

		var hour = candle.OpenTime.UtcDateTime.Hour;
		var inLondon = UseLondon && hour >= 7 && hour < 16;
		var inNewYork = UseNewYork && hour >= 13 && hour < 22;
		var inTokyo = UseTokyo && hour >= 0 && hour < 9;
		var inSession = inLondon || inNewYork || inTokyo;

		if (!inSession)
			return;

		if (Direction != Sides.Sell && higherUptrend)
		{
			if (entryLong && adxCondition && Position <= 0)
				BuyMarket();
			else if (exitLong && Position > 0)
				ClosePosition();
		}

		if (Direction != Sides.Buy && higherDowntrend)
		{
			if (entryShort && adxCondition && Position >= 0)
				SellMarket();
			else if (exitShort && Position < 0)
				ClosePosition();
		}
	}
}
