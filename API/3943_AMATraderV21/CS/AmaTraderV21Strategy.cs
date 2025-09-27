namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// AMA Trader v2.1 conversion that combines Kaufman AMA bursts with Heiken Ashi Smoothed and RSI filters.
/// </summary>
public class AmaTraderV21Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _amaLength;
	private readonly StrategyParam<int> _amaFastPeriod;
	private readonly StrategyParam<int> _amaSlowPeriod;
	private readonly StrategyParam<decimal> _amaPower;
	private readonly StrategyParam<decimal> _amaThreshold;
	private readonly StrategyParam<int> _firstMaPeriod;
	private readonly StrategyParam<int> _secondMaPeriod;
	private readonly StrategyParam<HeikenMaMethod> _firstMaMethod;
	private readonly StrategyParam<HeikenMaMethod> _secondMaMethod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _partialClosePercent;
	private readonly StrategyParam<int> _stopLossSteps;
	private readonly StrategyParam<int> _takeProfitSteps;
	private readonly StrategyParam<int> _trailingSteps;

	private HeikenAshiSmoothedCalculator _heiken = null!;
	private AmaSignalCalculator _ama = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal? _previousRsi;
	private decimal? _previousPreviousRsi;
	private decimal? _entryPrice;
	private decimal? _trailingStop;

	/// <summary>
	/// Initializes a new instance of the <see cref="AmaTraderV21Strategy"/> class.
	/// </summary>
	public AmaTraderV21Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for the indicator calculations.", "Data");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Trade Volume", "Base volume for new market orders.", "Trading")
		.SetGreaterThanZero();

		_amaLength = Param(nameof(AmaLength), 9)
		.SetDisplay("AMA Length", "Number of bars used for the Kaufman adaptive calculation.", "AMA")
		.SetGreaterThanZero();

		_amaFastPeriod = Param(nameof(AmaFastPeriod), 2)
		.SetDisplay("Fast Period", "Fast smoothing constant in bars.", "AMA")
		.SetGreaterThanZero();

		_amaSlowPeriod = Param(nameof(AmaSlowPeriod), 30)
		.SetDisplay("Slow Period", "Slow smoothing constant in bars.", "AMA")
		.SetGreaterThanZero();

		_amaPower = Param(nameof(AmaPower), 2m)
		.SetDisplay("AMA Power", "Power applied to the adaptive smoothing constant.", "AMA")
		.SetGreaterThanZero();

		_amaThreshold = Param(nameof(AmaThreshold), 2m)
		.SetDisplay("AMA Threshold (steps)", "Minimum AMA jump expressed in price steps to trigger signals.", "AMA")
		.SetGreaterThanZero();

		_firstMaPeriod = Param(nameof(FirstMaPeriod), 6)
		.SetDisplay("Heiken First MA", "Length of the first smoothing moving average.", "Heiken")
		.SetGreaterThanZero();

		_secondMaPeriod = Param(nameof(SecondMaPeriod), 2)
		.SetDisplay("Heiken Second MA", "Length of the second smoothing moving average.", "Heiken")
		.SetGreaterThanZero();

		_firstMaMethod = Param(nameof(FirstMaMethod), HeikenMaMethod.Smoothed)
		.SetDisplay("First MA Method", "Moving average applied to raw prices before Heiken Ashi calculation.", "Heiken");

		_secondMaMethod = Param(nameof(SecondMaMethod), HeikenMaMethod.LinearWeighted)
		.SetDisplay("Second MA Method", "Moving average used to smooth the Heiken Ashi buffers.", "Heiken");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "Number of bars for the RSI filter.", "RSI")
		.SetGreaterThanZero();

		_partialClosePercent = Param(nameof(PartialClosePercent), 70m)
		.SetDisplay("Partial Close (%)", "Percentage of the current position to close on RSI extremes.", "Risk")
		.SetNotNegative();

		_stopLossSteps = Param(nameof(StopLossSteps), 50)
		.SetDisplay("Stop Loss (steps)", "Protective stop size expressed in price steps.", "Risk")
		.SetNotNegative();

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 100)
		.SetDisplay("Take Profit (steps)", "Target size expressed in price steps.", "Risk")
		.SetNotNegative();

		_trailingSteps = Param(nameof(TrailingSteps), 30)
		.SetDisplay("Trailing Stop (steps)", "Trailing distance in price steps.", "Risk")
		.SetNotNegative();
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
	/// Base order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Length for the AMA calculation.
	/// </summary>
	public int AmaLength
	{
		get => _amaLength.Value;
		set => _amaLength.Value = value;
	}

	/// <summary>
	/// Fast AMA smoothing period.
	/// </summary>
	public int AmaFastPeriod
	{
		get => _amaFastPeriod.Value;
		set => _amaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow AMA smoothing period.
	/// </summary>
	public int AmaSlowPeriod
	{
		get => _amaSlowPeriod.Value;
		set => _amaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Power used for the AMA smoothing constant.
	/// </summary>
	public decimal AmaPower
	{
		get => _amaPower.Value;
		set => _amaPower.Value = value;
	}

	/// <summary>
	/// Minimum AMA move required to trigger a signal.
	/// </summary>
	public decimal AmaThreshold
	{
		get => _amaThreshold.Value;
		set => _amaThreshold.Value = value;
	}

	/// <summary>
	/// First Heiken Ashi smoothing period.
	/// </summary>
	public int FirstMaPeriod
	{
		get => _firstMaPeriod.Value;
		set => _firstMaPeriod.Value = value;
	}

	/// <summary>
	/// Second Heiken Ashi smoothing period.
	/// </summary>
	public int SecondMaPeriod
	{
		get => _secondMaPeriod.Value;
		set => _secondMaPeriod.Value = value;
	}

	/// <summary>
	/// First smoothing moving average method.
	/// </summary>
	public HeikenMaMethod FirstMaMethod
	{
		get => _firstMaMethod.Value;
		set => _firstMaMethod.Value = value;
	}

	/// <summary>
	/// Second smoothing moving average method.
	/// </summary>
	public HeikenMaMethod SecondMaMethod
	{
		get => _secondMaMethod.Value;
		set => _secondMaMethod.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Percentage for partial exits.
	/// </summary>
	public decimal PartialClosePercent
	{
		get => _partialClosePercent.Value;
		set => _partialClosePercent.Value = value;
	}

	/// <summary>
	/// Stop-loss size in price steps.
	/// </summary>
	public int StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit size in price steps.
	/// </summary>
	public int TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Trailing distance in price steps.
	/// </summary>
	public int TrailingSteps
	{
		get => _trailingSteps.Value;
		set => _trailingSteps.Value = value;
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

		_heiken = null!;
		_ama = null!;
		_rsi = null!;
		_previousRsi = null;
		_previousPreviousRsi = null;
		_entryPrice = null;
		_trailingStop = null;
		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_heiken = new HeikenAshiSmoothedCalculator(FirstMaMethod, SecondMaMethod, FirstMaPeriod, SecondMaPeriod);
		_ama = new AmaSignalCalculator(AmaLength, AmaFastPeriod, AmaSlowPeriod, AmaPower);
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenCandlesFinished(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var heiken = _heiken.Process(candle);
		if (heiken is null)
		return;

		var amaValue = _ama.Process(candle.ClosePrice, candle.CloseTime, true);
		if (!amaValue.HasValue)
		return;

		var rsiValue = _rsi.Process(candle.ClosePrice, candle.CloseTime, true).ToDecimal();
		if (!_rsi.IsFormed)
		{
			_previousPreviousRsi = _previousRsi;
			_previousRsi = rsiValue;
			return;
		}

		var step = Security.PriceStep ?? 1m;
		var amaThreshold = step * AmaThreshold;

		var bullishAma = amaValue.Value.Difference >= amaThreshold;
		var bearishAma = amaValue.Value.Difference <= -amaThreshold;

		var prevRsi = _previousRsi;
		var prevPrevRsi = _previousPreviousRsi;

		// Manage trailing stop updates.
		if (Position > 0 && TrailingSteps > 0)
		{
			var trailDistance = step * TrailingSteps;
			if (_entryPrice.HasValue && candle.ClosePrice > _entryPrice.Value + trailDistance)
			{
				var candidate = candle.ClosePrice - trailDistance;
				_trailingStop = _trailingStop.HasValue ? Math.Max(_trailingStop.Value, candidate) : candidate;
			}

			if (_trailingStop.HasValue && candle.LowPrice <= _trailingStop.Value)
			{
				SellMarket(Math.Abs(Position));
				_trailingStop = null;
				_entryPrice = null;
			}
		}
		else if (Position < 0 && TrailingSteps > 0)
		{
			var trailDistance = step * TrailingSteps;
			if (_entryPrice.HasValue && candle.ClosePrice < _entryPrice.Value - trailDistance)
			{
				var candidate = candle.ClosePrice + trailDistance;
				_trailingStop = _trailingStop.HasValue ? Math.Min(_trailingStop.Value, candidate) : candidate;
			}

			if (_trailingStop.HasValue && candle.HighPrice >= _trailingStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				_trailingStop = null;
				_entryPrice = null;
			}
		}

		// Manage fixed stop-loss and take-profit.
		if (Position > 0 && _entryPrice.HasValue)
		{
			if (StopLossSteps > 0)
			{
				var stopPrice = _entryPrice.Value - step * StopLossSteps;
				if (candle.LowPrice <= stopPrice)
				{
					SellMarket(Math.Abs(Position));
					_entryPrice = null;
					_trailingStop = null;
				}
			}

			if (TakeProfitSteps > 0)
			{
				var targetPrice = _entryPrice.Value + step * TakeProfitSteps;
				if (candle.HighPrice >= targetPrice)
				{
					SellMarket(Math.Abs(Position));
					_entryPrice = null;
					_trailingStop = null;
				}
			}
		}
		else if (Position < 0 && _entryPrice.HasValue)
		{
			if (StopLossSteps > 0)
			{
				var stopPrice = _entryPrice.Value + step * StopLossSteps;
				if (candle.HighPrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					_entryPrice = null;
					_trailingStop = null;
				}
			}

			if (TakeProfitSteps > 0)
			{
				var targetPrice = _entryPrice.Value - step * TakeProfitSteps;
				if (candle.LowPrice <= targetPrice)
				{
					BuyMarket(Math.Abs(Position));
					_entryPrice = null;
					_trailingStop = null;
				}
			}
		}

		// Partial close logic based on RSI extremes.
		if (PartialClosePercent > 0 && prevRsi.HasValue)
		{
			var fraction = (PartialClosePercent / 100m).Min(1m);
			if (Position > 0 && prevRsi <= 70m && rsiValue > 70m)
			{
				var volume = Math.Abs(Position) * fraction;
				if (volume > 0)
				SellMarket(volume);
			}
			else if (Position < 0 && prevRsi >= 30m && rsiValue < 30m)
			{
				var volume = Math.Abs(Position) * fraction;
				if (volume > 0)
				BuyMarket(volume);
			}
		}

		// Full exit signals from RSI.
		if (Position > 0 && prevRsi.HasValue && prevPrevRsi.HasValue)
		{
			var exitLong = rsiValue > 70m && prevRsi <= 70m;
			if (exitLong)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = null;
				_trailingStop = null;
			}
		}
		else if (Position < 0 && prevRsi.HasValue && prevPrevRsi.HasValue)
		{
			var exitShort = rsiValue < 30m && prevRsi >= 30m;
			if (exitShort)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = null;
				_trailingStop = null;
			}
		}

		var longFilter = heiken.IsBullish && prevRsi.HasValue && prevRsi.Value > rsiValue && rsiValue <= 70m;
		var shortFilter = heiken.IsBearish && prevRsi.HasValue && prevRsi.Value < rsiValue && rsiValue >= 30m;

		if (Position <= 0 && bullishAma && longFilter)
		{
			if (Position < 0)
			BuyMarket(Math.Abs(Position));

			BuyMarket(TradeVolume);
			_entryPrice = candle.ClosePrice;
			_trailingStop = null;
		}
		else if (Position >= 0 && bearishAma && shortFilter)
		{
			if (Position > 0)
			SellMarket(Math.Abs(Position));

			SellMarket(TradeVolume);
			_entryPrice = candle.ClosePrice;
			_trailingStop = null;
		}

		_previousPreviousRsi = _previousRsi;
		_previousRsi = rsiValue;
	}

	/// <summary>
	/// Available moving average methods for Heiken Ashi smoothing.
	/// </summary>
	public enum HeikenMaMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}

	private sealed class HeikenAshiSmoothedCalculator
	{
		private readonly IIndicator _openMa;
		private readonly IIndicator _closeMa;
		private readonly IIndicator _highMa;
		private readonly IIndicator _lowMa;
		private readonly IIndicator _bottomSmooth;
		private readonly IIndicator _topSmooth;
		private readonly IIndicator _openSmooth;
		private readonly IIndicator _closeSmooth;

		private decimal? _previousHaOpen;
		private decimal? _previousHaClose;

		public HeikenAshiSmoothedCalculator(HeikenMaMethod firstMethod, HeikenMaMethod secondMethod, int firstLength, int secondLength)
		{
			_openMa = CreateMovingAverage(firstMethod, firstLength);
			_closeMa = CreateMovingAverage(firstMethod, firstLength);
			_highMa = CreateMovingAverage(firstMethod, firstLength);
			_lowMa = CreateMovingAverage(firstMethod, firstLength);
			_bottomSmooth = CreateMovingAverage(secondMethod, secondLength);
			_topSmooth = CreateMovingAverage(secondMethod, secondLength);
			_openSmooth = CreateMovingAverage(secondMethod, secondLength);
			_closeSmooth = CreateMovingAverage(secondMethod, secondLength);
		}

		public HeikenResult Process(ICandleMessage candle)
		{
			var openValue = _openMa.Process(candle.OpenPrice, candle.OpenTime, true);
			var closeValue = _closeMa.Process(candle.ClosePrice, candle.OpenTime, true);
			var highValue = _highMa.Process(candle.HighPrice, candle.OpenTime, true);
			var lowValue = _lowMa.Process(candle.LowPrice, candle.OpenTime, true);

			if (!_openMa.IsFormed || !_closeMa.IsFormed || !_highMa.IsFormed || !_lowMa.IsFormed)
			return null;

			var maOpen = openValue.ToDecimal();
			var maClose = closeValue.ToDecimal();
			var maHigh = highValue.ToDecimal();
			var maLow = lowValue.ToDecimal();

			var haClose = (maOpen + maClose + maHigh + maLow) / 4m;
			var haOpen = _previousHaOpen.HasValue && _previousHaClose.HasValue
			? (_previousHaOpen.Value + _previousHaClose.Value) / 2m
			: (maOpen + maClose) / 2m;

			var haHigh = Math.Max(maHigh, Math.Max(haOpen, haClose));
			var haLow = Math.Min(maLow, Math.Min(haOpen, haClose));
			var isBullish = haClose >= haOpen;

			var lowerSource = isBullish ? haLow : haHigh;
			var upperSource = isBullish ? haHigh : haLow;

			var bottom = _bottomSmooth.Process(lowerSource, candle.OpenTime, true).ToDecimal();
			var top = _topSmooth.Process(upperSource, candle.OpenTime, true).ToDecimal();
			var openSmoothed = _openSmooth.Process(haOpen, candle.OpenTime, true).ToDecimal();
			var closeSmoothed = _closeSmooth.Process(haClose, candle.OpenTime, true).ToDecimal();

			if (!_bottomSmooth.IsFormed || !_topSmooth.IsFormed || !_openSmooth.IsFormed || !_closeSmooth.IsFormed)
			return null;

			_previousHaOpen = haOpen;
			_previousHaClose = haClose;

			return new HeikenResult
			{
				Lower = bottom,
				Upper = top,
				Open = openSmoothed,
				Close = closeSmoothed,
				IsBullish = isBullish,
				IsBearish = !isBullish
			};
		}

		private static IIndicator CreateMovingAverage(HeikenMaMethod method, int length)
		{
			return method switch
			{
				HeikenMaMethod.Simple => new SimpleMovingAverage { Length = length },
				HeikenMaMethod.Exponential => new ExponentialMovingAverage { Length = length },
				HeikenMaMethod.Smoothed => new SmoothedMovingAverage { Length = length },
				HeikenMaMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
				_ => new SimpleMovingAverage { Length = length }
			};
		}
	}

	private sealed class AmaSignalCalculator
	{
		private readonly int _length;
		private readonly decimal _fastSc;
		private readonly decimal _slowSc;
		private readonly decimal _power;
		private readonly Queue<decimal> _closes = new();

		private decimal? _previousAma;

		public AmaSignalCalculator(int length, int fastPeriod, int slowPeriod, decimal power)
		{
			_length = length;
			_fastSc = 2m / (fastPeriod + 1m);
			_slowSc = 2m / (slowPeriod + 1m);
			_power = power;
		}

		public AmaResult Process(decimal close, DateTimeOffset time, bool isFinal)
		{
			_closes.Enqueue(close);
			if (_closes.Count > _length + 1)
			_closes.Dequeue();

			if (_closes.Count <= _length)
			return null;

			decimal? prevClose = null;
			decimal noise = 0m;

			foreach (var price in _closes)
			{
				if (prevClose is not null)
				{
					var diff = Math.Abs(price - prevClose.Value);
					noise += diff;
				}

				prevClose = price;
			}

			if (noise == 0m)
			noise = 1m;

			var first = _closes.Peek();
			var signal = Math.Abs(close - first);
			var er = signal / noise;
			var sc = _slowSc + er * (_fastSc - _slowSc);
			var smoothing = (decimal)Math.Pow((double)sc, (double)_power);

			var ama = _previousAma ?? first;
			ama += smoothing * (close - ama);

			var diffValue = _previousAma.HasValue ? ama - _previousAma.Value : 0m;
			_previousAma = ama;

			return new AmaResult
			{
				Value = ama,
				Difference = diffValue
			};
		}
	}

	public sealed class HeikenResult
	{
		public decimal Lower { get; set; }
		public decimal Upper { get; set; }
		public decimal Open { get; set; }
		public decimal Close { get; set; }
		public bool IsBullish { get; set; }
		public bool IsBearish { get; set; }
	}

	public sealed class AmaResult
	{
		public decimal Value { get; set; }
		public decimal Difference { get; set; }
	}
}

