using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Evening Star candlestick pattern strategy converted from MQL5 implementation.
/// </summary>
public class EveningStarStrategy : Strategy
{
	public enum PatternDirection
	{
		Long,
		Short
	}

	private readonly StrategyParam<PatternDirection> _direction;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<bool> _considerGap;
	private readonly StrategyParam<bool> _candle2Bullish;
	private readonly StrategyParam<bool> _checkCandleSizes;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleSnapshot> _history = new();

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	public PatternDirection Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	public bool ConsiderGap
	{
		get => _considerGap.Value;
		set => _considerGap.Value = value;
	}

	public bool Candle2Bullish
	{
		get => _candle2Bullish.Value;
		set => _candle2Bullish.Value = value;
	}

	public bool CheckCandleSizes
	{
		get => _checkCandleSizes.Value;
		set => _checkCandleSizes.Value = value;
	}

	public bool CloseOppositePositions
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public EveningStarStrategy()
	{
		_direction = Param(nameof(Direction), PatternDirection.Short)
			.SetDisplay("Signal Direction", "Side to trade when the pattern appears", "General");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk Management")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk Management")
			.SetGreaterThanZero();

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetDisplay("Risk (%)", "Risk per trade as percentage of equity", "Risk Management")
			.SetGreaterThanZero();

		_shift = Param(nameof(Shift), 1)
			.SetDisplay("Shift", "Offset for the bar sequence", "Pattern")
			.SetGreaterThanZero();

		_considerGap = Param(nameof(ConsiderGap), true)
			.SetDisplay("Consider Gap", "Require price gaps between candles", "Pattern");

		_candle2Bullish = Param(nameof(Candle2Bullish), true)
			.SetDisplay("Middle Candle Bullish", "Should the second candle close above its open", "Pattern");

		_checkCandleSizes = Param(nameof(CheckCandleSizes), true)
			.SetDisplay("Check Candle Sizes", "Ensure the middle candle has the smallest body", "Pattern");

		_closeOpposite = Param(nameof(CloseOppositePositions), true)
			.SetDisplay("Close Opposite", "Close the existing opposite position before entry", "Execution");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_history.Clear();
		_pipSize = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Ensure we only process finished candles.
		if (candle.State != CandleStates.Finished)
		return;

		// Store the candle snapshot for pattern evaluation.
		_history.Add(new CandleSnapshot(candle.OpenPrice, candle.ClosePrice, candle.HighPrice, candle.LowPrice));
		TrimHistory();

		// Manage any open trade before searching for a new signal.
		HandleActivePosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// The pattern requires three completed candles with the configured shift.
		var requiredCount = Shift + 2;
		if (_history.Count < requiredCount)
		return;

		var lastIndex = _history.Count - Shift;
		if (lastIndex < 2 || lastIndex >= _history.Count)
		return;

		var recent = _history[lastIndex];
		var middle = _history[lastIndex - 1];
		var first = _history[lastIndex - 2];

		// Validate the Evening Star structure and optional filters.
		if (!IsPatternValid(first, middle, recent))
		return;

		var isLong = Direction == PatternDirection.Long;
		var entryPrice = recent.Close;
		var stopPrice = CalculateStop(entryPrice, isLong);
		var takeProfitPrice = CalculateTake(entryPrice, isLong);

		// Size the position using the risk percentage from the portfolio value.
		var volume = CalculatePositionSize(entryPrice, stopPrice);
		if (volume <= 0m)
		return;

