using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NASDAQ 100 peak hours strategy.
/// Trades only in the first two hours and the last hour of the cash session. A long opens when the close is above the short EMA,
/// the short EMA is above the long EMA, both EMAs are rising, RSI is above 50 and the close is above the session VWAP; a short uses
/// the opposite conditions. The position is protected by an initial ATR stop that moves to break-even and then trails by an ATR
/// multiple, and it is closed after TimeExitBars bars or when the EMA trend reverses.
/// </summary>
public class Nasdaq100PeakHoursStrategy : Strategy
{
	// Regular NASDAQ session (09:30-16:00 New York) expressed in UTC.
	private static readonly TimeSpan _sessionStart = new(13, 30, 0);
	private static readonly TimeSpan _sessionEnd = new(20, 0, 0);

	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _trailAtrMult;
	private readonly StrategyParam<decimal> _initialSlMult;
	private readonly StrategyParam<decimal> _breakEvenAtrMult;
	private readonly StrategyParam<int> _timeExitBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevShort;
	private decimal? _prevLong;
	private DateTime _vwapDate;
	private decimal _vwapPriceVolume;
	private decimal _vwapVolume;
	private decimal _entryPrice;
	private decimal _entryAtr;
	private decimal _stopPrice;
	private decimal _bestPrice;
	private int _barsInPosition;

	/// <summary>
	/// Long EMA period.
	/// </summary>
	public int LongEmaLength
	{
		get => _longEmaLength.Value;
		set => _longEmaLength.Value = value;
	}

	/// <summary>
	/// Short EMA period.
	/// </summary>
	public int ShortEmaLength
	{
		get => _shortEmaLength.Value;
		set => _shortEmaLength.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiple of the trailing stop.
	/// </summary>
	public decimal TrailAtrMult
	{
		get => _trailAtrMult.Value;
		set => _trailAtrMult.Value = value;
	}

	/// <summary>
	/// ATR multiple of the initial stop.
	/// </summary>
	public decimal InitialSlMult
	{
		get => _initialSlMult.Value;
		set => _initialSlMult.Value = value;
	}

	/// <summary>
	/// ATR profit that moves the stop to break-even.
	/// </summary>
	public decimal BreakEvenAtrMult
	{
		get => _breakEvenAtrMult.Value;
		set => _breakEvenAtrMult.Value = value;
	}

	/// <summary>
	/// Bars after which an open position is closed.
	/// </summary>
	public int TimeExitBars
	{
		get => _timeExitBars.Value;
		set => _timeExitBars.Value = value;
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
	public Nasdaq100PeakHoursStrategy()
	{
		_longEmaLength = Param(nameof(LongEmaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Long EMA period", "Indicators");

		_shortEmaLength = Param(nameof(ShortEmaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Short EMA period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI", "RSI period", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR", "ATR period", "Indicators");

		_trailAtrMult = Param(nameof(TrailAtrMult), 1.5m)
			.SetNotNegative()
			.SetDisplay("Trail ATR Mult", "ATR multiple of the trailing stop", "Risk");

		_initialSlMult = Param(nameof(InitialSlMult), 0.5m)
			.SetNotNegative()
			.SetDisplay("Initial SL Mult", "ATR multiple of the initial stop", "Risk");

		_breakEvenAtrMult = Param(nameof(BreakEvenAtrMult), 1.5m)
			.SetNotNegative()
			.SetDisplay("Break-even ATR Mult", "ATR profit that moves the stop to break-even", "Risk");

		_timeExitBars = Param(nameof(TimeExitBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Time Exit Bars", "Bars after which an open position is closed", "Risk");

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
		_vwapDate = default;
		_vwapPriceVolume = 0m;
		_vwapVolume = 0m;
		_entryPrice = 0m;
		_entryAtr = 0m;
		_stopPrice = 0m;
		_bestPrice = 0m;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var shortEma = new ExponentialMovingAverage { Length = ShortEmaLength };
		var longEma = new ExponentialMovingAverage { Length = LongEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortEma, longEma, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal longValue, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Session VWAP restarts every UTC day.
		var date = candle.OpenTime.Date;
		if (date != _vwapDate)
		{
			_vwapDate = date;
			_vwapPriceVolume = 0m;
			_vwapVolume = 0m;
		}

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		_vwapPriceVolume += typical * candle.TotalVolume;
		_vwapVolume += candle.TotalVolume;

		var prevShort = _prevShort;
		var prevLong = _prevLong;
		_prevShort = shortValue;
		_prevLong = longValue;

		if (prevShort is not decimal lastShort || prevLong is not decimal lastLong)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ManagePosition(candle, shortValue, longValue, atrValue))
			return;

		if (_vwapVolume <= 0 || atrValue <= 0 || !IsPeakHour(candle.OpenTime.TimeOfDay))
			return;

		var close = candle.ClosePrice;
		var vwap = _vwapPriceVolume / _vwapVolume;
		var rising = shortValue > lastShort && longValue > lastLong;
		var falling = shortValue < lastShort && longValue < lastLong;

		if (close > shortValue && shortValue > longValue && rising && rsiValue > 50 && close > vwap && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			OnEntry(close, atrValue, true);
		}
		else if (close < shortValue && shortValue < longValue && falling && rsiValue < 50 && close < vwap && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			OnEntry(close, atrValue, false);
		}
	}

	private static bool IsPeakHour(TimeSpan time)
	{
		var openingWindow = time >= _sessionStart && time < _sessionStart + TimeSpan.FromHours(2);
		var closingWindow = time >= _sessionEnd - TimeSpan.FromHours(1) && time < _sessionEnd;
		return openingWindow || closingWindow;
	}

	private void OnEntry(decimal price, decimal atr, bool isLong)
	{
		_entryPrice = price;
		_entryAtr = atr;
		_bestPrice = price;
		_barsInPosition = 0;
		_stopPrice = isLong ? price - atr * InitialSlMult : price + atr * InitialSlMult;
	}

	private bool ManagePosition(ICandleMessage candle, decimal shortValue, decimal longValue, decimal atr)
	{
		if (Position == 0)
			return false;

		_barsInPosition++;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || _barsInPosition >= TimeExitBars || shortValue < longValue)
			{
				SellMarket(Position);
				return true;
			}

			_bestPrice = Math.Max(_bestPrice, candle.HighPrice);

			if (_bestPrice - _entryPrice >= _entryAtr * BreakEvenAtrMult)
				_stopPrice = Math.Max(_stopPrice, _entryPrice);

			_stopPrice = Math.Max(_stopPrice, _bestPrice - atr * TrailAtrMult);
		}
		else
		{
			if (candle.HighPrice >= _stopPrice || _barsInPosition >= TimeExitBars || shortValue > longValue)
			{
				BuyMarket(-Position);
				return true;
			}

			_bestPrice = Math.Min(_bestPrice, candle.LowPrice);

			if (_entryPrice - _bestPrice >= _entryAtr * BreakEvenAtrMult)
				_stopPrice = Math.Min(_stopPrice, _entryPrice);

			_stopPrice = Math.Min(_stopPrice, _bestPrice + atr * TrailAtrMult);
		}

		return false;
	}
}
