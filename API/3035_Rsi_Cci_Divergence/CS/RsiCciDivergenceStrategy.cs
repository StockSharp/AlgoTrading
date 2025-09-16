using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI and CCI divergence strategy with multi-timeframe MACD confirmation.
/// </summary>
public class RsiCciDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _candlesToRetrace;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _higherMacdCandleType;
	private readonly StrategyParam<DataType> _macroMacdCandleType;

	private readonly List<decimal> _recentHighs = new();
	private readonly List<decimal> _recentLows = new();
	private readonly List<decimal> _recentCci = new();
	private readonly List<decimal> _recentRsi = new();
	private readonly Queue<decimal> _momentumValues = new();

	private bool _hasHigherMacd;
	private bool _hasMacroMacd;
	private bool _hasMomentum;
	private bool _isHigherMacdBullish;
	private bool _isHigherMacdBearish;
	private bool _isMacroMacdBullish;
	private bool _isMacroMacdBearish;

	private decimal _entryPrice;

	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }
	public int CciLength { get => _cciLength.Value; set => _cciLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int CandlesToRetrace { get => _candlesToRetrace.Value; set => _candlesToRetrace.Value = value; }
	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }
	public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }
	public int MomentumLength { get => _momentumLength.Value; set => _momentumLength.Value = value; }
	public decimal MomentumBuyThreshold { get => _momentumBuyThreshold.Value; set => _momentumBuyThreshold.Value = value; }
	public decimal MomentumSellThreshold { get => _momentumSellThreshold.Value; set => _momentumSellThreshold.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DataType MomentumCandleType { get => _momentumCandleType.Value; set => _momentumCandleType.Value = value; }
	public DataType HigherMacdCandleType { get => _higherMacdCandleType.Value; set => _higherMacdCandleType.Value = value; }
	public DataType MacroMacdCandleType { get => _macroMacdCandleType.Value; set => _macroMacdCandleType.Value = value; }

	public RsiCciDivergenceStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetDisplay("Fast LWMA Length", "Length of the fast linear weighted moving average", "Trend Filter");

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetDisplay("Slow LWMA Length", "Length of the slow linear weighted moving average", "Trend Filter");

		_cciLength = Param(nameof(CciLength), 14)
			.SetDisplay("CCI Length", "Number of periods for Commodity Channel Index", "Oscillators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "Number of periods for Relative Strength Index", "Oscillators");

		_candlesToRetrace = Param(nameof(CandlesToRetrace), 10)
			.SetDisplay("Divergence Lookback", "How many completed candles to scan for divergences", "Oscillators");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast", "Fast moving average length for MACD", "MACD");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "Slow moving average length for MACD", "MACD");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal moving average length for MACD", "MACD");

		_momentumLength = Param(nameof(MomentumLength), 14)
			.SetDisplay("Momentum Length", "Number of periods for the momentum filter", "Momentum");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetDisplay("Momentum Buy Threshold", "Minimum deviation from 100 required to confirm bullish momentum", "Momentum");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetDisplay("Momentum Sell Threshold", "Minimum deviation from 100 required to confirm bearish momentum", "Momentum");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Absolute price distance for protective stop loss (0 disables)", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Absolute price distance for profit target (0 disables)", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary Candle Type", "Main timeframe for divergence detection", "Data");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Momentum Candle Type", "Timeframe used for momentum confirmation", "Data");

		_higherMacdCandleType = Param(nameof(HigherMacdCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Higher MACD Candle Type", "Secondary timeframe for MACD confirmation", "Data");

		_macroMacdCandleType = Param(nameof(MacroMacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("Macro MACD Candle Type", "Macro timeframe for MACD confirmation", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, MomentumCandleType);
		yield return (Security, HigherMacdCandleType);
		yield return (Security, MacroMacdCandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_recentHighs.Clear();
		_recentLows.Clear();
		_recentCci.Clear();
		_recentRsi.Clear();
		_momentumValues.Clear();

		_hasHigherMacd = false;
		_hasMacroMacd = false;
		_hasMomentum = false;
		_isHigherMacdBullish = false;
		_isHigherMacdBearish = false;
		_isMacroMacdBullish = false;
		_isMacroMacdBearish = false;
		_entryPrice = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = new WeightedMovingAverage { Length = FastMaLength };
		var slowMa = new WeightedMovingAverage { Length = SlowMaLength };
		var cci = new CommodityChannelIndex { Length = CciLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = CreateMacd();

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(macd, fastMa, slowMa, cci, rsi, ProcessMainCandle)
			.Start();

		var higherMacd = CreateMacd();
		var higherSubscription = SubscribeCandles(HigherMacdCandleType);
		higherSubscription
			.Bind(higherMacd, ProcessHigherMacd)
			.Start();

		var macroMacd = CreateMacd();
		var macroSubscription = SubscribeCandles(MacroMacdCandleType);
		macroSubscription
			.Bind(macroMacd, ProcessMacroMacd)
			.Start();

		var momentum = new Momentum { Length = MomentumLength };
		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
			.Bind(momentum, ProcessMomentum)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);

			var macdArea = CreateChartArea();
			if (macdArea != null)
			{
				DrawIndicator(macdArea, macd);
				DrawIndicator(macdArea, cci);
				DrawIndicator(macdArea, rsi);
			}

			DrawOwnTrades(area);
		}
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd()
	{
		return new MovingAverageConvergenceDivergenceSignal
		{
				Macd =
				{
					ShortMa = { Length = MacdFastPeriod },
					LongMa = { Length = MacdSlowPeriod },
				},
				SignalMa = { Length = MacdSignalPeriod }
		};
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal macd, decimal signal, decimal histogram, decimal fastMa, decimal slowMa, decimal cci, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateRecentSeries(candle, cci, rsi);

		if (!_hasHigherMacd || !_hasMacroMacd || !_hasMomentum)
			return;

		var hasBullishDivergence = HasBullishDivergence(_recentCci, _recentHighs) || HasBullishDivergence(_recentRsi, _recentHighs);
		var hasBearishDivergence = HasBearishDivergence(_recentCci, _recentHighs) || HasBearishDivergence(_recentRsi, _recentHighs);

		var trendBullish = fastMa > slowMa;
		var trendBearish = fastMa < slowMa;
		var macdBullish = macd > signal;
		var macdBearish = macd < signal;
		var momentumBullish = CheckMomentum(MomentumBuyThreshold);
		var momentumBearish = CheckMomentum(MomentumSellThreshold);

		var hasPrevHigh = TryGetPreviousHigh(out var previousHigh);
		var hasLowTwoAgo = TryGetLowTwoAgo(out var lowTwoAgo);
		var hasPrevLow = TryGetPreviousLow(out var previousLow);
		var hasHighTwoAgo = TryGetHighTwoAgo(out var highTwoAgo);

		if (hasPrevHigh && hasLowTwoAgo && trendBullish && macdBullish && _isHigherMacdBullish && _isMacroMacdBullish && momentumBullish && hasBullishDivergence && lowTwoAgo < previousHigh && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (hasPrevLow && hasHighTwoAgo && trendBearish && macdBearish && _isHigherMacdBearish && _isMacroMacdBearish && momentumBearish && hasBearishDivergence && previousLow < highTwoAgo && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			{
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}

		CheckRiskManagement(candle);
	}

	private void ProcessHigherMacd(ICandleMessage candle, decimal macd, decimal signal, decimal histogram)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_hasHigherMacd = true;
		_isHigherMacdBullish = macd > signal;
		_isHigherMacdBearish = macd < signal;
	}

	private void ProcessMacroMacd(ICandleMessage candle, decimal macd, decimal signal, decimal histogram)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_hasMacroMacd = true;
		_isMacroMacdBullish = macd > signal;
		_isMacroMacdBearish = macd < signal;
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_momentumValues.Enqueue(momentum);
		while (_momentumValues.Count > 3)
			_momentumValues.Dequeue();

		_hasMomentum = _momentumValues.Count >= 3;
	}

	private void UpdateRecentSeries(ICandleMessage candle, decimal cci, decimal rsi)
	{
		_recentHighs.Add(candle.HighPrice);
		_recentLows.Add(candle.LowPrice);
		_recentCci.Add(cci);
		_recentRsi.Add(rsi);

		var maxSize = CandlesToRetrace + 5;
		TrimSeries(_recentHighs, maxSize);
		TrimSeries(_recentLows, maxSize);
		TrimSeries(_recentCci, maxSize);
		TrimSeries(_recentRsi, maxSize);
	}

	private static void TrimSeries(List<decimal> values, int maxSize)
	{
		var extra = values.Count - maxSize;
		if (extra <= 0)
			return;

		values.RemoveRange(0, extra);
	}

	private bool HasBullishDivergence(IReadOnlyList<decimal> indicator, IReadOnlyList<decimal> highs)
	{
		var currentIndex = indicator.Count - 1;
		if (currentIndex <= 0)
			return false;

		var previousHigh = highs[currentIndex];
		var maxLookback = Math.Min(CandlesToRetrace, currentIndex);
		for (var offset = 1; offset <= maxLookback; offset++)
		{
			var compareIndex = currentIndex - offset;
			if (indicator[currentIndex] > indicator[compareIndex] && previousHigh < highs[compareIndex])
				return true;
		}

		return false;
	}

	private bool HasBearishDivergence(IReadOnlyList<decimal> indicator, IReadOnlyList<decimal> highs)
	{
		var currentIndex = indicator.Count - 1;
		if (currentIndex <= 0)
			return false;

		var previousHigh = highs[currentIndex];
		var maxLookback = Math.Min(CandlesToRetrace, currentIndex);
		for (var offset = 1; offset <= maxLookback; offset++)
		{
			var compareIndex = currentIndex - offset;
			if (indicator[currentIndex] < indicator[compareIndex] && previousHigh > highs[compareIndex])
				return true;
		}

		return false;
	}

	private bool CheckMomentum(decimal threshold)
	{
		if (!_hasMomentum || threshold <= 0m)
			return false;

		foreach (var value in _momentumValues)
		{
			if (Math.Abs(value - 100m) >= threshold)
				return true;
		}

		return false;
	}

	private bool TryGetPreviousHigh(out decimal value)
	{
		if (_recentHighs.Count < 1)
		{
			value = 0m;
			return false;
		}

		value = _recentHighs[^1];
		return true;
	}

	private bool TryGetLowTwoAgo(out decimal value)
	{
		if (_recentLows.Count < 2)
		{
			value = 0m;
			return false;
		}

		value = _recentLows[^2];
		return true;
	}

	private bool TryGetPreviousLow(out decimal value)
	{
		if (_recentLows.Count < 1)
		{
			value = 0m;
			return false;
		}

		value = _recentLows[^1];
		return true;
	}

	private bool TryGetHighTwoAgo(out decimal value)
	{
		if (_recentHighs.Count < 2)
		{
			value = 0m;
			return false;
		}

		value = _recentHighs[^2];
		return true;
	}

	private void CheckRiskManagement(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var positionVolume = Position;
			if (TakeProfit > 0m && candle.HighPrice >= _entryPrice + TakeProfit)
			{
				SellMarket(positionVolume);
				_entryPrice = 0m;
				return;
			}

			if (StopLoss > 0m && candle.LowPrice <= _entryPrice - StopLoss)
			{
				SellMarket(positionVolume);
				_entryPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			var positionVolume = Math.Abs(Position);
			if (TakeProfit > 0m && candle.LowPrice <= _entryPrice - TakeProfit)
			{
				BuyMarket(positionVolume);
				_entryPrice = 0m;
				return;
			}

			if (StopLoss > 0m && candle.HighPrice >= _entryPrice + StopLoss)
			{
				BuyMarket(positionVolume);
				_entryPrice = 0m;
			}
		}
	}
}
