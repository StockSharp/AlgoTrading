using System;
using Ecng.ComponentModel;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy detecting three-candle bullish or bearish engulfing patterns with optional RSI breakout logic.
/// </summary>
public class ThreeCandleBullishEngulfingStrategy : Strategy
{
	private readonly StrategyParam<bool> _candleLongOnly;
	private readonly StrategyParam<bool> _candleIntraday;
	private readonly StrategyParam<bool> _rsiBreakout;
	private readonly StrategyParam<decimal> _trailPerc;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<int> _exitMinute;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiLevel;
	private readonly StrategyParam<TimeSpan> _rsiTimeframe;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<bool> _rsiIntraday;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prev1;
	private ICandleMessage _prev2;
	private decimal? _triggerPrice;
	private bool _triggerUpdated;
	private decimal _stopLevel;
	private decimal _highest;
	private decimal _lowest;

	public bool CandleLongOnly { get => _candleLongOnly.Value; set => _candleLongOnly.Value = value; }
	public bool CandleIntraday { get => _candleIntraday.Value; set => _candleIntraday.Value = value; }
	public bool RsiBreakout { get => _rsiBreakout.Value; set => _rsiBreakout.Value = value; }
	public decimal TrailPerc { get => _trailPerc.Value; set => _trailPerc.Value = value; }
	public int ExitHour { get => _exitHour.Value; set => _exitHour.Value = value; }
	public int ExitMinute { get => _exitMinute.Value; set => _exitMinute.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiLevel { get => _rsiLevel.Value; set => _rsiLevel.Value = value; }
	public TimeSpan RsiTimeframe { get => _rsiTimeframe.Value; set => _rsiTimeframe.Value = value; }
	public decimal StopLossPerc { get => _stopLossPerc.Value; set => _stopLossPerc.Value = value; }
	public bool RsiIntraday { get => _rsiIntraday.Value; set => _rsiIntraday.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ThreeCandleBullishEngulfingStrategy()
	{
		_candleLongOnly = Param(nameof(CandleLongOnly), false)
		.SetDisplay("Long only", "Allow only long trades", "Pattern");
		_candleIntraday = Param(nameof(CandleIntraday), false)
		.SetDisplay("Intraday", "Enable long and short pattern trades", "Pattern");
		_rsiBreakout = Param(nameof(RsiBreakout), false)
		.SetDisplay("RSI Breakout", "Enable RSI breakout logic", "RSI");
		_trailPerc = Param(nameof(TrailPerc), 1.5m)
		.SetDisplay("Trail %", "Trailing stop percent", "Risk");
		_exitHour = Param(nameof(ExitHour), 15)
		.SetDisplay("Exit Hour", "Hour for time based exit", "Pattern");
		_exitMinute = Param(nameof(ExitMinute), 15)
		.SetDisplay("Exit Minute", "Minute for time based exit", "Pattern");
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "RSI period", "RSI");
		_rsiLevel = Param(nameof(RsiLevel), 80)
		.SetDisplay("RSI Level", "RSI trigger level", "RSI");
		_rsiTimeframe = Param(nameof(RsiTimeframe), TimeSpan.FromMinutes(60))
		.SetDisplay("RSI TF", "Timeframe for RSI", "RSI");
		_stopLossPerc = Param(nameof(StopLossPerc), 5m)
		.SetDisplay("Stop %", "Stop loss percent from trigger price", "RSI");
		_rsiIntraday = Param(nameof(RsiIntraday), false)
		.SetDisplay("RSI Intraday", "Close RSI trades at session end", "RSI");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Working candle timeframe", "Data");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prev1 = null;
		_prev2 = null;
		_triggerPrice = null;
		_triggerUpdated = false;
		_stopLevel = 0;
		_highest = 0;
		_lowest = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessMain).Start();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var rsiSub = SubscribeCandles(RsiTimeframe.TimeFrame());
		rsiSub.Bind(rsi, ProcessRsi).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessRsi(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var bullish = candle.ClosePrice > candle.OpenPrice;
		var newTrigger = rsiValue >= RsiLevel && bullish;

		if (!newTrigger)
		return;

		_triggerUpdated = Position > 0;
		_triggerPrice = candle.ClosePrice;
	}

	private void ProcessMain(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var exitTime = candle.OpenTime.Hour == ExitHour && candle.OpenTime.Minute >= ExitMinute;

		bool entryCondLong = false;
		bool entryCondShort = false;

		if (_prev1 != null && _prev2 != null)
		{
			var isBullish1 = _prev2.ClosePrice > _prev2.OpenPrice;
			var body1 = Math.Abs(_prev2.ClosePrice - _prev2.OpenPrice);
			var body2 = Math.Abs(_prev1.ClosePrice - _prev1.OpenPrice);
			var isDoji2 = body2 <= body1 * 0.5m;
			var isEngulf3 = candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > _prev1.HighPrice;

			var isBearish1 = _prev2.ClosePrice < _prev2.OpenPrice;
			var bearBody1 = Math.Abs(_prev2.ClosePrice - _prev2.OpenPrice);
			var bearBody2 = Math.Abs(_prev1.ClosePrice - _prev1.OpenPrice);
			var isBearDoji2 = bearBody2 <= bearBody1 * 0.5m;
			var isBearEngulf3 = candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < _prev1.LowPrice;

			entryCondLong = isBullish1 && isDoji2 && isEngulf3;
			entryCondShort = isBearish1 && isBearDoji2 && isBearEngulf3;
		}

		if (entryCondLong && (CandleIntraday || CandleLongOnly) && Position <= 0)
		BuyMarket();

		if (entryCondShort && CandleIntraday && Position >= 0)
		SellMarket();

		if (exitTime && (CandleIntraday || (RsiBreakout && RsiIntraday)))
		{
			if (Position > 0)
			SellMarket();
			else if (Position < 0)
			BuyMarket();
		}

		if (Position > 0)
		{
			_highest = Math.Max(_highest, candle.HighPrice);
			var trailStop = _highest * (1m - TrailPerc / 100m);
			if (candle.ClosePrice < trailStop || (_prev1 != null && candle.ClosePrice < _prev1.LowPrice))
			SellMarket();
		}
		else if (Position < 0)
		{
			_lowest = Math.Min(_lowest, candle.LowPrice);
			var trailStop = _lowest * (1m + TrailPerc / 100m);
			if (candle.ClosePrice > trailStop || (_prev1 != null && candle.ClosePrice > _prev1.HighPrice))
			BuyMarket();
		}

		if (RsiBreakout)
		{
			if (Position == 0 && _triggerPrice != null && candle.ClosePrice > _triggerPrice)
			{
				BuyMarket();
				_stopLevel = _triggerPrice.Value * (1m - StopLossPerc / 100m);
				_triggerUpdated = false;
			}
			else if (Position > 0 && (_triggerUpdated || candle.ClosePrice <= _stopLevel))
			{
				SellMarket();
				_triggerPrice = null;
				_triggerUpdated = false;
			}
		}

		_prev2 = _prev1;
		_prev1 = candle;
	}
}
