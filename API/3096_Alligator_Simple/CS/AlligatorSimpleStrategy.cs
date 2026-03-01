using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified Bill Williams Alligator breakout strategy with pip-based risk management.
/// Buys when the Lips, Teeth, and Jaw lines expand upward on the previous candle.
/// Sells when the lines stack downward and manages optional stop-loss, take-profit, and trailing stop levels.
/// </summary>
public class AlligatorSimpleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw = null!;
	private SmoothedMovingAverage _teeth = null!;
	private SmoothedMovingAverage _lips = null!;

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal _prevPosition;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Trade volume expressed in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips converted using the symbol price step.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Zero disables the profit target.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Zero disables trailing logic.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional pip move required before advancing the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Period for the Alligator jaw (blue) smoothed moving average.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw value when evaluating signals.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Period for the Alligator teeth (red) smoothed moving average.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the teeth value when evaluating signals.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Period for the Alligator lips (green) smoothed moving average.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the lips value when evaluating signals.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Candle data type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="AlligatorSimpleStrategy"/>.
	/// </summary>
	public AlligatorSimpleStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Trade size in lots or contracts", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 100)
			.SetDisplay("Stop Loss (pips)", "Initial stop-loss distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetDisplay("Take Profit (pips)", "Initial take-profit distance", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Extra distance before trailing adjusts", "Risk");

		_jawPeriod = Param(nameof(JawPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Period", "Alligator jaw period", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
			.SetDisplay("Jaw Shift", "Forward shift for the jaw", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Period", "Alligator teeth period", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
			.SetDisplay("Teeth Shift", "Forward shift for the teeth", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Period", "Alligator lips period", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
			.SetDisplay("Lips Shift", "Forward shift for the lips", "Alligator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candle subscription", "General");
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

		_pipSize = 0m;
		_entryPrice = 0m;
		_prevPosition = 0m;

		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		_jaw = new SmoothedMovingAverage { Length = JawPeriod };
		_teeth = new SmoothedMovingAverage { Length = TeethPeriod };
		_lips = new SmoothedMovingAverage { Length = LipsPeriod };

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		var delta = Position - _prevPosition;
		_prevPosition = Position;

		if (Position == 0)
		{
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_longExitRequested = false;
			_shortExitRequested = false;
			return;
		}

		var entryPrice = _entryPrice;

		if (Position > 0 && delta > 0)
		{
			_longStopPrice = StopLossPips > 0 ? entryPrice - StopLossPips * _pipSize : (decimal?)null;
			_longTakePrice = TakeProfitPips > 0 ? entryPrice + TakeProfitPips * _pipSize : (decimal?)null;
			_longExitRequested = false;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_shortExitRequested = false;
		}
		else if (Position < 0 && delta < 0)
		{
			_shortStopPrice = StopLossPips > 0 ? entryPrice + StopLossPips * _pipSize : (decimal?)null;
			_shortTakePrice = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * _pipSize : (decimal?)null;
			_shortExitRequested = false;
			_longStopPrice = null;
			_longTakePrice = null;
			_longExitRequested = false;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position > 0)
		{
			ManageLong(candle);
		}
		else if (Position < 0)
		{
			ManageShort(candle);
		}

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawValue = _jaw.Process(new DecimalIndicatorValue(_jaw, median, candle.OpenTime) { IsFinal = true });
		var teethValue = _teeth.Process(new DecimalIndicatorValue(_teeth, median, candle.OpenTime) { IsFinal = true });
		var lipsValue = _lips.Process(new DecimalIndicatorValue(_lips, median, candle.OpenTime) { IsFinal = true });

		var jaw = jawValue.ToDecimal();
		var teeth = teethValue.ToDecimal();
		var lips = lipsValue.ToDecimal();

		if (!_jaw.IsFormed || !_teeth.IsFormed || !_lips.IsFormed)
			return;

		if (Position != 0)
			return;

		// Buy when lips > teeth > jaw (Alligator opening upward)
		if (lips > teeth && teeth > jaw)
		{
			_entryPrice = candle.ClosePrice;
			BuyMarket(volume: OrderVolume);
		}
		// Sell when lips < teeth < jaw (Alligator opening downward)
		else if (lips < teeth && teeth < jaw)
		{
			_entryPrice = candle.ClosePrice;
			SellMarket(volume: OrderVolume);
		}
	}

	private void ManageLong(ICandleMessage candle)
	{
		if (_longTakePrice is decimal take && candle.HighPrice >= take)
		{
			TryCloseLong();
			return;
		}

		if (TrailingStopPips > 0)
		{
			var trailDistance = TrailingStopPips * _pipSize;
			if (trailDistance > 0m)
			{
				var stepDistance = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;
				var referencePrice = Math.Max(candle.HighPrice, candle.ClosePrice);

				if (referencePrice - _entryPrice > trailDistance + stepDistance)
				{
					var desiredStop = referencePrice - trailDistance;
					var threshold = stepDistance > 0m ? desiredStop - stepDistance : desiredStop;

					if (_longStopPrice is not decimal currentStop || currentStop < threshold)
					{
						_longStopPrice = desiredStop;
					}
				}
			}
		}

		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			TryCloseLong();
		}
	}

	private void ManageShort(ICandleMessage candle)
	{
		if (_shortTakePrice is decimal take && candle.LowPrice <= take)
		{
			TryCloseShort();
			return;
		}

		if (TrailingStopPips > 0)
		{
			var trailDistance = TrailingStopPips * _pipSize;
			if (trailDistance > 0m)
			{
				var stepDistance = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;
				var referencePrice = Math.Min(candle.LowPrice, candle.ClosePrice);

				if (_entryPrice - referencePrice > trailDistance + stepDistance)
				{
					var desiredStop = referencePrice + trailDistance;
					var threshold = stepDistance > 0m ? desiredStop + stepDistance : desiredStop;

					if (_shortStopPrice is not decimal currentStop || currentStop > threshold)
					{
						_shortStopPrice = desiredStop;
					}
				}
			}
		}

		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			TryCloseShort();
		}
	}

	private void TryCloseLong()
	{
		if (_longExitRequested)
			return;

		_longExitRequested = true;
		SellMarket(volume: Position);
	}

	private void TryCloseShort()
	{
		if (_shortExitRequested)
			return;

		_shortExitRequested = true;
		BuyMarket(volume: Math.Abs(Position));
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}
}

