using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Options strategy V1.3.
/// A long opens when the short EMA crosses above the long EMA with RSI at or above RsiLongThreshold and candle volume at or above its
/// SMA; a short on the opposite cross with RSI at or below RsiShortThreshold. Optionally the close must also be beyond the New York
/// opening range. The stop is SlMultiplier times ATR from the entry and the target TpSlRatio times the stop distance. An opposite
/// cross closes (or reverses) the position, an optional no-trade window blocks entries and everything is flat at 15:55 New York time.
/// </summary>
public class OptionsV13Strategy : Strategy
{
	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiLongThreshold;
	private readonly StrategyParam<decimal> _rsiShortThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<decimal> _tpSlRatio;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<bool> _useOpeningRange;
	private readonly StrategyParam<TimeSpan> _openingRangeStart;
	private readonly StrategyParam<TimeSpan> _openingRangeEnd;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<bool> _useNoTradeWindow;
	private readonly StrategyParam<TimeSpan> _noTradeStart;
	private readonly StrategyParam<TimeSpan> _noTradeEnd;
	private readonly StrategyParam<DataType> _candleType;

	private static readonly TimeZoneInfo _newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

	private SimpleMovingAverage _volumeMa;
	private decimal? _prevShort;
	private decimal? _prevLong;
	private DateTime _orDay;
	private decimal? _orHigh;
	private decimal? _orLow;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Short EMA length.
	/// </summary>
	public int EmaShortLength
	{
		get => _emaShortLength.Value;
		set => _emaShortLength.Value = value;
	}

