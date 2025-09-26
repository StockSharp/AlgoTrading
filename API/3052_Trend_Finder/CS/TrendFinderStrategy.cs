using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe trend following strategy converted from the original TREND FINDER MQL script.
/// Combines weighted moving averages with higher timeframe momentum and MACD confirmation.
/// </summary>
public class TrendFinderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThresholdBuy;
	private readonly StrategyParam<decimal> _momentumThresholdSell;
	private readonly StrategyParam<int> _macdShortLength;
	private readonly StrategyParam<int> _macdLongLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _breakEvenTrigger;
	private readonly StrategyParam<decimal> _breakEvenOffset;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly List<ICandleMessage> _baseCandles = new();
	private readonly Queue<decimal> _momentumCloses = new();
	private readonly Queue<decimal> _momentumDiffs = new();
	private readonly List<(decimal macd, decimal signal)> _macdValues = new();

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal _breakEvenStop;
	private bool _breakEvenActive;

	/// <summary>
	/// Base timeframe for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used to compute momentum confirmation.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to evaluate the MACD confirmation.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Length of the fast linear weighted moving average.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow linear weighted moving average.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Period for the momentum calculation on the higher timeframe.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum momentum difference from 100 to allow long trades.
	/// </summary>
	public decimal MomentumThresholdBuy
	{
		get => _momentumThresholdBuy.Value;
		set => _momentumThresholdBuy.Value = value;
	}

	/// <summary>
	/// Minimum momentum difference from 100 to allow short trades.
	/// </summary>
	public decimal MomentumThresholdSell
	{
		get => _momentumThresholdSell.Value;
		set => _momentumThresholdSell.Value = value;
	}

	/// <summary>
	/// Fast EMA length inside the MACD calculation.
	/// </summary>
	public int MacdShortLength
	{
		get => _macdShortLength.Value;
		set => _macdShortLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length inside the MACD calculation.
	/// </summary>
	public int MacdLongLength
	{
		get => _macdLongLength.Value;
		set => _macdLongLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length for the MACD line.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Absolute stop-loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Absolute take-profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance that locks in profits.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Profit distance that activates the break-even stop.
	/// </summary>
	public decimal BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	/// <summary>
	/// Offset applied when the break-even stop is activated.
	/// </summary>
	public decimal BreakEvenOffset
	{
		get => _breakEvenOffset.Value;
		set => _breakEvenOffset.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TrendFinderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Base Candle Type", "Primary timeframe for entries", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Momentum Candle Type", "Higher timeframe used for momentum", "Confirmation");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candle Type", "Timeframe for MACD confirmation", "Confirmation");

		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA Length", "Length of the fast weighted moving average", "Indicator")
			.SetCanOptimize(true);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA Length", "Length of the slow weighted moving average", "Indicator")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Number of bars used for momentum", "Confirmation")
			.SetCanOptimize(true);

		_momentumThresholdBuy = Param(nameof(MomentumThresholdBuy), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Threshold Buy", "Minimum deviation from 100 for longs", "Confirmation")
			.SetCanOptimize(true);

		_momentumThresholdSell = Param(nameof(MomentumThresholdSell), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Threshold Sell", "Minimum deviation from 100 for shorts", "Confirmation")
			.SetCanOptimize(true);

		_macdShortLength = Param(nameof(MacdShortLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Length", "Short EMA length inside MACD", "Confirmation")
			.SetCanOptimize(true);

		_macdLongLength = Param(nameof(MacdLongLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Length", "Long EMA length inside MACD", "Confirmation")
			.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Length", "Signal EMA length inside MACD", "Confirmation")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 0.0020m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Absolute loss distance in price", "Risk")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 0.0050m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Absolute profit target in price", "Risk")
			.SetCanOptimize(true);

		_trailingStop = Param(nameof(TrailingStop), 0.0040m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Distance used to trail profitable trades", "Risk")
			.SetCanOptimize(true);

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 0.0030m)
			.SetGreaterThanZero()
			.SetDisplay("Break Even Trigger", "Profit needed to move stop to break even", "Risk")
			.SetCanOptimize(true);

		_breakEvenOffset = Param(nameof(BreakEvenOffset), 0.0010m)
			.SetGreaterThanZero()
			.SetDisplay("Break Even Offset", "Extra offset applied when break even activates", "Risk")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var seen = new HashSet<DataType>();
		if (seen.Add(CandleType))
			yield return (Security, CandleType);
		if (seen.Add(MomentumCandleType))
			yield return (Security, MomentumCandleType);
		if (seen.Add(MacdCandleType))
			yield return (Security, MacdCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_baseCandles.Clear();
		_momentumCloses.Clear();
		_momentumDiffs.Clear();
		_macdValues.Clear();
		ResetRiskTracking();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaLength };
		_slowMa = new WeightedMovingAverage { Length = SlowMaLength };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdShortLength },
				LongMa = { Length = MacdLongLength }
			},
			SignalMa = { Length = MacdSignalLength }
		};

		_baseCandles.Clear();
		_momentumCloses.Clear();
		_momentumDiffs.Clear();
		_macdValues.Clear();
		ResetRiskTracking();

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription.Bind(ProcessBaseCandle).Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription.Bind(ProcessMomentumCandle).Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription.Bind(ProcessMacdCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawOwnTrades(area);
			DrawIndicator(area, _fastMa, "Fast LWMA");
			DrawIndicator(area, _slowMa, "Slow LWMA");
		}
	}

	private void ProcessMomentumCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store closes to compute the momentum ratio similar to the MQL implementation.
		_momentumCloses.Enqueue(candle.ClosePrice);
		while (_momentumCloses.Count > MomentumPeriod + 3)
			_momentumCloses.Dequeue();

		if (_momentumCloses.Count <= MomentumPeriod)
			return;

		var closes = _momentumCloses.ToArray();
		var currentIndex = closes.Length - 1;
		var pastIndex = currentIndex - MomentumPeriod;
		if (pastIndex < 0)
			return;
		var current = closes[currentIndex];
		var past = closes[pastIndex];
		if (past == 0m)
			return;

		var momentum = current / past * 100m;
		var diff = Math.Abs(momentum - 100m);
		_momentumDiffs.Enqueue(diff);
		while (_momentumDiffs.Count > 3)
			_momentumDiffs.Dequeue();
	}

	private void ProcessMacdCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// MACD is calculated on the selected higher timeframe.
		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)_macd.Process(candle.ClosePrice, candle.CloseTime, true);
		if (macdValue.Macd is not decimal macd || macdValue.Signal is not decimal signal)
			return;

		_macdValues.Add((macd, signal));
		if (_macdValues.Count > 3)
			_macdValues.RemoveAt(0);
	}

	private void ProcessBaseCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var fastValue = _fastMa.Process(typicalPrice, candle.CloseTime, true);
		var slowValue = _slowMa.Process(typicalPrice, candle.CloseTime, true);
		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			TrackBaseCandle(candle);
			ManageRisk(candle);
			return;
		}

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		TrackBaseCandle(candle);
		ManageRisk(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_momentumDiffs.Count < 3 || _macdValues.Count == 0)
			return;

		var prev1 = GetPreviousCandle(1);
		var prev2 = GetPreviousCandle(2);
		if (prev1 is null || prev2 is null)
			return;

		var trendBreakout = DetermineTrendBreakout(candle);
		var macd = _macdValues[^1];

		if (trendBreakout == 1
			&& fast > slow
			&& prev2.LowPrice < prev1.HighPrice
			&& HasMomentumForLong()
			&& macd.macd > macd.signal
			&& Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			SetLongEntryState(candle);
		}
		else if (trendBreakout == 2
			&& fast < slow
			&& prev1.LowPrice < prev2.HighPrice
			&& HasMomentumForShort()
			&& macd.macd < macd.signal
			&& Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			SetShortEntryState(candle);
		}
	}

	private void TrackBaseCandle(ICandleMessage candle)
	{
		_baseCandles.Add(candle);
		if (_baseCandles.Count > 150)
			_baseCandles.RemoveAt(0);
	}

	private ICandleMessage GetPreviousCandle(int shift)
	{
		var index = _baseCandles.Count - 1 - shift;
		if (index < 0)
			return null;
		return _baseCandles[index];
	}

	private int DetermineTrendBreakout(ICandleMessage current)
	{
		if (_baseCandles.Count < 4)
			return 0;

		var prev1 = GetPreviousCandle(1);
		var prev2 = GetPreviousCandle(2);
		var prev3 = GetPreviousCandle(3);
		if (prev1 is null || prev2 is null || prev3 is null)
			return 0;

		var limit = Math.Min(100, _baseCandles.Count - 3);
		for (var lookback = 3; lookback <= limit; lookback++)
		{
			var maxHigh = decimal.MinValue;
			var minLow = decimal.MaxValue;
			var start = _baseCandles.Count - 3 - lookback;
			if (start < 0)
				continue;

			for (var i = start; i < _baseCandles.Count - 3; i++)
			{
				var candle = _baseCandles[i];
				if (candle.HighPrice > maxHigh)
					maxHigh = candle.HighPrice;
				if (candle.LowPrice < minLow)
					minLow = candle.LowPrice;
			}

			if ((prev1.HighPrice < maxHigh || prev2.HighPrice < maxHigh || prev3.HighPrice < maxHigh)
				&& current.OpenPrice > maxHigh)
				return 1;

			if ((prev1.LowPrice > minLow || prev2.LowPrice > minLow || prev3.LowPrice > minLow)
				&& current.OpenPrice < minLow)
				return 2;
		}

		return 0;
	}

	private bool HasMomentumForLong()
	{
		foreach (var diff in _momentumDiffs)
		{
			if (diff >= MomentumThresholdBuy)
				return true;
		}

		return false;
	}

	private bool HasMomentumForShort()
	{
		foreach (var diff in _momentumDiffs)
		{
			if (diff >= MomentumThresholdSell)
				return true;
		}

		return false;
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);

			if (TakeProfit > 0m && candle.ClosePrice >= _entryPrice + TakeProfit)
			{
				CloseLong();
				return;
			}

			if (BreakEvenTrigger > 0m && !_breakEvenActive && _highestPrice - _entryPrice >= BreakEvenTrigger)
			{
				_breakEvenActive = true;
				_breakEvenStop = _entryPrice + BreakEvenOffset;
			}

			decimal? exitStop = null;
			if (StopLoss > 0m)
				exitStop = _entryPrice - StopLoss;

			if (TrailingStop > 0m)
			{
				var trailing = _highestPrice - TrailingStop;
				exitStop = exitStop.HasValue ? Math.Max(exitStop.Value, trailing) : trailing;
			}

			if (_breakEvenActive)
				exitStop = exitStop.HasValue ? Math.Max(exitStop.Value, _breakEvenStop) : _breakEvenStop;

			if (exitStop.HasValue && candle.ClosePrice <= exitStop.Value)
			{
				CloseLong();
			}
		}
		else if (Position < 0)
		{
			_lowestPrice = _lowestPrice == 0m ? candle.LowPrice : Math.Min(_lowestPrice, candle.LowPrice);

			if (TakeProfit > 0m && candle.ClosePrice <= _entryPrice - TakeProfit)
			{
				CloseShort();
				return;
			}

			if (BreakEvenTrigger > 0m && !_breakEvenActive && _entryPrice - _lowestPrice >= BreakEvenTrigger)
			{
				_breakEvenActive = true;
				_breakEvenStop = _entryPrice - BreakEvenOffset;
			}

			decimal? exitStop = null;
			if (StopLoss > 0m)
				exitStop = _entryPrice + StopLoss;

			if (TrailingStop > 0m)
			{
				var trailing = _lowestPrice + TrailingStop;
				exitStop = exitStop.HasValue ? Math.Min(exitStop.Value, trailing) : trailing;
			}

			if (_breakEvenActive)
				exitStop = exitStop.HasValue ? Math.Min(exitStop.Value, _breakEvenStop) : _breakEvenStop;

			if (exitStop.HasValue && candle.ClosePrice >= exitStop.Value)
			{
				CloseShort();
			}
		}
	}

	private void SetLongEntryState(ICandleMessage candle)
	{
		_entryPrice = candle.ClosePrice;
		_highestPrice = candle.HighPrice;
		_lowestPrice = candle.LowPrice;
		_breakEvenActive = false;
		_breakEvenStop = 0m;
	}

	private void SetShortEntryState(ICandleMessage candle)
	{
		_entryPrice = candle.ClosePrice;
		_lowestPrice = candle.LowPrice;
		_highestPrice = candle.HighPrice;
		_breakEvenActive = false;
		_breakEvenStop = 0m;
	}

	private void CloseLong()
	{
		if (Position <= 0)
			return;

		SellMarket(Position);
		ResetRiskTracking();
	}

	private void CloseShort()
	{
		if (Position >= 0)
			return;

		BuyMarket(-Position);
		ResetRiskTracking();
	}

	private void ResetRiskTracking()
	{
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_breakEvenActive = false;
		_breakEvenStop = 0m;
	}
}
