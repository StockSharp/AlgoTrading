namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Multi-factor strategy combining MACD, RSI, ATR, and trend filters.
/// Opens long positions only on bullish MACD crossovers confirmed by RSI and long-term trend.
/// </summary>
public class MultiFactorStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopAtrMultiplier;
	private readonly StrategyParam<decimal> _profitAtrMultiplier;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _prevDiff;
	private bool _hasPrevDiff;
	private int _cooldownRemaining;
	private MovingAverageConvergenceDivergenceSignal _macd;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;
	private SMA _sma50;
	private SMA _sma200;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal StopAtrMultiplier { get => _stopAtrMultiplier.Value; set => _stopAtrMultiplier.Value = value; }
	public decimal ProfitAtrMultiplier { get => _profitAtrMultiplier.Value; set => _profitAtrMultiplier.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

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
			.SetDisplay("ATR Length", "ATR period", "ATR");

		_stopAtrMultiplier = Param(nameof(StopAtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop ATR Mult", "ATR multiplier for stop", "Risk");

		_profitAtrMultiplier = Param(nameof(ProfitAtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Profit ATR Mult", "ATR multiplier for take profit", "Risk");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait after entries and exits", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		_entryPrice = 0m;
		_prevDiff = 0m;
		_hasPrevDiff = false;
		_cooldownRemaining = 0;
		_macd = null;
		_rsi = null;
		_atr = null;
		_sma50 = null;
		_sma200 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		_rsi = new() { Length = RsiLength };
		_atr = new() { Length = AtrLength };
		_sma50 = new() { Length = 50 };
		_sma200 = new() { Length = 200 };
		_entryPrice = 0m;
		_prevDiff = 0m;
		_hasPrevDiff = false;
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsiValue = _rsi.Process(candle);
		var atrValue = _atr.Process(candle);
		var sma50Value = _sma50.Process(new DecimalIndicatorValue(_sma50, candle.ClosePrice, candle.ServerTime));
		var sma200Value = _sma200.Process(new DecimalIndicatorValue(_sma200, candle.ClosePrice, candle.ServerTime));

		if (!macdValue.IsFormed || !rsiValue.IsFormed || !atrValue.IsFormed || !sma50Value.IsFormed || !sma200Value.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var macdData = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdData.Macd is not decimal macdLine || macdData.Signal is not decimal signalLine)
			return;

		var diff = macdLine - signalLine;
		var rsi = rsiValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var sma50 = sma50Value.ToDecimal();
		var sma200 = sma200Value.ToDecimal();

		if (!_hasPrevDiff)
		{
			_prevDiff = diff;
			_hasPrevDiff = true;
			return;
		}

		if (Position > 0)
		{
			var stop = _entryPrice - StopAtrMultiplier * atr;
			var target = _entryPrice + ProfitAtrMultiplier * atr;
			var bearishCross = _prevDiff >= 0m && diff < 0m;

			if (candle.LowPrice <= stop || candle.HighPrice >= target || bearishCross || candle.ClosePrice < sma50 || rsi >= 70m)
			{
				SellMarket(Position);
				_cooldownRemaining = SignalCooldownBars;
			}
		}
		else if (_cooldownRemaining == 0)
		{
			var bullishCross = _prevDiff <= 0m && diff > 0m;
			var bullishTrend = candle.ClosePrice > sma50;

			if (bullishCross && bullishTrend && rsi <= 65m)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_cooldownRemaining = SignalCooldownBars;
			}
		}

		_prevDiff = diff;
	}
}