	/// <summary>
	/// Long EMA length.
	/// </summary>
	public int EmaLongLength
	{
		get => _emaLongLength.Value;
		set => _emaLongLength.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Minimum RSI for a long entry.
	/// </summary>
	public decimal RsiLongThreshold
	{
		get => _rsiLongThreshold.Value;
		set => _rsiLongThreshold.Value = value;
	}

	/// <summary>
	/// Maximum RSI for a short entry.
	/// </summary>
	public decimal RsiShortThreshold
	{
		get => _rsiShortThreshold.Value;
		set => _rsiShortThreshold.Value = value;
	}

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Stop distance in ATR multiples.
	/// </summary>
	public decimal SlMultiplier
	{
		get => _slMultiplier.Value;
		set => _slMultiplier.Value = value;
	}

	/// <summary>
	/// Target distance in multiples of the stop distance.
	/// </summary>
	public decimal TpSlRatio
	{
		get => _tpSlRatio.Value;
		set => _tpSlRatio.Value = value;
	}

	/// <summary>
	/// Length of the volume SMA.
	/// </summary>
	public int VolumeMaLength
	{
		get => _volumeMaLength.Value;
		set => _volumeMaLength.Value = value;
	}

	/// <summary>
	/// Require a close beyond the opening range.
	/// </summary>
	public bool UseOpeningRange
	{
		get => _useOpeningRange.Value;
		set => _useOpeningRange.Value = value;
	}

	/// <summary>
	/// Opening range start (New York time).
	/// </summary>
	public TimeSpan OpeningRangeStart
	{
		get => _openingRangeStart.Value;
		set => _openingRangeStart.Value = value;
	}

	/// <summary>
	/// Opening range end (New York time).
	/// </summary>
	public TimeSpan OpeningRangeEnd
	{
		get => _openingRangeEnd.Value;
		set => _openingRangeEnd.Value = value;
	}

	/// <summary>
	/// Time when positions are closed (New York time).
	/// </summary>
	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Block entries inside the no-trade window.
	/// </summary>
	public bool UseNoTradeWindow
	{
		get => _useNoTradeWindow.Value;
		set => _useNoTradeWindow.Value = value;
	}

	/// <summary>
	/// No-trade window start (New York time).
	/// </summary>
	public TimeSpan NoTradeStart
	{
		get => _noTradeStart.Value;
		set => _noTradeStart.Value = value;
	}

	/// <summary>
	/// No-trade window end (New York time).
	/// </summary>
	public TimeSpan NoTradeEnd
	{
		get => _noTradeEnd.Value;
		set => _noTradeEnd.Value = value;
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
	public OptionsV13Strategy()
	{
		_emaShortLength = Param(nameof(EmaShortLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("EMA Short", "Short EMA length", "Indicators");

		_emaLongLength = Param(nameof(EmaLongLength), 28)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long", "Long EMA length", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_rsiLongThreshold = Param(nameof(RsiLongThreshold), 50m)
			.SetDisplay("RSI Long", "Minimum RSI for a long entry", "Indicators");

		_rsiShortThreshold = Param(nameof(RsiShortThreshold), 50m)
			.SetDisplay("RSI Short", "Maximum RSI for a short entry", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length", "Risk");

		_slMultiplier = Param(nameof(SlMultiplier), 1.4m)
			.SetGreaterThanZero()
			.SetDisplay("SL Multiplier", "Stop distance in ATR multiples", "Risk");

		_tpSlRatio = Param(nameof(TpSlRatio), 4m)
			.SetGreaterThanZero()
			.SetDisplay("TP/SL Ratio", "Target distance in multiples of the stop distance", "Risk");

		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Length of the volume SMA", "Indicators");

		_useOpeningRange = Param(nameof(UseOpeningRange), false)
			.SetDisplay("Use Opening Range", "Require a close beyond the opening range", "Session");

		_openingRangeStart = Param(nameof(OpeningRangeStart), new TimeSpan(9, 30, 0))
			.SetDisplay("OR Start", "Opening range start (New York time)", "Session");

		_openingRangeEnd = Param(nameof(OpeningRangeEnd), new TimeSpan(10, 0, 0))
			.SetDisplay("OR End", "Opening range end (New York time)", "Session");

		_closeTime = Param(nameof(CloseTime), new TimeSpan(15, 55, 0))
			.SetDisplay("Close Time", "Time when positions are closed (New York time)", "Session");

		_useNoTradeWindow = Param(nameof(UseNoTradeWindow), false)
			.SetDisplay("Use No-Trade Window", "Block entries inside the no-trade window", "Session");

		_noTradeStart = Param(nameof(NoTradeStart), new TimeSpan(12, 0, 0))
			.SetDisplay("No-Trade Start", "No-trade window start (New York time)", "Session");

		_noTradeEnd = Param(nameof(NoTradeEnd), new TimeSpan(13, 0, 0))
			.SetDisplay("No-Trade End", "No-trade window end (New York time)", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevShort = null;
		_prevLong = null;
		_orDay = default;
		_orHigh = null;
		_orLow = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var emaShort = new ExponentialMovingAverage { Length = EmaShortLength };
		var emaLong = new ExponentialMovingAverage { Length = EmaLongLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		_volumeMa = new SimpleMovingAverage { Length = VolumeMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaShort, emaLong, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShort, decimal emaLong, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeValue = _volumeMa.Process(new DecimalIndicatorValue(_volumeMa, candle.TotalVolume, candle.OpenTime) { IsFinal = true });
		decimal? volumeMa = _volumeMa.IsFormed ? volumeValue.GetValue<decimal>() : null;

		var nyTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(candle.OpenTime.ToUniversalTime(), DateTimeKind.Unspecified), _newYork);
		var nyDay = nyTime.Date;
		var tod = nyTime.TimeOfDay;

		if (nyDay != _orDay)
		{
			_orDay = nyDay;
			_orHigh = null;
			_orLow = null;
		}

		var inOpeningRange = tod >= OpeningRangeStart && tod < OpeningRangeEnd;
		if (inOpeningRange)
		{
			_orHigh = _orHigh is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;
			_orLow = _orLow is decimal l ? Math.Min(l, candle.LowPrice) : candle.LowPrice;
		}

		var prevShort = _prevShort;
		var prevLong = _prevLong;
		_prevShort = emaShort;
		_prevLong = emaLong;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (tod >= CloseTime)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			return;
		}

		if (Position > 0 && (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice))
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice))
		{
			BuyMarket(-Position);
			return;
		}

		if (prevShort is not decimal ps || prevLong is not decimal pl)
			return;

		var crossUp = ps <= pl && emaShort > emaLong;
		var crossDown = ps >= pl && emaShort < emaLong;

		if (!crossUp && !crossDown)
			return;

		var close = candle.ClosePrice;
		var volumeOk = volumeMa is decimal vma && candle.TotalVolume >= vma;
		var blocked = (UseNoTradeWindow && tod >= NoTradeStart && tod < NoTradeEnd)
			|| (UseOpeningRange && (inOpeningRange || tod < OpeningRangeStart));
		var stopDistance = atr * SlMultiplier;

		if (crossUp)
		{
			var orOk = !UseOpeningRange || (_orHigh is decimal orHigh && close > orHigh);

			if (Position <= 0 && !blocked && rsi >= RsiLongThreshold && volumeOk && orOk && stopDistance > 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_stopPrice = close - stopDistance;
				_takePrice = close + stopDistance * TpSlRatio;
			}
			else if (Position < 0)
			{
				BuyMarket(-Position);
			}
		}
		else
		{
			var orOk = !UseOpeningRange || (_orLow is decimal orLow && close < orLow);

			if (Position >= 0 && !blocked && rsi <= RsiShortThreshold && volumeOk && orOk && stopDistance > 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_stopPrice = close + stopDistance;
				_takePrice = close - stopDistance * TpSlRatio;
			}
			else if (Position > 0)
			{
				SellMarket(Position);
			}
		}
	}
}
