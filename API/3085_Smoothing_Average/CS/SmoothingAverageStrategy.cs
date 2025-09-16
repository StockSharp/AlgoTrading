using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smoothing Average strategy converted from MQL5.
/// Opens trades when price moves away from the moving average by a configurable delta.
/// Supports reversing the signals and shifting the moving average output.
/// </summary>
public class SmoothingAverageStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageKind> _maType;
	private readonly StrategyParam<CandlePrice> _priceSource;
	private readonly StrategyParam<decimal> _entryDeltaPips;
	private readonly StrategyParam<decimal> _closeDeltaCoefficient;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _tradeVolume;

	private readonly Queue<decimal> _maShiftBuffer = new();

	private decimal _entryDelta;
	private decimal _closeDelta;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SmoothingAverageStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");

		_maLength = Param(nameof(MaLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Period of the smoothing average", "Moving Average");

		_maShift = Param(nameof(MaShift), 3)
			.SetGreaterThanOrEqualToZero()
			.SetDisplay("MA Shift", "Horizontal shift applied to the average", "Moving Average");

		_maType = Param(nameof(MaType), MovingAverageKind.Simple)
			.SetDisplay("MA Type", "Type of smoothing applied", "Moving Average");

		_priceSource = Param(nameof(PriceSource), CandlePrice.Typical)
			.SetDisplay("Price Source", "Price used for the moving average", "Moving Average");

		_entryDeltaPips = Param(nameof(EntryDeltaPips), 60m)
			.SetGreaterThanOrEqualToZero()
			.SetDisplay("Entry Delta (pips)", "Distance from MA to trigger entries", "Trading Rules");

		_closeDeltaCoefficient = Param(nameof(CloseDeltaCoefficient), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Close Delta Coefficient", "Multiplier applied to entry delta for exits", "Trading Rules");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert long and short logic", "Trading Rules");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume for each entry", "Risk");
	}

	/// <summary>
	/// Primary candle series used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Number of candles used to shift the moving average output.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MovingAverageKind MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Candle price source for the moving average.
	/// </summary>
	public CandlePrice PriceSource
	{
		get => _priceSource.Value;
		set => _priceSource.Value = value;
	}

	/// <summary>
	/// Delta in pip units used to open new positions.
	/// </summary>
	public decimal EntryDeltaPips
	{
		get => _entryDeltaPips.Value;
		set => _entryDeltaPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the entry delta when evaluating exits.
	/// </summary>
	public decimal CloseDeltaCoefficient
	{
		get => _closeDeltaCoefficient.Value;
		set => _closeDeltaCoefficient.Value = value;
	}

	/// <summary>
	/// If true, swaps long and short signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Volume sent with market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
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

		_maShiftBuffer.Clear();
		_entryDelta = 0m;
		_closeDelta = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Sync the base strategy volume with the parameter value.
		Volume = TradeVolume;

		// Calculate pip-based offsets once at the start to avoid repeated computations.
		_entryDelta = CalculateEntryDelta();
		_closeDelta = _entryDelta * CloseDeltaCoefficient;

		var movingAverage = CreateMovingAverage(MaType, MaLength);
		movingAverage.CandlePrice = PriceSource;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(movingAverage, ProcessCandle)
			.Start();

		// Enable built-in protection helpers (no additional parameters required).
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var shiftedMa = ApplyShift(maValue);

		// Use candle close as a proxy for bid/ask checks from the original Expert Advisor.
		var askPrice = candle.ClosePrice;
		var bidPrice = candle.ClosePrice;

		var entryUpper = shiftedMa + _entryDelta;
		var entryLower = shiftedMa - _entryDelta;
		var closeUpper = shiftedMa + _closeDelta;
		var closeLower = shiftedMa - _closeDelta;

		if (Position == 0m)
		{
			if (!ReverseSignals)
			{
				if (askPrice > entryLower)
				{
					OpenLong();
					return;
				}

				if (bidPrice < entryUpper)
				{
					OpenShort();
					return;
				}
			}
			else
			{
				if (askPrice > entryLower)
				{
					OpenShort();
					return;
				}

				if (bidPrice < entryUpper)
				{
					OpenLong();
					return;
				}
			}
		}
		else
		{
			if (!ReverseSignals)
			{
				if (Position < 0m && bidPrice > closeUpper)
					CloseShort();

				if (Position > 0m && askPrice < closeLower)
					CloseLong();
			}
			else
			{
				if (Position > 0m && askPrice < closeLower)
					CloseLong();

				if (Position < 0m && bidPrice > closeUpper)
					CloseShort();
			}
		}
	}

	private decimal ApplyShift(decimal currentValue)
	{
		if (MaShift <= 0)
			return currentValue;

		var shifted = _maShiftBuffer.Count < MaShift ? currentValue : _maShiftBuffer.Peek();

		_maShiftBuffer.Enqueue(currentValue);

		if (_maShiftBuffer.Count > MaShift)
			_maShiftBuffer.Dequeue();

		return shifted;
	}

	private decimal CalculateEntryDelta()
	{
		var pip = CalculatePipSize();
		return pip * EntryDeltaPips;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		var digits = (int)Math.Round(Math.Log10((double)(1m / step)));
		return digits == 3 || digits == 5 ? step * 10m : step;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageKind type, int length)
	{
		return type switch
		{
			MovingAverageKind.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageKind.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageKind.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageKind.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private void OpenLong()
	{
		var volume = TradeVolume + Math.Max(0m, -Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
	}

	private void OpenShort()
	{
		var volume = TradeVolume + Math.Max(0m, Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);
	}

	private void CloseLong()
	{
		if (Position <= 0m)
			return;

		SellMarket(Position);
	}

	private void CloseShort()
	{
		if (Position >= 0m)
			return;

		BuyMarket(Math.Abs(Position));
	}

	/// <summary>
	/// Supported moving average types replicating the MQL5 enumeration.
	/// </summary>
	public enum MovingAverageKind
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}
}
