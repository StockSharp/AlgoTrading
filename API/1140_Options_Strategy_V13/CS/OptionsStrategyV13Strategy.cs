using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum SignalDirection
{
	Long,
	Short,
	Both
}

/// <summary>
/// EMA crossover strategy with RSI and volume filter.
/// </summary>
public class OptionsStrategyV13Strategy : Strategy
{
	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiLongThreshold;
	private readonly StrategyParam<int> _rsiShortThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<decimal> _tpSlRatio;
	private readonly StrategyParam<bool> _enableOrBreakout;
	private readonly StrategyParam<bool> _enableEodClose;
	private readonly StrategyParam<SignalDirection> _signalDirection;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<TimeSpan> _noTradeStart;
	private readonly StrategyParam<TimeSpan> _noTradeEnd;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private decimal _prevEmaShort;
	private decimal _prevEmaLong;
	private bool _readyLong;
	private bool _readyShort;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private decimal _orHigh;
	private decimal _orLow;
	private bool _inOrSession;
	private readonly TimeZoneInfo _nyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

	public int EmaShortLength { get => _emaShortLength.Value; set => _emaShortLength.Value = value; }
	public int EmaLongLength { get => _emaLongLength.Value; set => _emaLongLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiLongThreshold { get => _rsiLongThreshold.Value; set => _rsiLongThreshold.Value = value; }
	public int RsiShortThreshold { get => _rsiShortThreshold.Value; set => _rsiShortThreshold.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal SlMultiplier { get => _slMultiplier.Value; set => _slMultiplier.Value = value; }
	public decimal TpSlRatio { get => _tpSlRatio.Value; set => _tpSlRatio.Value = value; }
	public bool EnableOrBreakout { get => _enableOrBreakout.Value; set => _enableOrBreakout.Value = value; }
	public bool EnableEodClose { get => _enableEodClose.Value; set => _enableEodClose.Value = value; }
	public SignalDirection SignalDirection { get => _signalDirection.Value; set => _signalDirection.Value = value; }
	public int VolumeMaLength { get => _volumeMaLength.Value; set => _volumeMaLength.Value = value; }
	public TimeSpan NoTradeStart { get => _noTradeStart.Value; set => _noTradeStart.Value = value; }
	public TimeSpan NoTradeEnd { get => _noTradeEnd.Value; set => _noTradeEnd.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OptionsStrategyV13Strategy()
	{
		_emaShortLength = Param(nameof(EmaShortLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("EMA Short Length", "Period of short EMA", "General");
		_emaLongLength = Param(nameof(EmaLongLength), 28)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long Length", "Period of long EMA", "General");
		_rsiLength = Param(nameof(RsiLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation period", "General");
		_rsiLongThreshold = Param(nameof(RsiLongThreshold), 56)
			.SetDisplay("RSI Long Threshold", "Minimum RSI for long", "General");
		_rsiShortThreshold = Param(nameof(RsiShortThreshold), 26)
			.SetDisplay("RSI Short Threshold", "Maximum RSI for short", "General");
		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "Risk");
		_slMultiplier = Param(nameof(SlMultiplier), 1.4m)
			.SetGreaterThanZero()
			.SetDisplay("SL Multiplier", "ATR multiplier for stop-loss", "Risk");
		_tpSlRatio = Param(nameof(TpSlRatio), 4m)
			.SetGreaterThanZero()
			.SetDisplay("TP/SL Ratio", "Take-profit to stop-loss ratio", "Risk");
		_enableOrBreakout = Param(nameof(EnableOrBreakout), false)
			.SetDisplay("Require OR Breakout", "Require price to break opening range", "Signals");
		_enableEodClose = Param(nameof(EnableEodClose), true)
			.SetDisplay("Auto Close at 15:55", "Close positions at 15:55 NY time", "Session");
		_signalDirection = Param(nameof(SignalDirection), SignalDirection.Both)
			.SetDisplay("Signal Direction", "Allowed trade direction", "Signals");
		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Length for volume SMA", "Volume");
		_noTradeStart = Param(nameof(NoTradeStart), new TimeSpan(12, 0, 0))
			.SetDisplay("No-Trade Start", "Start of custom no-trade session", "Session");
		_noTradeEnd = Param(nameof(NoTradeEnd), new TimeSpan(15, 0, 0))
			.SetDisplay("No-Trade End", "End of custom no-trade session", "Session");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevEmaShort = 0m;
		_prevEmaLong = 0m;
		_readyLong = false;
		_readyShort = false;
		_stopPrice = 0m;
		_targetPrice = 0m;
		_orHigh = 0m;
		_orLow = 0m;
		_inOrSession = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var emaShort = new ExponentialMovingAverage { Length = EmaShortLength };
		var emaLong = new ExponentialMovingAverage { Length = EmaLongLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		_volumeSma = new SimpleMovingAverage { Length = VolumeMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaShort, emaLong, rsi, atr, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShort, decimal emaLong, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var nyTime = TimeZoneInfo.ConvertTime(candle.OpenTime.UtcDateTime, _nyTimeZone);
		var time = nyTime.TimeOfDay;

		var volumeAvg = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		if (!_volumeSma.IsFormed)
		{
			_prevEmaShort = emaShort;
			_prevEmaLong = emaLong;
			return;
		}
		var volOk = candle.TotalVolume >= volumeAvg;

		var orStart = new TimeSpan(9, 30, 0);
		var orEnd = new TimeSpan(9, 45, 0);
		if (time >= orStart && time < orEnd)
		{
			if (!_inOrSession)
			{
				_inOrSession = true;
				_orHigh = candle.HighPrice;
				_orLow = candle.LowPrice;
			}
			else
			{
				_orHigh = Math.Max(_orHigh, candle.HighPrice);
				_orLow = Math.Min(_orLow, candle.LowPrice);
			}
		}
		else if (_inOrSession)
		{
			_inOrSession = false;
		}

		var blockedMorning = time >= new TimeSpan(4, 0, 0) && time < new TimeSpan(9, 30, 0);
		var blockedEvening = time >= new TimeSpan(16, 0, 0) && time < new TimeSpan(20, 0, 0);
		var inNoTrade = InTimeRange(time, NoTradeStart, NoTradeEnd);
		var blockedSession = blockedMorning || blockedEvening || inNoTrade;

		if (EnableEodClose && nyTime.Hour == 15 && nyTime.Minute == 55)
		{
			if (Position != 0)
				ClosePosition();
		}

		var crossOver = _prevEmaShort <= _prevEmaLong && emaShort > emaLong;
		var crossUnder = _prevEmaShort >= _prevEmaLong && emaShort < emaLong;
		if (crossOver)
		{
			_readyLong = true;
			_readyShort = false;
		}
		else if (crossUnder)
		{
			_readyShort = true;
			_readyLong = false;
		}

		if (!blockedSession && volOk && Position == 0)
		{
			var allowLong = SignalDirection == SignalDirection.Both || SignalDirection == SignalDirection.Long;
			var allowShort = SignalDirection == SignalDirection.Both || SignalDirection == SignalDirection.Short;

			if (allowLong && _readyLong && rsiValue >= RsiLongThreshold && (!EnableOrBreakout || (candle.ClosePrice > _orHigh && _orHigh > 0)))
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_stopPrice = candle.ClosePrice - atrValue * SlMultiplier;
				_targetPrice = candle.ClosePrice + atrValue * SlMultiplier * TpSlRatio;
				_readyLong = false;
			}
			else if (allowShort && _readyShort && rsiValue <= RsiShortThreshold && (!EnableOrBreakout || (candle.ClosePrice < _orLow && _orLow > 0)))
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				_stopPrice = candle.ClosePrice + atrValue * SlMultiplier;
				_targetPrice = candle.ClosePrice - atrValue * SlMultiplier * TpSlRatio;
				_readyShort = false;
			}
		}
		else if (Position > 0)
		{
			var crossExit = _prevEmaShort > _prevEmaLong && emaShort < emaLong;
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice || crossExit)
				ClosePosition();
		}
		else if (Position < 0)
		{
			var crossExit = _prevEmaShort < _prevEmaLong && emaShort > emaLong;
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice || crossExit)
				ClosePosition();
		}

		_prevEmaShort = emaShort;
		_prevEmaLong = emaLong;
	}

	private static bool InTimeRange(TimeSpan time, TimeSpan start, TimeSpan end)
	{
		return start <= end ? time >= start && time <= end : time >= start || time <= end;
	}
}
