using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD, RSI, EMA, Bollinger Bands and ATR day trading strategy.
/// Goes long when MACD crosses above its signal line while the fast EMA is above the slow EMA, and short on the opposite cross with
/// the fast EMA below the slow EMA. RSI must be between RsiOversold and RsiOverbought, and Bollinger Bands must not be in a squeeze
/// (band width below its average over BbLength candles). The stop starts AtrMultiplier ATRs from entry and trails TrailAtrMultiplier
/// ATRs behind the close, the target is RiskReward times the initial stop distance. An opposite signal reverses the position.
/// </summary>
public class MacdRsiEmaBbAtrDayTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _emaFast;
	private readonly StrategyParam<int> _emaSlow;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _trailAtrMultiplier;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _widths = new();
	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
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
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int EmaFast
	{
		get => _emaFast.Value;
		set => _emaFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaSlow
	{
		get => _emaSlow.Value;
		set => _emaSlow.Value = value;
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
	/// ATR multiplier of the initial stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the trailing stop.
	/// </summary>
	public decimal TrailAtrMultiplier
	{
		get => _trailAtrMultiplier.Value;
		set => _trailAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit as a multiple of the initial stop distance.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
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
	public MacdRsiEmaBbAtrDayTradingStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast period", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow period", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal period", "MACD");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		_emaFast = Param(nameof(EmaFast), 9)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA period", "Trend");

		_emaSlow = Param(nameof(EmaSlow), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA period", "Trend");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the initial stop", "Risk");

		_trailAtrMultiplier = Param(nameof(TrailAtrMultiplier), 1.5m)
			.SetNotNegative()
			.SetDisplay("Trail ATR Multiplier", "ATR multiplier of the trailing stop", "Risk");

		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger");

		_bbMultiplier = Param(nameof(BbMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger Bands width multiplier", "Bollinger");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Take profit as a multiple of the stop distance", "Risk");

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
		_widths.Clear();
		_prevMacd = null;
		_prevSignal = null;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var emaFast = new ExponentialMovingAverage { Length = EmaFast };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlow };
		var atr = new AverageTrueRange { Length = AtrLength };
		var bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, rsi, emaFast, emaSlow, atr, bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue, IIndicatorValue emaFastValue,
		IIndicatorValue emaSlowValue, IIndicatorValue atrValue, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		decimal? width = null;
		if (bollingerValue.IsFormed && bollingerValue is BollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
		{
			width = upper - lower;
			_widths.Enqueue(upper - lower);
			while (_widths.Count > BbLength)
				_widths.Dequeue();
		}

		if (!macdValue.IsFormed || !rsiValue.IsFormed || !emaFastValue.IsFormed || !emaSlowValue.IsFormed || !atrValue.IsFormed)
			return;

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		_prevMacd = macdLine;
		_prevSignal = signalLine;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (Position > 0 && _stopPrice is decimal longStop && _takePrice is decimal longTake)
		{
			if (candle.LowPrice <= longStop || candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}

			_stopPrice = Math.Max(longStop, close - atr * TrailAtrMultiplier);
		}
		else if (Position < 0 && _stopPrice is decimal shortStop && _takePrice is decimal shortTake)
		{
			if (candle.HighPrice >= shortStop || candle.LowPrice <= shortTake)
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}

			_stopPrice = Math.Min(shortStop, close + atr * TrailAtrMultiplier);
		}

		if (prevMacd is not decimal pm || prevSignal is not decimal ps || width is not decimal currentWidth || _widths.Count < BbLength)
			return;

		var squeeze = currentWidth < _widths.Average();
		if (squeeze)
			return;

		var rsi = rsiValue.GetValue<decimal>();
		if (rsi <= RsiOversold || rsi >= RsiOverbought)
			return;

		var fast = emaFastValue.GetValue<decimal>();
		var slow = emaSlowValue.GetValue<decimal>();
		var stopDistance = atr * AtrMultiplier;

		if (pm <= ps && macdLine > signalLine && fast > slow && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - stopDistance;
			_takePrice = close + stopDistance * RiskReward;
		}
		else if (pm >= ps && macdLine < signalLine && fast < slow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + stopDistance;
			_takePrice = close - stopDistance * RiskReward;
		}
	}
}
