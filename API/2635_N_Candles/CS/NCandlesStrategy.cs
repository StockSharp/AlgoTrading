using System;
using System.Collections.Generic;
using StockSharp.Algo;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Available position accounting modes to control stacking logic.
/// </summary>
public enum PositionAccountingMode
{
	/// <summary>
	/// Netting mode limits total net position volume.
	/// </summary>
	Netting,

	/// <summary>
	/// Hedging mode limits the number of entries per direction.
	/// </summary>
	Hedging
}

/// <summary>
/// Strategy that opens positions after detecting N identical candles in a row.
/// Handles optional stop-loss, take-profit, and trailing stop management in pips.
/// </summary>
public class NCandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _consecutiveCandles;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _maxPositionsPerDirection;
	private readonly StrategyParam<decimal> _maxNetVolume;
	private readonly StrategyParam<PositionAccountingMode> _accountingMode;
	private readonly StrategyParam<DataType> _candleType;

	private int _consecutiveDirection;
	private int _consecutiveCount;

	private decimal _pipSize;

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal _longHighestPrice;
	private decimal _shortLowestPrice;

	private decimal? _longStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfitPrice;

	private int _longPositionCount;
	private int _shortPositionCount;

	/// <summary>
	/// Initializes a new instance of the <see cref="NCandlesStrategy"/> class.
	/// </summary>
	public NCandlesStrategy()
	{
		_consecutiveCandles = Param(nameof(ConsecutiveCandles), 3)
			.SetGreaterThanZero()
			.SetDisplay("Consecutive Candles", "Number of identical candles in a row", "Entry")
			.SetCanOptimize(true)
			.SetOptimize(2, 6, 1);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 10m);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 10m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 4m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Additional distance before shifting the trail", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_maxPositionsPerDirection = Param(nameof(MaxPositionsPerDirection), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum entries in one direction for hedging", "Risk");

		_maxNetVolume = Param(nameof(MaxNetVolume), 2m)
			.SetNotNegative()
			.SetDisplay("Max Net Volume", "Maximum aggregate position size for netting", "Risk");

		_accountingMode = Param(nameof(AccountingMode), PositionAccountingMode.Netting)
			.SetDisplay("Accounting Mode", "Select between netting or hedging style limits", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyze", "General");
	}

	/// <summary>
	/// Number of identical candles required before entering a trade.
	/// </summary>
	public int ConsecutiveCandles
	{
		get => _consecutiveCandles.Value;
		set => _consecutiveCandles.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step that controls how far price must move before shifting the trail.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of allowed entries in one direction while hedging.
	/// </summary>
	public int MaxPositionsPerDirection
	{
		get => _maxPositionsPerDirection.Value;
		set => _maxPositionsPerDirection.Value = value;
	}

	/// <summary>
	/// Maximum aggregate volume when operating in netting mode.
	/// </summary>
	public decimal MaxNetVolume
	{
		get => _maxNetVolume.Value;
		set => _maxNetVolume.Value = value;
	}

	/// <summary>
	/// Determines whether limits are enforced by volume (netting) or entry count (hedging).
	/// </summary>
	public PositionAccountingMode AccountingMode
	{
		get => _accountingMode.Value;
		set => _accountingMode.Value = value;
	}

	/// <summary>
	/// Candle type used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_consecutiveDirection = 0;
		_consecutiveCount = 0;
		_pipSize = 0m;

		_longPositionCount = 0;
		_shortPositionCount = 0;

		ResetLongProtection();
		ResetShortProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = Security?.Decimals ?? 0;
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailing(candle);
		CheckExits(candle);

		var direction = GetCandleDirection(candle);

		if (direction == 0)
		{
			_consecutiveDirection = 0;
			_consecutiveCount = 0;
			return;
		}

		if (direction == _consecutiveDirection)
		{
			_consecutiveCount++;
		}
		else
		{
			_consecutiveDirection = direction;
			_consecutiveCount = 1;
		}

		if (_consecutiveCount < ConsecutiveCandles)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (direction > 0)
		{
			TryEnterLong();
		}
		else
		{
			TryEnterShort();
		}
	}

	private void TryEnterLong()
	{
		if (Volume <= 0)
			return;

		if (AccountingMode == PositionAccountingMode.Netting && MaxNetVolume > 0)
		{
			var nextVolume = Position.Abs() + Volume;
			if (nextVolume > MaxNetVolume)
				return;
		}
		else if (AccountingMode == PositionAccountingMode.Hedging)
		{
			if (MaxPositionsPerDirection > 0 && _longPositionCount >= MaxPositionsPerDirection)
				return;
		}

		BuyMarket(Volume);
	}

	private void TryEnterShort()
	{
		if (Volume <= 0)
			return;

		if (AccountingMode == PositionAccountingMode.Netting && MaxNetVolume > 0)
		{
			var nextVolume = Position.Abs() + Volume;
			if (nextVolume > MaxNetVolume)
				return;
		}
		else if (AccountingMode == PositionAccountingMode.Hedging)
		{
			if (MaxPositionsPerDirection > 0 && _shortPositionCount >= MaxPositionsPerDirection)
				return;
		}

		SellMarket(Volume);
	}

	private int GetCandleDirection(ICandleMessage candle)
	{
		if (candle.ClosePrice > candle.OpenPrice)
			return 1;
		if (candle.ClosePrice < candle.OpenPrice)
			return -1;
		return 0;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			_longEntryPrice = Position.AveragePrice;
			_longHighestPrice = Math.Max(_longHighestPrice, _longEntryPrice);
			_longStopPrice = StopLossPips > 0 ? NormalizePrice(_longEntryPrice - StopLossPips * _pipSize) : null;
			_longTakeProfitPrice = TakeProfitPips > 0 ? NormalizePrice(_longEntryPrice + TakeProfitPips * _pipSize) : null;
			_longPositionCount = GetPositionCount(Position.Abs());
			_shortPositionCount = 0;
			ResetShortProtection();
		}
		else if (Position < 0)
		{
			_shortEntryPrice = Position.AveragePrice;
			_shortLowestPrice = _shortLowestPrice == 0m ? _shortEntryPrice : Math.Min(_shortLowestPrice, _shortEntryPrice);
			_shortStopPrice = StopLossPips > 0 ? NormalizePrice(_shortEntryPrice + StopLossPips * _pipSize) : null;
			_shortTakeProfitPrice = TakeProfitPips > 0 ? NormalizePrice(_shortEntryPrice - TakeProfitPips * _pipSize) : null;
			_shortPositionCount = GetPositionCount(Position.Abs());
			_longPositionCount = 0;
			ResetLongProtection();
		}
		else
		{
			_longPositionCount = 0;
			_shortPositionCount = 0;
			ResetLongProtection();
			ResetShortProtection();
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0)
			return;

		var trailingOffset = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0)
		{
			_longHighestPrice = Math.Max(_longHighestPrice, candle.HighPrice);

			if (!_longStopPrice.HasValue && StopLossPips <= 0 && _longEntryPrice > 0)
			{
				if (_longHighestPrice - trailingOffset > _longEntryPrice)
				{
					// Activate break-even stop once price moves enough in favor.
					_longStopPrice = NormalizePrice(_longEntryPrice);
				}
			}
			else if (_longStopPrice is decimal currentStop && _longEntryPrice > 0)
			{
				var desiredStop = NormalizePrice(_longHighestPrice - trailingOffset);
				if (desiredStop - trailingStep > currentStop)
				{
					// Shift trailing stop closer to current price while keeping distance.
					_longStopPrice = desiredStop;
				}
			}
		}
		else if (Position < 0)
		{
			_shortLowestPrice = _shortLowestPrice == 0m ? candle.LowPrice : Math.Min(_shortLowestPrice, candle.LowPrice);

			if (!_shortStopPrice.HasValue && StopLossPips <= 0 && _shortEntryPrice > 0)
			{
				if (_shortLowestPrice + trailingOffset < _shortEntryPrice)
				{
					// Activate break-even stop for short position once price drops enough.
					_shortStopPrice = NormalizePrice(_shortEntryPrice);
				}
			}
			else if (_shortStopPrice is decimal currentStop && _shortEntryPrice > 0)
			{
				var desiredStop = NormalizePrice(_shortLowestPrice + trailingOffset);
				if (desiredStop + trailingStep < currentStop)
				{
					// Shift trailing stop lower as price advances in favor of the short.
					_shortStopPrice = desiredStop;
				}
			}
		}
	}

	private void CheckExits(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var volume = Position.Abs();
			if (volume <= 0)
				return;

			var stopPrice = _longStopPrice;
			var takePrice = _longTakeProfitPrice;
			var open = candle.OpenPrice;
			var low = candle.LowPrice;
			var high = candle.HighPrice;

			var stopHit = stopPrice.HasValue && (low <= stopPrice.Value || open <= stopPrice.Value);
			var takeHit = takePrice.HasValue && (high >= takePrice.Value || open >= takePrice.Value);

			if (stopHit || takeHit)
			{
				var reason = stopHit ? "Stop" : "TakeProfit";
				LogInfo($"Closing long position via {reason} at candle {candle.OpenTime:O}.");
				SellMarket(volume);
				ResetLongProtection();
			}
		}
		else if (Position < 0)
		{
			var volume = Position.Abs();
			if (volume <= 0)
				return;

			var stopPrice = _shortStopPrice;
			var takePrice = _shortTakeProfitPrice;
			var open = candle.OpenPrice;
			var low = candle.LowPrice;
			var high = candle.HighPrice;

			var stopHit = stopPrice.HasValue && (high >= stopPrice.Value || open >= stopPrice.Value);
			var takeHit = takePrice.HasValue && (low <= takePrice.Value || open <= takePrice.Value);

			if (stopHit || takeHit)
			{
				var reason = stopHit ? "Stop" : "TakeProfit";
				LogInfo($"Closing short position via {reason} at candle {candle.OpenTime:O}.");
				BuyMarket(volume);
				ResetShortProtection();
			}
		}
	}

	private int GetPositionCount(decimal volume)
	{
		if (Volume <= 0 || volume <= 0)
			return 0;

		var ratio = volume / Volume;
		return ratio <= 0 ? 0 : (int)Math.Ceiling(ratio);
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return Math.Max(0m, price);

		var normalized = Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
		return Math.Max(0m, normalized);
	}

	private void ResetLongProtection()
	{
		_longEntryPrice = 0m;
		_longHighestPrice = 0m;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void ResetShortProtection()
	{
		_shortEntryPrice = 0m;
		_shortLowestPrice = 0m;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}
}
