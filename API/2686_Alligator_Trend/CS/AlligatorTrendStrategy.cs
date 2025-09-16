
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Classic Bill Williams Alligator strategy with stop management rules.
/// </summary>
/// <remarks>
/// The strategy opens a long position when the Lips, Teeth, and Jaw lines are aligned upward
/// and opens a short position when they are aligned downward. Once in a trade the algorithm
/// applies the zero level rule to move the stop to break-even, updates a trailing stop with a
/// configurable step, and closes the position at the stop or take-profit levels.
/// </remarks>
public class AlligatorTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _zeroLevelPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;

	private readonly Queue<decimal> _jawBuffer = new();
	private readonly Queue<decimal> _teethBuffer = new();
	private readonly Queue<decimal> _lipsBuffer = new();

	private decimal? _longStop;
	private decimal? _longTake;
	private bool _longBreakevenActivated;
	private decimal _longBestPrice;

	private decimal? _shortStop;
	private decimal? _shortTake;
	private bool _shortBreakevenActivated;
	private decimal _shortBestPrice;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Jaw length.
	/// </summary>
	public int JawLength
	{
		get => _jawLength.Value;
		set => _jawLength.Value = value;
	}

	/// <summary>
	/// Teeth length.
	/// </summary>
	public int TeethLength
	{
		get => _teethLength.Value;
		set => _teethLength.Value = value;
	}

	/// <summary>
	/// Lips length.
	/// </summary>
	public int LipsLength
	{
		get => _lipsLength.Value;
		set => _lipsLength.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw line.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the teeth line.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the lips line.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
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
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance to move the stop to break-even.
	/// </summary>
	public decimal ZeroLevelPips
	{
		get => _zeroLevelPips.Value;
		set => _zeroLevelPips.Value = value;
	}

	/// <summary>
	/// Distance between price extreme and trailing stop.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum increment for trailing stop updates.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AlligatorTrendStrategy"/> class.
	/// </summary>
	public AlligatorTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");

		_jawLength = Param(nameof(JawLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Length", "Smoothed moving average period for the jaw", "Alligator")
			.SetCanOptimize(true);

		_teethLength = Param(nameof(TeethLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Length", "Smoothed moving average period for the teeth", "Alligator")
			.SetCanOptimize(true);

		_lipsLength = Param(nameof(LipsLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Length", "Smoothed moving average period for the lips", "Alligator")
			.SetCanOptimize(true);

		_jawShift = Param(nameof(JawShift), 8)
			.SetDisplay("Jaw Shift", "Forward shift applied to the jaw line", "Alligator")
			.SetCanOptimize(true);

		_teethShift = Param(nameof(TeethShift), 5)
			.SetDisplay("Teeth Shift", "Forward shift applied to the teeth line", "Alligator")
			.SetCanOptimize(true);

		_lipsShift = Param(nameof(LipsShift), 3)
			.SetDisplay("Lips Shift", "Forward shift applied to the lips line", "Alligator")
			.SetCanOptimize(true);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long entries", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short entries", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 45m)
			.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 145m)
			.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true);

		_zeroLevelPips = Param(nameof(ZeroLevelPips), 30m)
			.SetDisplay("Zero Level", "Distance to move stop to break-even", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 50m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 10m)
			.SetDisplay("Trailing Step", "Minimum trailing stop increment in pips", "Risk")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var jaw = new SmoothedMovingAverage { Length = JawLength };
		var teeth = new SmoothedMovingAverage { Length = TeethLength };
		var lips = new SmoothedMovingAverage { Length = LipsLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jaw);
			DrawIndicator(area, teeth);
			DrawIndicator(area, lips);
			DrawOwnTrades(area);
		}

		StartProtection();

		void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;

			var jawValue = jaw.Process(medianPrice);
			var teethValue = teeth.Process(medianPrice);
			var lipsValue = lips.Process(medianPrice);

			if (!jawValue.IsFormed || !teethValue.IsFormed || !lipsValue.IsFormed)
				return;

			var jawShifted = GetShiftedValue(_jawBuffer, jawValue.GetValue<decimal>(), JawShift);
			var teethShifted = GetShiftedValue(_teethBuffer, teethValue.GetValue<decimal>(), TeethShift);
			var lipsShifted = GetShiftedValue(_lipsBuffer, lipsValue.GetValue<decimal>(), LipsShift);

			if (!jawShifted.HasValue || !teethShifted.HasValue || !lipsShifted.HasValue)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (ManagePosition(candle))
				return;

			var bullishAlignment = lipsShifted.Value > teethShifted.Value && teethShifted.Value > jawShifted.Value;
			var bearishAlignment = lipsShifted.Value < teethShifted.Value && teethShifted.Value < jawShifted.Value;

			if (Position == 0)
			{
				if (bullishAlignment && EnableLong)
				{
					BuyMarket();
				}
				else if (bearishAlignment && EnableShort)
				{
					SellMarket();
				}
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var price = trade.Trade?.Price;
		var direction = trade.Order?.Direction;

		if (price is null || direction is null)
			return;

		var distanceToStop = StopLossPips > 0m ? GetPriceByPips(StopLossPips) : (decimal?)null;
		var distanceToTake = TakeProfitPips > 0m ? GetPriceByPips(TakeProfitPips) : (decimal?)null;

		if (direction == Sides.Buy)
		{
			if (Position > 0)
			{
				_longStop = distanceToStop.HasValue ? price.Value - distanceToStop.Value : null;
				_longTake = distanceToTake.HasValue ? price.Value + distanceToTake.Value : null;
				_longBreakevenActivated = false;
				_longBestPrice = price.Value;
			}
			else if (Position == 0)
			{
				ResetShort();
			}
		}
		else if (direction == Sides.Sell)
		{
			if (Position < 0)
			{
				_shortStop = distanceToStop.HasValue ? price.Value + distanceToStop.Value : null;
				_shortTake = distanceToTake.HasValue ? price.Value - distanceToTake.Value : null;
				_shortBreakevenActivated = false;
				_shortBestPrice = price.Value;
			}
			else if (Position == 0)
			{
				ResetLong();
			}
		}

		if (Position == 0)
		{
			ResetLong();
			ResetShort();
		}
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var entryPrice = Position.AveragePrice;
			if (entryPrice == 0m)
				return false;

			_longBestPrice = Math.Max(_longBestPrice, candle.HighPrice);

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Position);
				ResetLong();
				return true;
			}

			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Position);
				ResetLong();
				return true;
			}

			if (ZeroLevelPips > 0m && !_longBreakevenActivated && _longStop.HasValue && entryPrice > _longStop.Value)
			{
				var zeroDistance = GetPriceByPips(ZeroLevelPips);
				if (_longBestPrice - entryPrice >= zeroDistance)
				{
					_longStop = entryPrice;
					_longBreakevenActivated = true;
				}
			}

			if (TrailingStopPips > 0m)
			{
				var trailingDistance = GetPriceByPips(TrailingStopPips);
				var step = GetPriceByPips(TrailingStepPips);
				var candidate = _longBestPrice - trailingDistance;

				if (!_longStop.HasValue || candidate - _longStop.Value >= step)
					_longStop = candidate;
			}
		}
		else if (Position < 0)
		{
			var entryPrice = Position.AveragePrice;
			if (entryPrice == 0m)
				return false;

			_shortBestPrice = _shortBestPrice == 0m ? candle.LowPrice : Math.Min(_shortBestPrice, candle.LowPrice);

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShort();
				return true;
			}

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShort();
				return true;
			}

			if (ZeroLevelPips > 0m && !_shortBreakevenActivated && _shortStop.HasValue && entryPrice < _shortStop.Value)
			{
				var zeroDistance = GetPriceByPips(ZeroLevelPips);
				if (entryPrice - candle.LowPrice >= zeroDistance)
				{
					_shortStop = entryPrice;
					_shortBreakevenActivated = true;
				}
			}

			if (TrailingStopPips > 0m)
			{
				var trailingDistance = GetPriceByPips(TrailingStopPips);
				var step = GetPriceByPips(TrailingStepPips);
				var candidate = _shortBestPrice + trailingDistance;

				if (!_shortStop.HasValue || _shortStop.Value - candidate >= step)
					_shortStop = candidate;
			}
		}
		else
		{
			ResetLong();
			ResetShort();
		}

		return false;
	}

	private static decimal? GetShiftedValue(Queue<decimal> buffer, decimal value, int shift)
	{
		if (shift <= 0)
			return value;

		buffer.Enqueue(value);

		if (buffer.Count <= shift)
			return null;

		return buffer.Dequeue();
	}

	private decimal GetPriceByPips(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		return pips * step;
	}

	private void ResetLong()
	{
		_longStop = null;
		_longTake = null;
		_longBreakevenActivated = false;
		_longBestPrice = 0m;
	}

	private void ResetShort()
	{
		_shortStop = null;
		_shortTake = null;
		_shortBreakevenActivated = false;
		_shortBestPrice = 0m;
	}
}
