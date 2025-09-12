using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// PresentTrend Strategy - uses ATR with RSI or MFI to build adaptive trend line.
/// </summary>
public class PresentTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<string> _tradeDirection;

	private AverageTrueRange _atr;
	private RelativeStrengthIndex _rsi;
	private MoneyFlowIndex _mfi;

	private decimal _presentTrend;
	private decimal _presentTrendPrev;
	private int _trendDirection;
	private int _lastLongIndex = int.MinValue;
	private int _lastShortIndex = int.MinValue;
	private int _barIndex;

	public PresentTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation.", "General");

		_length = Param(nameof(Length), 14)
			.SetDisplay("Length", "ATR and oscillator period.", "PresentTrend")
			.SetCanOptimize();

		_multiplier = Param(nameof(Multiplier), 1.618m)
			.SetDisplay("Multiplier", "ATR multiplier.", "PresentTrend")
			.SetCanOptimize();

		_useRsi = Param(nameof(UseRsi), false)
			.SetDisplay("Use RSI", "Use RSI instead of MFI.", "PresentTrend");

		_tradeDirection = Param(nameof(TradeDirection), "Both")
			.SetDisplay("Trade Direction", "Allowed trade direction (Long/Short/Both).", "PresentTrend");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public bool UseRsi { get => _useRsi.Value; set => _useRsi.Value = value; }
	public string TradeDirection { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new() { Length = Length };
		_rsi = new() { Length = Length };
		_mfi = new() { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, _rsi, _mfi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal rsi, decimal mfi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upperThreshold = candle.LowPrice - atr * Multiplier;
		var lowerThreshold = candle.HighPrice + atr * Multiplier;
		var indicator = UseRsi ? rsi : mfi;

		var prev = _presentTrend;
		var prev2 = _presentTrendPrev;

		decimal newValue;

		if (indicator >= 50m)
		{
			var candidate = upperThreshold;
			newValue = candidate < prev ? prev : candidate;
		}
		else
		{
			var candidate = lowerThreshold;
			newValue = candidate > prev ? prev : candidate;
		}

		var hasHistory = _barIndex >= 2;
		var longSignal = hasHistory && prev < prev2 && newValue > prev2;
		var shortSignal = hasHistory && prev > prev2 && newValue < prev2;

		var prevLong = _lastLongIndex;
		var prevShort = _lastShortIndex;

		if (longSignal && prevLong < _lastShortIndex)
			_trendDirection = 1;
		else if (shortSignal && prevShort < _lastLongIndex)
			_trendDirection = -1;

		if (longSignal)
			_lastLongIndex = _barIndex;
		if (shortSignal)
			_lastShortIndex = _barIndex;

		if (_trendDirection == 1 && (TradeDirection == "Long" || TradeDirection == "Both"))
		{
			if (Position <= 0)
				BuyMarket();
		}
		else if (_trendDirection == -1 && (TradeDirection == "Short" || TradeDirection == "Both"))
		{
			if (Position >= 0)
				SellMarket();
		}

		if (TradeDirection == "Long" && _trendDirection == -1 && Position > 0)
			ClosePosition();
		else if (TradeDirection == "Short" && _trendDirection == 1 && Position < 0)
			ClosePosition();

		_presentTrendPrev = prev;
		_presentTrend = newValue;
		_barIndex++;
	}
}
