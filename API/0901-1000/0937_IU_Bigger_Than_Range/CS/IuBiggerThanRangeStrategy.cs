using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stop loss placement methods.
/// </summary>
public enum IuBiggerThanRangeStopMethods
{
	/// <summary>
	/// Low (long) or high (short) of the previous candle.
	/// </summary>
	PreviousHighLow,

	/// <summary>
	/// ATR multiplied by AtrFactor away from the entry close.
	/// </summary>
	Atr,

	/// <summary>
	/// Lowest low (long) or highest high (short) of the last SwingLength candles.
	/// </summary>
	Swing,
}

/// <summary>
/// IU bigger than range strategy.
/// The previous range spans the highest open/close and lowest open/close of the LookbackPeriod candles before the current one.
/// A candle whose body is larger than that range enters in its direction, reversing an opposite position. The stop is placed by
/// StopLossMethod and the target sits RiskToReward times the stop distance away; touching either closes the trade.
/// </summary>
public class IuBiggerThanRangeStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _riskToReward;
	private readonly StrategyParam<IuBiggerThanRangeStopMethods> _stopLossMethod;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _swingLength;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _bodyHigh;
	private Lowest _bodyLow;
	private Highest _swingHigh;
	private Lowest _swingLow;
	private decimal? _prevRangeHigh;
	private decimal? _prevRangeLow;
	private decimal? _prevCandleHigh;
	private decimal? _prevCandleLow;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// Candles the previous range spans.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Target distance as a multiple of the stop distance.
	/// </summary>
	public int RiskToReward
	{
		get => _riskToReward.Value;
		set => _riskToReward.Value = value;
	}

	/// <summary>
	/// How the stop loss is placed.
	/// </summary>
	public IuBiggerThanRangeStopMethods StopLossMethod
	{
		get => _stopLossMethod.Value;
		set => _stopLossMethod.Value = value;
	}

	/// <summary>
	/// ATR period for the ATR stop.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the ATR stop.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Candles the swing stop looks back over.
	/// </summary>
	public int SwingLength
	{
		get => _swingLength.Value;
		set => _swingLength.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IuBiggerThanRangeStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 22)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Candles the previous range spans", "Parameters");

		_riskToReward = Param(nameof(RiskToReward), 3)
			.SetGreaterThanZero()
			.SetDisplay("Risk To Reward", "Target distance as a multiple of the stop distance", "Risk");

		_stopLossMethod = Param(nameof(StopLossMethod), IuBiggerThanRangeStopMethods.PreviousHighLow)
			.SetDisplay("Stop Loss Method", "How the stop loss is placed", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for the ATR stop", "Risk");

		_atrFactor = Param(nameof(AtrFactor), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Factor", "ATR multiplier for the ATR stop", "Risk");

		_swingLength = Param(nameof(SwingLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Swing Length", "Candles the swing stop looks back over", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevRangeHigh = _prevRangeLow = null;
		_prevCandleHigh = _prevCandleLow = null;
		_stopPrice = _targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		_bodyHigh = new Highest { Length = LookbackPeriod };
		_bodyLow = new Lowest { Length = LookbackPeriod };
		_swingHigh = new Highest { Length = SwingLength };
		_swingLow = new Lowest { Length = SwingLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

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

		var time = candle.OpenTime;
		var bodyTop = Math.Max(candle.OpenPrice, candle.ClosePrice);
		var bodyBottom = Math.Min(candle.OpenPrice, candle.ClosePrice);

		// The range is measured on the candles before this one.
		var rangeHigh = _prevRangeHigh;
		var rangeLow = _prevRangeLow;
		var prevHigh = _prevCandleHigh;
		var prevLow = _prevCandleLow;

		var bodyHighValue = _bodyHigh.Process(new DecimalIndicatorValue(_bodyHigh, bodyTop, time) { IsFinal = true });
		var bodyLowValue = _bodyLow.Process(new DecimalIndicatorValue(_bodyLow, bodyBottom, time) { IsFinal = true });
		var swingHighValue = _swingHigh.Process(new DecimalIndicatorValue(_swingHigh, candle.HighPrice, time) { IsFinal = true });
		var swingLowValue = _swingLow.Process(new DecimalIndicatorValue(_swingLow, candle.LowPrice, time) { IsFinal = true });

		_prevRangeHigh = _bodyHigh.IsFormed ? bodyHighValue.GetValue<decimal>() : null;
		_prevRangeLow = _bodyLow.IsFormed ? bodyLowValue.GetValue<decimal>() : null;
		_prevCandleHigh = candle.HighPrice;
		_prevCandleLow = candle.LowPrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && _stopPrice is decimal longStop && _targetPrice is decimal longTarget)
		{
			if (candle.LowPrice <= longStop || candle.HighPrice >= longTarget)
			{
				SellMarket(Position);
				_stopPrice = _targetPrice = null;
				return;
			}
		}
		else if (Position < 0 && _stopPrice is decimal shortStop && _targetPrice is decimal shortTarget)
		{
			if (candle.HighPrice >= shortStop || candle.LowPrice <= shortTarget)
			{
				BuyMarket(-Position);
				_stopPrice = _targetPrice = null;
				return;
			}
		}

		if (rangeHigh is not decimal high || rangeLow is not decimal low || prevHigh is not decimal lastHigh || prevLow is not decimal lastLow)
			return;

		var body = bodyTop - bodyBottom;
		if (body <= high - low)
			return;

		var close = candle.ClosePrice;

		if (candle.ClosePrice > candle.OpenPrice && Position <= 0)
		{
			decimal? stop = StopLossMethod switch
			{
				IuBiggerThanRangeStopMethods.Atr => close - atrValue * AtrFactor,
				IuBiggerThanRangeStopMethods.Swing => _swingLow.IsFormed ? swingLowValue.GetValue<decimal>() : null,
				_ => lastLow,
			};

			if (stop is not decimal stopPrice || stopPrice >= close)
				return;

			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = stopPrice;
			_targetPrice = close + (close - stopPrice) * RiskToReward;
		}
		else if (candle.ClosePrice < candle.OpenPrice && Position >= 0)
		{
			decimal? stop = StopLossMethod switch
			{
				IuBiggerThanRangeStopMethods.Atr => close + atrValue * AtrFactor,
				IuBiggerThanRangeStopMethods.Swing => _swingHigh.IsFormed ? swingHighValue.GetValue<decimal>() : null,
				_ => lastHigh,
			};

			if (stop is not decimal stopPrice || stopPrice <= close)
				return;

			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = stopPrice;
			_targetPrice = close - (stopPrice - close) * RiskToReward;
		}
	}
}
