using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing Monster strategy using KAMA trend and RSI filter.
/// </summary>
public class TrailingMonsterStrategy : Strategy
{
	private readonly StrategyParam<int> _kamaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _barsBetweenEntries;
	private readonly StrategyParam<decimal> _trailingStopPct;
	private readonly StrategyParam<int> _delayBars;
	private readonly StrategyParam<DataType> _candleType;

	private int _barIndex;
	private int _lastTradeIndex = int.MinValue;
	private int _entryIndex;
	private decimal _prevKama;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _kamaInitialized;

	/// <summary>
	/// KAMA length.
	/// </summary>
	public int KamaLength { get => _kamaLength.Value; set => _kamaLength.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// SMA length.
	/// </summary>
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }

	/// <summary>
	/// Bars between entries.
	/// </summary>
	public int BarsBetweenEntries { get => _barsBetweenEntries.Value; set => _barsBetweenEntries.Value = value; }

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingStopPct { get => _trailingStopPct.Value; set => _trailingStopPct.Value = value; }

	/// <summary>
	/// Delay before trailing activates.
	/// </summary>
	public int DelayBars { get => _delayBars.Value; set => _delayBars.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrailingMonsterStrategy()
	{
		_kamaLength = Param(nameof(KamaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("KAMA length", "Lookback for KAMA", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI length", "Lookback for RSI", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetDisplay("RSI overbought", "Upper RSI threshold", "Strategy");

		_rsiOversold = Param(nameof(RsiOversold), 30)
			.SetDisplay("RSI oversold", "Lower RSI threshold", "Strategy");

		_smaLength = Param(nameof(SmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA length", "Filter MA length", "Indicators");

		_barsBetweenEntries = Param(nameof(BarsBetweenEntries), 3)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown bars", "Bars between entries", "Strategy");

		_trailingStopPct = Param(nameof(TrailingStopPct), 12m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing stop %", "Percent for trailing stop", "Risk");

		_delayBars = Param(nameof(DelayBars), 3)
			.SetGreaterThanZero()
			.SetDisplay("Delay bars", "Bars before trailing activates", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Type of candles", "General");
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

		_barIndex = 0;
		_lastTradeIndex = int.MinValue;
		_entryIndex = 0;
		_prevKama = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_kamaInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var kama = new KaufmanAdaptiveMovingAverage { Length = KamaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(kama, rsi, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, kama);
			DrawIndicator(area, rsi);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal kamaValue, decimal rsiValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_barIndex++;

		var bullish = false;
		var bearish = false;

		if (_kamaInitialized)
		{
		bullish = kamaValue > _prevKama;
		bearish = kamaValue < _prevKama;
		}
		else
		{
		_kamaInitialized = true;
		}

		_prevKama = kamaValue;

		var canEnter = _barIndex - _lastTradeIndex >= BarsBetweenEntries;
		var inPosition = Position != 0;
		var barsInPosition = inPosition ? _barIndex - _entryIndex : 0;
		var trailReady = barsInPosition >= DelayBars;
		var trailOffset = _entryPrice * (TrailingStopPct / 100m);

		var longSignal = rsiValue > RsiOverbought && candle.ClosePrice > smaValue && bullish && canEnter;
		var shortSignal = rsiValue < RsiOversold && candle.ClosePrice < smaValue && bearish && canEnter;

		if (longSignal)
		{
		if (Position < 0)
		BuyMarket(Math.Abs(Position));

		if (Position == 0)
		{
		BuyMarket(Volume);
		_lastTradeIndex = _barIndex;
		_entryIndex = _barIndex;
		_entryPrice = candle.ClosePrice;
		_stopPrice = _entryPrice - trailOffset;
		}
		}
		else if (shortSignal)
		{
		if (Position > 0)
		SellMarket(Position);

		if (Position == 0)
		{
		SellMarket(Volume);
		_lastTradeIndex = _barIndex;
		_entryIndex = _barIndex;
		_entryPrice = candle.ClosePrice;
		_stopPrice = _entryPrice + trailOffset;
		}
		}

		if (inPosition && trailReady)
		{
		if (Position > 0)
		{
		var trail = candle.ClosePrice - trailOffset;
		if (trail > _stopPrice)
		_stopPrice = trail;

		if (candle.LowPrice <= _stopPrice)
		SellMarket(Position);
		}
		else if (Position < 0)
		{
		var trail = candle.ClosePrice + trailOffset;
		if (trail < _stopPrice)
		_stopPrice = trail;

		if (candle.HighPrice >= _stopPrice)
		BuyMarket(-Position);
		}
		}
	}
}
