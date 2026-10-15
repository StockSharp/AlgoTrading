using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hybrid RSI breakout strategy.
/// When ADX is at or below AdxThreshold the market is ranging: a long opens when RSI drops below RsiBuy with the close above the
/// EmaLength EMA, a short when RSI rises above RsiSell with the close below it, and these RSI trades close when RSI crosses back
/// over RsiExit. When ADX is above AdxThreshold the market is trending: a long opens when the close breaks above the highest close of
/// the previous BreakoutLength candles and a short when it breaks below the lowest close, and these breakout trades are closed by an
/// ATR trailing stop of AtrMultiplier * ATR. Trading starts at StartDate. The last trade type and direction are kept for display.
/// </summary>
public class HybridRsiBreakoutDashboardStrategy : Strategy
{
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBuy;
	private readonly StrategyParam<decimal> _rsiSell;
	private readonly StrategyParam<decimal> _rsiExit;
	private readonly StrategyParam<int> _breakoutLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private decimal? _prevHighest;
	private decimal? _prevLowest;
	private bool _isBreakoutTrade;
	private decimal? _trailingStop;

	/// <summary>
	/// Type of the last trade: "RSI" or "Breakout".
	/// </summary>
	public string LastTradeType { get; private set; }

	/// <summary>
	/// Direction of the last trade: "Long" or "Short".
	/// </summary>
	public string LastTradeDirection { get; private set; }

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	/// <summary>
	/// ADX level above which the market is trending.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Trend EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
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
	/// RSI level below which a ranging long opens.
	/// </summary>
	public decimal RsiBuy
	{
		get => _rsiBuy.Value;
		set => _rsiBuy.Value = value;
	}

	/// <summary>
	/// RSI level above which a ranging short opens.
	/// </summary>
	public decimal RsiSell
	{
		get => _rsiSell.Value;
		set => _rsiSell.Value = value;
	}

	/// <summary>
	/// RSI level that closes RSI trades.
	/// </summary>
	public decimal RsiExit
	{
		get => _rsiExit.Value;
		set => _rsiExit.Value = value;
	}

	/// <summary>
	/// Previous closes the breakout range spans.
	/// </summary>
	public int BreakoutLength
	{
		get => _breakoutLength.Value;
		set => _breakoutLength.Value = value;
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
	/// ATR multiplier of the trailing stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Date trading starts from.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
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
	public HybridRsiBreakoutDashboardStrategy()
	{
		_adxLength = Param(nameof(AdxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetDisplay("ADX Threshold", "ADX level above which the market is trending", "Indicators");

		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Trend EMA period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiBuy = Param(nameof(RsiBuy), 40m)
			.SetDisplay("RSI Buy", "RSI level below which a ranging long opens", "Signals");

		_rsiSell = Param(nameof(RsiSell), 60m)
			.SetDisplay("RSI Sell", "RSI level above which a ranging short opens", "Signals");

		_rsiExit = Param(nameof(RsiExit), 50m)
			.SetDisplay("RSI Exit", "RSI level that closes RSI trades", "Signals");

		_breakoutLength = Param(nameof(BreakoutLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Length", "Previous closes the breakout range spans", "Signals");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the trailing stop", "Risk");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Date trading starts from", "General");

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
		_prevHighest = null;
		_prevLowest = null;
		_isBreakoutTrade = false;
		_trailingStop = null;
		LastTradeType = null;
		LastTradeDirection = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		_highest = new Highest { Length = BreakoutLength };
		_lowest = new Lowest { Length = BreakoutLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ema, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, adx);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue emaValue, IIndicatorValue rsiValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// The breakout range is measured on the closes before this candle.
		var rangeHigh = _prevHighest;
		var rangeLow = _prevLowest;

		var highestValue = _highest.Process(new DecimalIndicatorValue(_highest, close, candle.OpenTime) { IsFinal = true });
		var lowestValue = _lowest.Process(new DecimalIndicatorValue(_lowest, close, candle.OpenTime) { IsFinal = true });

		if (_highest.IsFormed && _lowest.IsFormed)
		{
			_prevHighest = highestValue.GetValue<decimal>();
			_prevLowest = lowestValue.GetValue<decimal>();
		}

		if (!adxValue.IsFormed || adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal adx })
			return;

		if (!emaValue.IsFormed || !rsiValue.IsFormed || !atrValue.IsFormed || rangeHigh is not decimal high || rangeLow is not decimal low)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime < StartDate.UtcDateTime)
			return;

		var ema = emaValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var trail = atrValue.GetValue<decimal>() * AtrMultiplier;

		// Manage the open trade first.
		if (Position > 0)
		{
			if (_isBreakoutTrade)
			{
				_trailingStop = Math.Max(_trailingStop ?? close - trail, close - trail);
				if (candle.LowPrice <= _trailingStop)
				{
					SellMarket(Position);
					_trailingStop = null;
					return;
				}
			}
			else if (rsi > RsiExit)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0)
		{
			if (_isBreakoutTrade)
			{
				_trailingStop = Math.Min(_trailingStop ?? close + trail, close + trail);
				if (candle.HighPrice >= _trailingStop)
				{
					BuyMarket(-Position);
					_trailingStop = null;
					return;
				}
			}
			else if (rsi < RsiExit)
			{
				BuyMarket(-Position);
				return;
			}
		}

		var trending = adx > AdxThreshold;
		var bullish = close > ema;

		bool goLong, goShort;
		if (trending)
		{
			goLong = close > high;
			goShort = close < low;
		}
		else
		{
			goLong = rsi < RsiBuy && bullish;
			goShort = rsi > RsiSell && !bullish;
		}

		if (goLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			OnEntry(trending, true, close, trail);
		}
		else if (goShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			OnEntry(trending, false, close, trail);
		}
	}

	private void OnEntry(bool breakout, bool isLong, decimal close, decimal trail)
	{
		_isBreakoutTrade = breakout;
		_trailingStop = breakout ? (isLong ? close - trail : close + trail) : null;
		LastTradeType = breakout ? "Breakout" : "RSI";
		LastTradeDirection = isLong ? "Long" : "Short";
	}
}