		if (isLong)
		{
		if (Position < 0 && !CloseOppositePositions)
		return;

		var volumeToSend = volume;
		if (CloseOppositePositions && Position < 0)
		volumeToSend += Math.Abs(Position);

		// Enter a long position or flip the existing short exposure.
		BuyMarket(volumeToSend);

		_entryPrice = entryPrice;
		_stopPrice = stopPrice;
		_takeProfitPrice = takeProfitPrice;
		}
		else
		{
		if (Position > 0 && !CloseOppositePositions)
		return;

		var volumeToSend = volume;
		if (CloseOppositePositions && Position > 0)
		volumeToSend += Math.Abs(Position);

		// Enter a short position or flip the existing long exposure.
		SellMarket(volumeToSend);

		_entryPrice = entryPrice;
		_stopPrice = stopPrice;
		_takeProfitPrice = takeProfitPrice;
		}
	}

	private void HandleActivePosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
		// Nothing is open, so cached targets must be cleared.
		ResetTargets();
		return;
		}

		if (Position > 0)
		{
		var stopHit = _stopPrice > 0m && candle.LowPrice <= _stopPrice;
		var takeHit = _takeProfitPrice > 0m && candle.HighPrice >= _takeProfitPrice;

		if (stopHit || takeHit)
		{
		SellMarket(Position);
		ResetTargets();
		}
		}
		else if (Position < 0)
		{
		var stopHit = _stopPrice > 0m && candle.HighPrice >= _stopPrice;
		var takeHit = _takeProfitPrice > 0m && candle.LowPrice <= _takeProfitPrice;

		if (stopHit || takeHit)
		{
		BuyMarket(Math.Abs(Position));
		ResetTargets();
		}
		}
	}

	private bool IsPatternValid(CandleSnapshot first, CandleSnapshot middle, CandleSnapshot recent)
	{
		// Evening Star requires a bullish candle, a small-bodied candle, then a bearish candle.
		if (!(recent.Open > recent.Close && first.Open < first.Close))
		return false;

		if (CheckCandleSizes)
		{
		var lastBody = Math.Abs(recent.Open - recent.Close);
		var middleBody = Math.Abs(middle.Open - middle.Close);
		var firstBody = Math.Abs(first.Open - first.Close);

		if (lastBody < middleBody || firstBody < middleBody)
		return false;
		}

		if (Candle2Bullish)
		{
		if (middle.Open > middle.Close)
		return false;
		}
		else
		{
		if (middle.Close > middle.Open)
		return false;
		}

		if (ConsiderGap && _pipSize > 0m)
		{
		var gap = _pipSize;
		if (recent.Open >= middle.Close - gap || middle.Open <= first.Close + gap)
		return false;
		}

		return true;
	}

	private decimal CalculateStop(decimal entryPrice, bool isLong)
	{
		var distance = StopLossPips * _pipSize;
		if (distance <= 0m)
		return 0m;

		return isLong ? entryPrice - distance : entryPrice + distance;
	}

	private decimal CalculateTake(decimal entryPrice, bool isLong)
	{
		var distance = TakeProfitPips * _pipSize;
		if (distance <= 0m)
		return 0m;

		return isLong ? entryPrice + distance : entryPrice - distance;
	}

	private decimal CalculatePositionSize(decimal entryPrice, decimal stopPrice)
	{
		var distance = Math.Abs(entryPrice - stopPrice);
		if (distance <= 0m)
		return 0m;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
		return 0m;

		var riskAmount = equity * (RiskPercent / 100m);
		if (riskAmount <= 0m)
		return 0m;

		var rawVolume = riskAmount / distance;
		var step = Security.VolumeStep ?? 1m;
		var min = Security.VolumeMin ?? step;
		var max = Security.VolumeMax ?? decimal.MaxValue;

		if (step <= 0m)
		step = 1m;

		var steps = Math.Floor(rawVolume / step);
		var volume = steps * step;

		if (volume < min)
		return 0m;

		if (volume > max)
		volume = max;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
		return 0m;

		var decimals = Security.Decimals;
		// Forex symbols use fractional pips; replicate the 3/5 digit adjustment from MQL.
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private void TrimHistory()
	{
		// Keep only the most recent candles needed for pattern detection.
		var maxCount = Math.Max(Shift + 5, 10);
		if (_history.Count <= maxCount)
		return;

		var removeCount = _history.Count - maxCount;
		_history.RemoveRange(0, removeCount);
	}

	private void ResetTargets()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}

	// Lightweight snapshot to keep only the data required for pattern checks.
	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal open, decimal close, decimal high, decimal low)
		{
			Open = open;
			Close = close;
			High = high;
			Low = low;
		}

		public decimal Open { get; }
		public decimal Close { get; }
		public decimal High { get; }
		public decimal Low { get; }
	}
}
