using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy Exp_FineTuningMACandle_Duplex.
/// Evaluates two independent FineTuningMA candle streams to control long and short trades separately.
/// </summary>
public class FineTuningMaCandleDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<decimal> _longRank1;
	private readonly StrategyParam<decimal> _longRank2;
	private readonly StrategyParam<decimal> _longRank3;
	private readonly StrategyParam<decimal> _longShift1;
	private readonly StrategyParam<decimal> _longShift2;
	private readonly StrategyParam<decimal> _longShift3;
	private readonly StrategyParam<decimal> _longGap;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableLongExits;

	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<decimal> _shortRank1;
	private readonly StrategyParam<decimal> _shortRank2;
	private readonly StrategyParam<decimal> _shortRank3;
	private readonly StrategyParam<decimal> _shortShift1;
	private readonly StrategyParam<decimal> _shortShift2;
	private readonly StrategyParam<decimal> _shortShift3;
	private readonly StrategyParam<decimal> _shortGap;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableShortExits;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;

	private FineTuningMaCandleIndicator _longIndicator;
	private FineTuningMaCandleIndicator _shortIndicator;

	private readonly List<decimal> _longColorHistory = new();
	private readonly List<decimal> _shortColorHistory = new();

	/// <summary>
	/// Candle type for evaluating the long FineTuningMA stream.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// FineTuningMA length for the long stream.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Rank1 shaping parameter for the long indicator.
	/// </summary>
	public decimal LongRank1
	{
		get => _longRank1.Value;
		set => _longRank1.Value = value;
	}

	/// <summary>
	/// Rank2 shaping parameter for the long indicator.
	/// </summary>
	public decimal LongRank2
	{
		get => _longRank2.Value;
		set => _longRank2.Value = value;
	}

	/// <summary>
	/// Rank3 shaping parameter for the long indicator.
	/// </summary>
	public decimal LongRank3
	{
		get => _longRank3.Value;
		set => _longRank3.Value = value;
	}

	/// <summary>
	/// Shift1 coefficient for the long indicator weighting profile.
	/// </summary>
	public decimal LongShift1
	{
		get => _longShift1.Value;
		set => _longShift1.Value = value;
	}

	/// <summary>
	/// Shift2 coefficient for the long indicator weighting profile.
	/// </summary>
	public decimal LongShift2
	{
		get => _longShift2.Value;
		set => _longShift2.Value = value;
	}

	/// <summary>
	/// Shift3 coefficient for the long indicator weighting profile.
	/// </summary>
	public decimal LongShift3
	{
		get => _longShift3.Value;
		set => _longShift3.Value = value;
	}

	/// <summary>
	/// Maximum body gap (in price units) that keeps a bullish candle open value flat.
	/// </summary>
	public decimal LongGap
	{
		get => _longGap.Value;
		set => _longGap.Value = value;
	}

	/// <summary>
	/// Number of completed candles to look back before evaluating the long signal.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Enables opening long positions.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	/// <summary>
	/// Enables closing long positions.
	/// </summary>
	public bool EnableLongExits
	{
		get => _enableLongExits.Value;
		set => _enableLongExits.Value = value;
	}

	/// <summary>
	/// Candle type for evaluating the short FineTuningMA stream.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// FineTuningMA length for the short stream.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Rank1 shaping parameter for the short indicator.
	/// </summary>
	public decimal ShortRank1
	{
		get => _shortRank1.Value;
		set => _shortRank1.Value = value;
	}

	/// <summary>
	/// Rank2 shaping parameter for the short indicator.
	/// </summary>
	public decimal ShortRank2
	{
		get => _shortRank2.Value;
		set => _shortRank2.Value = value;
	}

	/// <summary>
	/// Rank3 shaping parameter for the short indicator.
	/// </summary>
	public decimal ShortRank3
	{
		get => _shortRank3.Value;
		set => _shortRank3.Value = value;
	}

	/// <summary>
	/// Shift1 coefficient for the short indicator weighting profile.
	/// </summary>
	public decimal ShortShift1
	{
		get => _shortShift1.Value;
		set => _shortShift1.Value = value;
	}

	/// <summary>
	/// Shift2 coefficient for the short indicator weighting profile.
	/// </summary>
	public decimal ShortShift2
	{
		get => _shortShift2.Value;
		set => _shortShift2.Value = value;
	}

	/// <summary>
	/// Shift3 coefficient for the short indicator weighting profile.
	/// </summary>
	public decimal ShortShift3
	{
		get => _shortShift3.Value;
		set => _shortShift3.Value = value;
	}

	/// <summary>
	/// Maximum body gap (in price units) that keeps a bearish candle open value flat.
	/// </summary>
	public decimal ShortGap
	{
		get => _shortGap.Value;
		set => _shortGap.Value = value;
	}

	/// <summary>
	/// Number of completed candles to look back before evaluating the short signal.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Enables opening short positions.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	/// <summary>
	/// Enables closing short positions.
	/// </summary>
	public bool EnableShortExits
	{
		get => _enableShortExits.Value;
		set => _enableShortExits.Value = value;
	}

	/// <summary>
	/// Base order size used when entering a new position.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points. Set to zero to disable.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points. Set to zero to disable.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="FineTuningMaCandleDuplexStrategy"/>.
	/// </summary>
	public FineTuningMaCandleDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Long Candle Type", "Candle type for the long FineTuningMA stream", "Long stream");

		_longLength = Param(nameof(LongLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Long Length", "FineTuningMA length for long signals", "Long stream")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_longRank1 = Param(nameof(LongRank1), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Long Rank1", "First shaping exponent for long weights", "Long stream");

		_longRank2 = Param(nameof(LongRank2), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Long Rank2", "Second shaping exponent for long weights", "Long stream");

		_longRank3 = Param(nameof(LongRank3), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Long Rank3", "Third shaping exponent for long weights", "Long stream");

		_longShift1 = Param(nameof(LongShift1), 1m)
		.SetNotNegative()
		.SetLessOrEquals(1m)
		.SetDisplay("Long Shift1", "First shift coefficient for long weights", "Long stream");

		_longShift2 = Param(nameof(LongShift2), 1m)
		.SetNotNegative()
		.SetLessOrEquals(1m)
		.SetDisplay("Long Shift2", "Second shift coefficient for long weights", "Long stream");

		_longShift3 = Param(nameof(LongShift3), 1m)
		.SetNotNegative()
		.SetLessOrEquals(1m)
		.SetDisplay("Long Shift3", "Third shift coefficient for long weights", "Long stream");

		_longGap = Param(nameof(LongGap), 10m)
		.SetNotNegative()
		.SetDisplay("Long Gap", "Maximum real candle body that keeps the synthetic open flat", "Long stream");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Long Signal Bar", "Lookback offset used for long entries", "Long stream");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions", "Long stream");

		_enableLongExits = Param(nameof(EnableLongExits), true)
		.SetDisplay("Enable Long Exits", "Allow closing long positions", "Long stream");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Short Candle Type", "Candle type for the short FineTuningMA stream", "Short stream");

		_shortLength = Param(nameof(ShortLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Short Length", "FineTuningMA length for short signals", "Short stream")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_shortRank1 = Param(nameof(ShortRank1), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Short Rank1", "First shaping exponent for short weights", "Short stream");

		_shortRank2 = Param(nameof(ShortRank2), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Short Rank2", "Second shaping exponent for short weights", "Short stream");

		_shortRank3 = Param(nameof(ShortRank3), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Short Rank3", "Third shaping exponent for short weights", "Short stream");

		_shortShift1 = Param(nameof(ShortShift1), 1m)
		.SetNotNegative()
		.SetLessOrEquals(1m)
		.SetDisplay("Short Shift1", "First shift coefficient for short weights", "Short stream");

		_shortShift2 = Param(nameof(ShortShift2), 1m)
		.SetNotNegative()
		.SetLessOrEquals(1m)
		.SetDisplay("Short Shift2", "Second shift coefficient for short weights", "Short stream");

		_shortShift3 = Param(nameof(ShortShift3), 1m)
		.SetNotNegative()
		.SetLessOrEquals(1m)
		.SetDisplay("Short Shift3", "Third shift coefficient for short weights", "Short stream");

		_shortGap = Param(nameof(ShortGap), 10m)
		.SetNotNegative()
		.SetDisplay("Short Gap", "Maximum real candle body that keeps the synthetic open flat", "Short stream");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Short Signal Bar", "Lookback offset used for short entries", "Short stream");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions", "Short stream");

		_enableShortExits = Param(nameof(EnableShortExits), true)
		.SetDisplay("Enable Short Exits", "Allow closing short positions", "Short stream");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Base volume for opening positions", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take-profit distance in price points", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop-loss distance in price points", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, LongCandleType);

		if (!Equals(LongCandleType, ShortCandleType))
		yield return (Security, ShortCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_longColorHistory.Clear();
		_shortColorHistory.Clear();
		_longIndicator?.Reset();
		_shortIndicator?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longIndicator = new FineTuningMaCandleIndicator
		{
			Length = LongLength,
			Rank1 = LongRank1,
			Rank2 = LongRank2,
			Rank3 = LongRank3,
			Shift1 = LongShift1,
			Shift2 = LongShift2,
			Shift3 = LongShift3,
			Gap = LongGap
		};

		_shortIndicator = new FineTuningMaCandleIndicator
		{
			Length = ShortLength,
			Rank1 = ShortRank1,
			Rank2 = ShortRank2,
			Rank3 = ShortRank3,
			Shift1 = ShortShift1,
			Shift2 = ShortShift2,
			Shift3 = ShortShift3,
			Gap = ShortGap
		};

		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription
		.Bind(_longIndicator, ProcessLongCandle)
		.Start();

		var shortSubscription = SubscribeCandles(ShortCandleType);
		shortSubscription
		.Bind(_shortIndicator, ProcessShortCandle)
		.Start();

		var step = Security?.Step ?? 0m;
		Unit takeProfit = null;
		Unit stopLoss = null;

		if (step > 0m)
		{
			if (TakeProfitPoints > 0m)
			takeProfit = new Unit(TakeProfitPoints * step, UnitTypes.Point);

			if (StopLossPoints > 0m)
			stopLoss = new Unit(StopLossPoints * step, UnitTypes.Point);
		}

		if (takeProfit != null || stopLoss != null)
		StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);

		var longArea = CreateChartArea("FineTuningMA Long");
		if (longArea != null)
		{
			DrawCandles(longArea, longSubscription);
			DrawIndicator(longArea, _longIndicator);
			DrawOwnTrades(longArea);
		}

		if (!Equals(LongCandleType, ShortCandleType))
		{
			var shortArea = CreateChartArea("FineTuningMA Short");
			if (shortArea != null)
			{
				DrawCandles(shortArea, shortSubscription);
				DrawIndicator(shortArea, _shortIndicator);
				DrawOwnTrades(shortArea);
			}
		}
	}

	private void ProcessLongCandle(ICandleMessage candle, decimal color)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Track the latest long-side colors to reproduce the buffer indexing logic.
		_longColorHistory.Insert(0, color);
		var maxHistory = Math.Max(2, LongSignalBar + 2);
		if (_longColorHistory.Count > maxHistory)
		_longColorHistory.RemoveAt(_longColorHistory.Count - 1);

		if (_longIndicator?.IsFormed != true)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var offset = Math.Max(1, LongSignalBar + 1);
		if (_longColorHistory.Count <= offset)
		return;

		var currentColor = _longColorHistory[offset - 1];
		var previousColor = _longColorHistory[offset];

		var shouldClose = EnableLongExits && previousColor == 0m && Position > 0m;
		if (shouldClose)
		{
			SellMarket(Position);
			LogInfo($"Exit long because the FineTuningMA candle turned bearish at {candle.OpenTime:O}.");
		}

		var shouldOpen = EnableLongEntries && previousColor == 2m && currentColor != 2m && Position <= 0m;
		if (shouldOpen)
		{
			var volume = OrderVolume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Enter long after the FineTuningMA candle lost its bullish color at {candle.OpenTime:O}.");
		}
	}

	private void ProcessShortCandle(ICandleMessage candle, decimal color)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Track the latest short-side colors to reproduce the buffer indexing logic.
		_shortColorHistory.Insert(0, color);
		var maxHistory = Math.Max(2, ShortSignalBar + 2);
		if (_shortColorHistory.Count > maxHistory)
		_shortColorHistory.RemoveAt(_shortColorHistory.Count - 1);

		if (_shortIndicator?.IsFormed != true)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var offset = Math.Max(1, ShortSignalBar + 1);
		if (_shortColorHistory.Count <= offset)
		return;

		var currentColor = _shortColorHistory[offset - 1];
		var previousColor = _shortColorHistory[offset];

		var shouldClose = EnableShortExits && previousColor == 2m && Position < 0m;
		if (shouldClose)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short because the FineTuningMA candle turned bullish at {candle.OpenTime:O}.");
		}

		var shouldOpen = EnableShortEntries && previousColor == 0m && currentColor != 0m && Position >= 0m;
		if (shouldOpen)
		{
			var volume = OrderVolume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Enter short after the FineTuningMA candle lost its bearish color at {candle.OpenTime:O}.");
		}
	}

	private sealed class FineTuningMaCandleIndicator : Indicator<ICandleMessage>
	{
		public int Length { get; set; } = 10;
		public decimal Rank1 { get; set; } = 2m;
		public decimal Rank2 { get; set; } = 2m;
		public decimal Rank3 { get; set; } = 2m;
		public decimal Shift1 { get; set; } = 1m;
		public decimal Shift2 { get; set; } = 1m;
		public decimal Shift3 { get; set; } = 1m;
		public decimal Gap { get; set; } = 10m;

		private readonly List<ICandleMessage> _history = new();
		private double[] _weights = Array.Empty<double>();
		private decimal _lastColor = 1m;
		private decimal? _previousSyntheticClose;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			if (!input.IsFinal)
			return new DecimalIndicatorValue(this, _lastColor, input.Time);

			if (_weights.Length != Length)
			RecalculateWeights();

			_history.Add(candle);
			if (_history.Count > Length)
			_history.RemoveAt(0);

			if (_history.Count < Length)
			{
				IsFormed = false;
				_lastColor = 1m;
				_previousSyntheticClose = candle.ClosePrice;
				return new DecimalIndicatorValue(this, _lastColor, input.Time);
			}

			decimal weightedOpen = 0m;
			decimal weightedClose = 0m;

			for (var i = 0; i < Length; i++)
			{
				var histIndex = _history.Count - 1 - i;
				var source = _history[histIndex];
				var weight = (decimal)_weights[i];

				weightedOpen += weight * source.OpenPrice;
				weightedClose += weight * source.ClosePrice;
			}

			var syntheticOpen = weightedOpen;
			var syntheticClose = weightedClose;

			if (Math.Abs(candle.OpenPrice - candle.ClosePrice) <= Gap)
			syntheticOpen = _previousSyntheticClose ?? syntheticClose;

			var colorValue = syntheticOpen < syntheticClose ? 2m : syntheticOpen > syntheticClose ? 0m : 1m;

			IsFormed = true;
			_lastColor = colorValue;
			_previousSyntheticClose = syntheticClose;

			return new DecimalIndicatorValue(this, colorValue, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_history.Clear();
			_lastColor = 1m;
			_previousSyntheticClose = null;
			RecalculateWeights();
			IsFormed = false;
		}

		private void RecalculateWeights()
		{
			if (Length <= 0)
			{
				_weights = Array.Empty<double>();
				return;
			}

			var weights = new double[Length];
			var denominator = Math.Max(1, Length - 1);
			var rank1 = (double)Rank1;
			var rank2 = (double)Rank2;
			var rank3 = (double)Rank3;
			var shift1 = (double)Shift1;
			var shift2 = (double)Shift2;
			var shift3 = (double)Shift3;
			double sum = 0d;

			for (var h = 0; h < Length; h++)
			{
				var ratio = denominator == 0 ? 0d : h / (double)denominator;
				var weight = shift1 + Math.Pow(ratio, rank1) * (1d - shift1);
				weight *= shift2 + Math.Pow(1d - ratio, rank2) * (1d - shift2);

				double thirdComponent;
				if (ratio < 0.5d)
				thirdComponent = shift3 + Math.Pow(1d - ratio * 2d, rank3) * (1d - shift3);
				else
				thirdComponent = shift3 + Math.Pow(ratio * 2d - 1d, rank3) * (1d - shift3);

				weight *= thirdComponent;
				weights[h] = weight;
				sum += weight;
			}

			if (sum <= double.Epsilon)
			{
				var uniform = 1d / Math.Max(1, Length);
				for (var i = 0; i < Length; i++)
				weights[i] = uniform;
			}
			else
			{
				for (var i = 0; i < Length; i++)
				weights[i] /= sum;
			}

			_weights = weights;
		}
	}
}
