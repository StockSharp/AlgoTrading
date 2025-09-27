
using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Variable Moving Average (VarMovAvg) reversal strategy converted from the MetaTrader expert.
/// Tracks adaptive VMA swings and enters on the Bar A/Bar B breakout pattern.
/// </summary>
public class VarMovAvgStrategy : Strategy
{
	/// <summary>
	/// MetaTrader moving average methods supported by the stop calculation.
	/// </summary>
	public enum MovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}

	private readonly StrategyParam<int> _amaPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _smoothingPower;
	private readonly StrategyParam<decimal> _signalPipsBarA;
	private readonly StrategyParam<decimal> _signalPipsBarB;
	private readonly StrategyParam<decimal> _signalPipsTrade;
	private readonly StrategyParam<decimal> _entryPipsDiff;
	private readonly StrategyParam<decimal> _stopPipsDiff;
	private readonly StrategyParam<int> _stopMaPeriod;
	private readonly StrategyParam<int> _stopMaShift;
	private readonly StrategyParam<MovingAverageMethod> _stopMaMethod;
	private readonly StrategyParam<DataType> _candleType;

	private VariableMovingAverage _vma = null!;
	private IIndicator _stopLowMa = null!;
	private IIndicator _stopHighMa = null!;
	private readonly Queue<decimal> _lowMaValues = new();
	private readonly Queue<decimal> _highMaValues = new();
	private readonly SignalTracker _longSignal = new(true);
	private readonly SignalTracker _shortSignal = new(false);

	/// <summary>
	/// VMA adaptive window length.
	/// </summary>
	public int AmaPeriod
	{
		get => _amaPeriod.Value;
		set => _amaPeriod.Value = value;
	}

	/// <summary>
	/// Fast smoothing period used inside the VMA efficiency ratio.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothing period used inside the VMA efficiency ratio.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Power applied to the smoothing coefficient (MetaTrader parameter G).
	/// </summary>
	public decimal SmoothingPower
	{
		get => _smoothingPower.Value;
		set => _smoothingPower.Value = value;
	}

	/// <summary>
	/// Distance in pips required for Bar A confirmation.
	/// </summary>
	public decimal SignalPipsBarA
	{
		get => _signalPipsBarA.Value;
		set => _signalPipsBarA.Value = value;
	}

	/// <summary>
	/// Additional distance in pips required for Bar B confirmation.
	/// </summary>
	public decimal SignalPipsBarB
	{
		get => _signalPipsBarB.Value;
		set => _signalPipsBarB.Value = value;
	}

	/// <summary>
	/// Offset in pips between the Bar B extreme and the actual entry line.
	/// </summary>
	public decimal SignalPipsTrade
	{
		get => _signalPipsTrade.Value;
		set => _signalPipsTrade.Value = value;
	}

	/// <summary>
	/// Width in pips accepted when price touches the entry line.
	/// </summary>
	public decimal EntryPipsDiff
	{
		get => _entryPipsDiff.Value;
		set => _entryPipsDiff.Value = value;
	}

	/// <summary>
	/// Offset in pips applied to the trailing stop moving average.
	/// </summary>
	public decimal StopPipsDiff
	{
		get => _stopPipsDiff.Value;
		set => _stopPipsDiff.Value = value;
	}

	/// <summary>
	/// Period of the trailing stop moving average.
	/// </summary>
	public int StopMaPeriod
	{
		get => _stopMaPeriod.Value;
		set => _stopMaPeriod.Value = value;
	}

	/// <summary>
	/// Shift (in bars) applied to the trailing stop moving average.
	/// </summary>
	public int StopMaShift
	{
		get => _stopMaShift.Value;
		set => _stopMaShift.Value = value;
	}

	/// <summary>
	/// Moving average method used for trailing stop calculation.
	/// </summary>
	public MovingAverageMethod StopMaMethod
	{
		get => _stopMaMethod.Value;
		set => _stopMaMethod.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the VarMovAvg strategy.
	/// </summary>
	public VarMovAvgStrategy()
	{
		_amaPeriod = Param(nameof(AmaPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 120, 10);

		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast smoothing period for VMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 15, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow smoothing period for VMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(15, 60, 5);

		_smoothingPower = Param(nameof(SmoothingPower), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Power", "Exponent applied to the smoothing coefficient", "Indicators");

		_signalPipsBarA = Param(nameof(SignalPipsBarA), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bar A Distance", "Pips distance below/above VMA for Bar A", "Signals");

		_signalPipsBarB = Param(nameof(SignalPipsBarB), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Bar B Distance", "Extra pips distance for Bar B confirmation", "Signals");

		_signalPipsTrade = Param(nameof(SignalPipsTrade), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Offset", "Pips offset from Bar B extreme to entry", "Signals");

		_entryPipsDiff = Param(nameof(EntryPipsDiff), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Band", "Accepted pips range around the entry price", "Signals");

		_stopPipsDiff = Param(nameof(StopPipsDiff), 34m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Offset", "Pips offset from the trailing moving average", "Risk");

		_stopMaPeriod = Param(nameof(StopMaPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Stop MA Period", "Period of the trailing moving average", "Risk");

		_stopMaShift = Param(nameof(StopMaShift), 0)
			.SetNotNegative()
			.SetDisplay("Stop MA Shift", "Bars shift applied to the stop moving average", "Risk");

		_stopMaMethod = Param(nameof(StopMaMethod), MovingAverageMethod.Exponential)
			.SetDisplay("Stop MA Method", "Moving average type used for stops", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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

		_lowMaValues.Clear();
		_highMaValues.Clear();
		_longSignal.Reset();
		_shortSignal.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_vma = new VariableMovingAverage
		{
			Length = AmaPeriod,
			FastPeriod = FastPeriod,
			SlowPeriod = SlowPeriod,
			SmoothingPower = SmoothingPower
		};

		_stopLowMa = CreateMovingAverage(StopMaMethod, StopMaPeriod);
		_stopHighMa = CreateMovingAverage(StopMaMethod, StopMaPeriod);

		_lowMaValues.Clear();
		_highMaValues.Clear();
		_longSignal.Reset();
		_shortSignal.Reset();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.CloseTime;
		var vmaValue = _vma.Process(candle.ClosePrice, time, true).GetValue<decimal>();
		var lowMaRaw = _stopLowMa.Process(candle.LowPrice, time, true).GetValue<decimal>();
		var highMaRaw = _stopHighMa.Process(candle.HighPrice, time, true).GetValue<decimal>();

		var lowMa = GetShiftedValue(_lowMaValues, lowMaRaw, StopMaShift);
		var highMa = GetShiftedValue(_highMaValues, highMaRaw, StopMaShift);

		var barADistance = ToPriceDistance(SignalPipsBarA);
		var barBDistance = ToPriceDistance(SignalPipsBarB);
		var tradeOffset = ToPriceDistance(SignalPipsTrade);
		var entryBand = ToPriceDistance(EntryPipsDiff);
		var stopOffset = ToPriceDistance(StopPipsDiff);

		_longSignal.Update(candle, vmaValue, barADistance, barBDistance, tradeOffset);
		_shortSignal.Update(candle, vmaValue, barADistance, barBDistance, tradeOffset);

		if (Position == 0)
		{
			if (Volume > 0 && _longSignal.TryEnter(candle, entryBand))
			{
				BuyMarket(Volume);
				AfterEntry();
			}
			else if (Volume > 0 && _shortSignal.TryEnter(candle, entryBand))
			{
				SellMarket(Volume);
				AfterEntry();
			}
			return;
		}

		if (Position > 0)
		{
			if (Volume > 0 && _shortSignal.TryEnter(candle, entryBand))
			{
				var volumeToSell = Position + Volume;
				if (volumeToSell > 0)
					SellMarket(volumeToSell);
				AfterEntry();
				return;
			}

			var stopPrice = lowMa - stopOffset;
			if (stopPrice > 0m && candle.LowPrice <= stopPrice)
			{
				SellMarket(Position);
				AfterExit();
			}
		}
		else
		{
			if (Volume > 0 && _longSignal.TryEnter(candle, entryBand))
			{
				var volumeToBuy = Math.Abs(Position) + Volume;
				if (volumeToBuy > 0)
					BuyMarket(volumeToBuy);
				AfterEntry();
				return;
			}

			var stopPrice = highMa + stopOffset;
			if (stopPrice > 0m && candle.HighPrice >= stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				AfterExit();
			}
		}
	}

	private void AfterEntry()
	{
		_longSignal.Reset();
		_shortSignal.Reset();
	}

	private void AfterExit()
	{
		_longSignal.Reset();
		_shortSignal.Reset();
	}

	private decimal ToPriceDistance(decimal pips)
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? pips * step : pips;
	}

	private static decimal GetShiftedValue(Queue<decimal> buffer, decimal value, int shift)
	{
		buffer.Enqueue(value);

		var maxCount = Math.Max(1, shift + 1);
		while (buffer.Count > maxCount)
			buffer.Dequeue();

		var index = buffer.Count - 1 - Math.Min(shift, buffer.Count - 1);
		var current = 0;
		foreach (var item in buffer)
		{
			if (current == index)
				return item;
			current++;
		}

		return value;
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length }
		};
	}

	private sealed class SignalTracker
	{
		private enum SignalState
		{
			Neutral,
			BarA,
			BarB
		}

		private readonly bool _isLong;
		private SignalState _state = SignalState.Neutral;
		private decimal _barAReference;
		private decimal _entryPrice;

		public SignalTracker(bool isLong)
		{
			_isLong = isLong;
		}

		public void Reset()
		{
			_state = SignalState.Neutral;
			_barAReference = 0m;
			_entryPrice = 0m;
		}

		public void Update(ICandleMessage candle, decimal vma, decimal barAOffset, decimal barBOffset, decimal tradeOffset)
		{
			var close = candle.ClosePrice;
			var high = candle.HighPrice;
			var low = candle.LowPrice;

			if (_isLong)
			{
				if (close <= vma - barAOffset)
				{
					Reset();
					return;
				}

				switch (_state)
				{
					case SignalState.Neutral:
						if (close >= vma + barAOffset)
						{
							_state = SignalState.BarA;
							_barAReference = close;
						}
						break;
					case SignalState.BarA:
						if (close <= vma - barAOffset)
						{
							Reset();
							return;
						}

						if (close >= _barAReference + barBOffset)
						{
							_state = SignalState.BarB;
							_entryPrice = high + tradeOffset;
						}
						break;
					case SignalState.BarB:
						if (close <= vma - barAOffset)
							Reset();
						break;
				}
			}
			else
			{
				if (close >= vma + barAOffset)
				{
					Reset();
					return;
				}

				switch (_state)
				{
					case SignalState.Neutral:
						if (close <= vma - barAOffset)
						{
							_state = SignalState.BarA;
							_barAReference = close;
						}
						break;
					case SignalState.BarA:
						if (close >= vma + barAOffset)
						{
							Reset();
							return;
						}

						if (close <= _barAReference - barBOffset)
						{
							_state = SignalState.BarB;
							_entryPrice = low - tradeOffset;
						}
						break;
					case SignalState.BarB:
						if (close >= vma + barAOffset)
							Reset();
						break;
				}
			}
		}

		public bool TryEnter(ICandleMessage candle, decimal entryBand)
		{
			if (_state != SignalState.BarB)
				return false;

			var close = candle.ClosePrice;

			if (_isLong)
			{
				var upper = _entryPrice + entryBand;
				if (close >= _entryPrice && close <= upper)
				{
					Reset();
					return true;
				}
			}
			else
			{
				var lower = _entryPrice - entryBand;
				if (close <= _entryPrice && close >= lower)
				{
					Reset();
					return true;
				}
			}

			return false;
		}
	}

	private sealed class VariableMovingAverage : LengthIndicator<decimal>
	{
		private readonly Queue<decimal> _closes = new();
		private decimal? _previousAma;

		public int FastPeriod { get; set; } = 5;
		public int SlowPeriod { get; set; } = 20;
		public decimal SmoothingPower { get; set; } = 1m;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = input.GetValue<decimal>();
			_closes.Enqueue(value);

			var required = Math.Max(2, Length + 1);
			while (_closes.Count > required)
				_closes.Dequeue();

			if (_previousAma == null)
				_previousAma = value;

			var closeCount = _closes.Count;
			if (closeCount < 2)
				return new DecimalIndicatorValue(this, value, input.Time);

			var effectiveLength = Math.Min(Length, closeCount - 1);
			var closes = _closes.ToArray();
			var newestIndex = closes.Length - 1;
			var baseIndex = newestIndex - effectiveLength;
			if (baseIndex < 0)
				baseIndex = 0;

			var newest = closes[newestIndex];
			var oldest = closes[baseIndex];
			var signal = Math.Abs(newest - oldest);

			decimal noise = 0.000000001m;
			for (var i = baseIndex; i < newestIndex; i++)
				noise += Math.Abs(closes[i + 1] - closes[i]);

			var efficiency = noise != 0m ? signal / noise : 0m;
			var slowSc = 2m / (SlowPeriod + 1m);
			var fastSc = 2m / (FastPeriod + 1m);
			var smoothing = slowSc + efficiency * (fastSc - slowSc);
			var smoothingFactor = smoothing > 0m ? (decimal)Math.Pow((double)smoothing, (double)SmoothingPower) : 0m;

			var amaPrev = _previousAma ?? oldest;
			var ama = amaPrev + smoothingFactor * (value - amaPrev);
			_previousAma = ama;
			IsFormed = closeCount >= required;

			return new DecimalIndicatorValue(this, ama, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_closes.Clear();
			_previousAma = null;
		}
	}
}

