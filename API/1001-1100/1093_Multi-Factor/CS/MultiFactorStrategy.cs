using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-factor strategy.
/// Goes long when MACD is above its signal, RSI is below 70, the close is above the 50-period SMA and the 50 SMA is above the 200 SMA;
/// goes short on the mirrored conditions (RSI above 30), reversing an opposite position. Each position is closed by a stop loss and a
/// take profit placed StopAtrMultiplier and ProfitAtrMultiplier ATRs away from the entry price.
/// </summary>
public class MultiFactorStrategy : Strategy
{
	private const int _trendFastLength = 50;
	private const int _trendSlowLength = 200;
	private const decimal _rsiUpper = 70m;
	private const decimal _rsiLower = 30m;

	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopAtrMultiplier;
	private readonly StrategyParam<decimal> _profitAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
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
	/// ATR multiple for the stop loss distance.
	/// </summary>
	public decimal StopAtrMultiplier
	{
		get => _stopAtrMultiplier.Value;
		set => _stopAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiple for the take profit distance.
	/// </summary>
	public decimal ProfitAtrMultiplier
	{
		get => _profitAtrMultiplier.Value;
		set => _profitAtrMultiplier.Value = value;
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
	public MultiFactorStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA length", "MACD");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA length", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal EMA length", "MACD");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_stopAtrMultiplier = Param(nameof(StopAtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("Stop ATR Mult", "ATR multiple for the stop loss", "Risk");

		_profitAtrMultiplier = Param(nameof(ProfitAtrMultiplier), 3m)
			.SetNotNegative()
			.SetDisplay("Profit ATR Mult", "ATR multiple for the take profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopPrice = null;
		_takePrice = null;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var sma50 = new SimpleMovingAverage { Length = _trendFastLength };
		var sma200 = new SimpleMovingAverage { Length = _trendSlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, rsi, atr, sma50, sma200, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma50);
			DrawIndicator(area, sma200);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, macd);
				DrawIndicator(oscillators, rsi);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue, IIndicatorValue atrValue, IIndicatorValue sma50Value, IIndicatorValue sma200Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The ATR levels are checked against the candle range, so exits happen before new signals.
		if (Position > 0 && _stopPrice is decimal longStop && _takePrice is decimal longTake)
		{
			if (candle.LowPrice <= longStop || candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
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
		}

		if (!macdValue.IsFormed || !rsiValue.IsFormed || !atrValue.IsFormed || !sma50Value.IsFormed || !sma200Value.IsFormed)
			return;

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var sma50 = sma50Value.GetValue<decimal>();
		var sma200 = sma200Value.GetValue<decimal>();
		var close = candle.ClosePrice;

		var longSignal = macdLine > signalLine && rsi < _rsiUpper && close > sma50 && sma50 > sma200;
		var shortSignal = macdLine < signalLine && rsi > _rsiLower && close < sma50 && sma50 < sma200;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			SetLevels(close, atr, true);
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			SetLevels(close, atr, false);
		}
	}

	private void SetLevels(decimal entry, decimal atr, bool isLong)
	{
		var stop = atr * StopAtrMultiplier;
		var take = atr * ProfitAtrMultiplier;

		// A zero multiplier disables that level.
		_stopPrice = StopAtrMultiplier > 0 ? (isLong ? entry - stop : entry + stop) : (isLong ? decimal.MinValue : decimal.MaxValue);
		_takePrice = ProfitAtrMultiplier > 0 ? (isLong ? entry + take : entry - take) : (isLong ? decimal.MaxValue : decimal.MinValue);
	}
}
