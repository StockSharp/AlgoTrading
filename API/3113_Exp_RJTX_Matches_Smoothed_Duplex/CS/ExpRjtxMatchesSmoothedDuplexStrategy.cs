using System;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// StockSharp port of the Exp_RJTX_Matches_Smoothed_Duplex.mq5 expert advisor.
/// Two independent RJTX blocks read bullish / bearish "matches" from smoothed open/close series.
/// </summary>
public class ExpRjtxMatchesSmoothedDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<decimal> _longVolume;
	private readonly StrategyParam<bool> _longAllowOpen;
	private readonly StrategyParam<bool> _longAllowClose;
	private readonly StrategyParam<int> _longStopLossPoints;
	private readonly StrategyParam<int> _longTakeProfitPoints;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<SmoothingMethod> _longMethod;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _longPhase;

	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<decimal> _shortVolume;
	private readonly StrategyParam<bool> _shortAllowOpen;
	private readonly StrategyParam<bool> _shortAllowClose;
	private readonly StrategyParam<int> _shortStopLossPoints;
	private readonly StrategyParam<int> _shortTakeProfitPoints;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<SmoothingMethod> _shortMethod;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _shortPhase;

	private IIndicator _longOpenSmoother = null!;
	private IIndicator _longCloseSmoother = null!;
	private IIndicator _shortOpenSmoother = null!;
	private IIndicator _shortCloseSmoother = null!;

	private readonly List<decimal> _longOpenHistory = new();
	private readonly List<RjtxSignal> _longSignalHistory = new();
	private readonly List<decimal> _shortOpenHistory = new();
	private readonly List<RjtxSignal> _shortSignalHistory = new();

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpRjtxMatchesSmoothedDuplexStrategy"/> class.
	/// </summary>
	public ExpRjtxMatchesSmoothedDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Long Candle Type", "Time-frame used by the long RJTX block", "Long Block");

		_longVolume = Param(nameof(LongVolume), 0.1m)
		.SetDisplay("Long Volume", "Market volume opened by the long block", "Long Block");

		_longAllowOpen = Param(nameof(LongAllowOpen), true)
		.SetDisplay("Allow Long Entries", "Enable opening long positions", "Long Block");

		_longAllowClose = Param(nameof(LongAllowClose), true)
		.SetDisplay("Allow Long Closes", "Enable closing long positions", "Long Block");

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000)
		.SetRange(0, 100000)
		.SetDisplay("Long Stop Loss", "Protective stop for long trades expressed in price steps", "Long Block");

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 2000)
		.SetRange(0, 100000)
		.SetDisplay("Long Take Profit", "Profit target for long trades expressed in price steps", "Long Block");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
		.SetRange(0, 20)
		.SetDisplay("Long Signal Bar", "Shift applied when reading the RJTX buffers", "Long Block");

		_longPeriod = Param(nameof(LongPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Long Period", "Lookback used when comparing smoothed open prices", "Long Block");

		_longMethod = Param(nameof(LongMethod), SmoothingMethod.Sma)
		.SetDisplay("Long Smooth Method", "Smoothing algorithm applied to open/close prices", "Long Block");

		_longLength = Param(nameof(LongLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Long Smooth Length", "Length of the smoothing filter", "Long Block");

		_longPhase = Param(nameof(LongPhase), 15)
		.SetRange(-100, 100)
		.SetDisplay("Long Phase", "Phase parameter used by Jurik-style smoothing", "Long Block");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Short Candle Type", "Time-frame used by the short RJTX block", "Short Block");

		_shortVolume = Param(nameof(ShortVolume), 0.1m)
		.SetDisplay("Short Volume", "Market volume opened by the short block", "Short Block");

		_shortAllowOpen = Param(nameof(ShortAllowOpen), true)
		.SetDisplay("Allow Short Entries", "Enable opening short positions", "Short Block");

		_shortAllowClose = Param(nameof(ShortAllowClose), true)
		.SetDisplay("Allow Short Closes", "Enable closing short positions", "Short Block");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000)
		.SetRange(0, 100000)
		.SetDisplay("Short Stop Loss", "Protective stop for short trades expressed in price steps", "Short Block");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 2000)
		.SetRange(0, 100000)
		.SetDisplay("Short Take Profit", "Profit target for short trades expressed in price steps", "Short Block");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
		.SetRange(0, 20)
		.SetDisplay("Short Signal Bar", "Shift applied when reading the RJTX buffers", "Short Block");

		_shortPeriod = Param(nameof(ShortPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Short Period", "Lookback used when comparing smoothed open prices", "Short Block");

		_shortMethod = Param(nameof(ShortMethod), SmoothingMethod.Sma)
		.SetDisplay("Short Smooth Method", "Smoothing algorithm applied to open/close prices", "Short Block");

		_shortLength = Param(nameof(ShortLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Short Smooth Length", "Length of the smoothing filter", "Short Block");

		_shortPhase = Param(nameof(ShortPhase), 15)
		.SetRange(-100, 100)
		.SetDisplay("Short Phase", "Phase parameter used by Jurik-style smoothing", "Short Block");
	}

	/// <summary>
	/// Candle type that feeds the long RJTX block.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Volume used when opening long positions.
	/// </summary>
	public decimal LongVolume
	{
		get => _longVolume.Value;
		set => _longVolume.Value = value;
	}

	/// <summary>
	/// Enables opening new long positions.
	/// </summary>
	public bool LongAllowOpen
	{
		get => _longAllowOpen.Value;
		set => _longAllowOpen.Value = value;
	}

	/// <summary>
	/// Enables closing long positions on bearish matches.
	/// </summary>
	public bool LongAllowClose
	{
		get => _longAllowClose.Value;
		set => _longAllowClose.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades (price steps).
	/// </summary>
	public int LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long trades (price steps).
	/// </summary>
	public int LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Number of closed candles to skip before acting on long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Lookback depth for the long smoothed-open comparison.
	/// </summary>
	public int LongPeriod
	{
		get => _longPeriod.Value;
		set => _longPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing algorithm used by the long block.
	/// </summary>
	public SmoothingMethod LongMethod
	{
		get => _longMethod.Value;
		set => _longMethod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing filter used by the long block.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Phase parameter used by Jurik-style smoothing inside the long block.
	/// </summary>
	public int LongPhase
	{
		get => _longPhase.Value;
		set => _longPhase.Value = value;
	}

	/// <summary>
	/// Candle type that feeds the short RJTX block.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Volume used when opening short positions.
	/// </summary>
	public decimal ShortVolume
	{
		get => _shortVolume.Value;
		set => _shortVolume.Value = value;
	}

	/// <summary>
	/// Enables opening new short positions.
	/// </summary>
	public bool ShortAllowOpen
	{
		get => _shortAllowOpen.Value;
		set => _shortAllowOpen.Value = value;
	}

	/// <summary>
	/// Enables closing short positions on bullish matches.
	/// </summary>
	public bool ShortAllowClose
	{
		get => _shortAllowClose.Value;
		set => _shortAllowClose.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades (price steps).
	/// </summary>
	public int ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades (price steps).
	/// </summary>
	public int ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Number of closed candles to skip before acting on short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Lookback depth for the short smoothed-open comparison.
	/// </summary>
	public int ShortPeriod
	{
		get => _shortPeriod.Value;
		set => _shortPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing algorithm used by the short block.
	/// </summary>
	public SmoothingMethod ShortMethod
	{
		get => _shortMethod.Value;
		set => _shortMethod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing filter used by the short block.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Phase parameter used by Jurik-style smoothing inside the short block.
	/// </summary>
	public int ShortPhase
	{
		get => _shortPhase.Value;
		set => _shortPhase.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, LongCandleType);

		if (LongCandleType != ShortCandleType)
		yield return (Security, ShortCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longOpenHistory.Clear();
		_longSignalHistory.Clear();
		_shortOpenHistory.Clear();
		_shortSignalHistory.Clear();
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longOpenSmoother = CreateSmoother(LongMethod, LongLength, LongPhase);
		_longCloseSmoother = CreateSmoother(LongMethod, LongLength, LongPhase);
		_shortOpenSmoother = CreateSmoother(ShortMethod, ShortLength, ShortPhase);
		_shortCloseSmoother = CreateSmoother(ShortMethod, ShortLength, ShortPhase);

		var longSubscription = SubscribeCandles(LongCandleType);

		if (LongCandleType == ShortCandleType)
		{
			longSubscription.WhenCandlesFinished(ProcessCombinedCandle).Start();
		}
		else
		{
			longSubscription.WhenCandlesFinished(ProcessLongCandle).Start();

			SubscribeCandles(ShortCandleType)
			.WhenCandlesFinished(ProcessShortCandle)
			.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, longSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCombinedCandle(ICandleMessage candle)
	{
		ProcessLongCandle(candle);
		ProcessShortCandle(candle);
	}

	private void ProcessLongCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var time = candle.CloseTime;

		var openValue = _longOpenSmoother.Process(candle.OpenPrice, time, true);
		var closeValue = _longCloseSmoother.Process(candle.ClosePrice, time, true);

		if (!openValue.IsFinal || !closeValue.IsFinal)
		return;

		var smoothedOpen = openValue.ToDecimal();
		var smoothedClose = closeValue.ToDecimal();

		UpdateHistory(_longOpenHistory, smoothedOpen, Math.Max(2, LongPeriod + LongSignalBar + 3));

		if (_longOpenHistory.Count <= LongPeriod)
		{
			UpdateHistory(_longSignalHistory, RjtxSignal.None, Math.Max(2, LongSignalBar + 2));
			return;
		}

		var referenceOpen = _longOpenHistory[LongPeriod];
		var signal = smoothedClose > referenceOpen ? RjtxSignal.Bullish : RjtxSignal.Bearish;

		UpdateHistory(_longSignalHistory, signal, Math.Max(2, LongSignalBar + 2));

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_longSignalHistory.Count <= LongSignalBar)
		return;

		var executedSignal = _longSignalHistory[LongSignalBar];

		if (executedSignal == RjtxSignal.Bullish)
		TryOpenLong(candle.ClosePrice);
		else if (executedSignal == RjtxSignal.Bearish && LongAllowClose)
		CloseLong();

		UpdateRiskManagement(candle);
	}

	private void ProcessShortCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var time = candle.CloseTime;

		var openValue = _shortOpenSmoother.Process(candle.OpenPrice, time, true);
		var closeValue = _shortCloseSmoother.Process(candle.ClosePrice, time, true);

		if (!openValue.IsFinal || !closeValue.IsFinal)
		return;

		var smoothedOpen = openValue.ToDecimal();
		var smoothedClose = closeValue.ToDecimal();

		UpdateHistory(_shortOpenHistory, smoothedOpen, Math.Max(2, ShortPeriod + ShortSignalBar + 3));

		if (_shortOpenHistory.Count <= ShortPeriod)
		{
			UpdateHistory(_shortSignalHistory, RjtxSignal.None, Math.Max(2, ShortSignalBar + 2));
			return;
		}

		var referenceOpen = _shortOpenHistory[ShortPeriod];
		var signal = smoothedClose > referenceOpen ? RjtxSignal.Bullish : RjtxSignal.Bearish;

		UpdateHistory(_shortSignalHistory, signal, Math.Max(2, ShortSignalBar + 2));

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_shortSignalHistory.Count <= ShortSignalBar)
		return;

		var executedSignal = _shortSignalHistory[ShortSignalBar];

		if (executedSignal == RjtxSignal.Bearish)
		TryOpenShort(candle.ClosePrice);
		else if (executedSignal == RjtxSignal.Bullish && ShortAllowClose)
		CloseShort();

		UpdateRiskManagement(candle);
	}

	private void TryOpenLong(decimal entryPrice)
	{
		if (!LongAllowOpen || LongVolume <= 0m)
		return;

		if (Position < 0m)
		{
			if (!ShortAllowClose)
			return;

			var coverVolume = Math.Abs(Position);
			if (coverVolume > 0m)
			{
				BuyMarket(coverVolume);
				_shortEntryPrice = null;
			}
		}

		if (Position <= 0m)
		{
			BuyMarket(LongVolume);
			_longEntryPrice = entryPrice;
		}
	}

	private void TryOpenShort(decimal entryPrice)
	{
		if (!ShortAllowOpen || ShortVolume <= 0m)
		return;

		if (Position > 0m)
		{
			if (!LongAllowClose)
			return;

			var coverVolume = Position;
			if (coverVolume > 0m)
			{
				SellMarket(coverVolume);
				_longEntryPrice = null;
			}
		}

		if (Position >= 0m)
		{
			SellMarket(ShortVolume);
			_shortEntryPrice = entryPrice;
		}
	}

	private void CloseLong()
	{
		if (Position <= 0m)
		return;

		SellMarket(Position);
		_longEntryPrice = null;
	}

	private void CloseShort()
	{
		if (Position >= 0m)
		return;

		BuyMarket(Math.Abs(Position));
		_shortEntryPrice = null;
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		if (Position > 0m && _longEntryPrice.HasValue)
		{
			var stop = LongStopLossPoints > 0 ? _longEntryPrice.Value - LongStopLossPoints * step : (decimal?)null;
			var take = LongTakeProfitPoints > 0 ? _longEntryPrice.Value + LongTakeProfitPoints * step : (decimal?)null;

			if (stop.HasValue && candle.LowPrice <= stop.Value)
			{
				CloseLong();
				return;
			}

			if (take.HasValue && candle.HighPrice >= take.Value)
			CloseLong();
		}
		else if (Position < 0m && _shortEntryPrice.HasValue)
		{
			var stop = ShortStopLossPoints > 0 ? _shortEntryPrice.Value + ShortStopLossPoints * step : (decimal?)null;
			var take = ShortTakeProfitPoints > 0 ? _shortEntryPrice.Value - ShortTakeProfitPoints * step : (decimal?)null;

			if (stop.HasValue && candle.HighPrice >= stop.Value)
			{
				CloseShort();
				return;
			}

			if (take.HasValue && candle.LowPrice <= take.Value)
			CloseShort();
		}
	}

	private static void UpdateHistory<T>(List<T> history, T value, int maxSize)
	{
		history.Insert(0, value);

		if (history.Count > maxSize)
		history.RemoveAt(history.Count - 1);
	}

	private static IIndicator CreateSmoother(SmoothingMethod method, int length, int phase)
	{
		var normalizedLength = Math.Max(1, length);
		var offset = 0.5m + phase / 200m;
		offset = Math.Max(0m, Math.Min(1m, offset));

		return method switch
		{
			SmoothingMethod.Sma => new SimpleMovingAverage { Length = normalizedLength },
			SmoothingMethod.Ema => new ExponentialMovingAverage { Length = normalizedLength },
			SmoothingMethod.Smma => new SmoothedMovingAverage { Length = normalizedLength },
			SmoothingMethod.Lwma => new WeightedMovingAverage { Length = normalizedLength },
			SmoothingMethod.Jjma => CreateJurik(normalizedLength, phase),
			SmoothingMethod.Jurx => new ZeroLagExponentialMovingAverage { Length = normalizedLength },
			SmoothingMethod.Parma => new ArnaudLegouxMovingAverage { Length = normalizedLength, Offset = offset, Sigma = 6m },
			SmoothingMethod.T3 => new TripleExponentialMovingAverage { Length = normalizedLength },
			SmoothingMethod.Vidya => new ExponentialMovingAverage { Length = normalizedLength },
			SmoothingMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = normalizedLength },
			_ => new SimpleMovingAverage { Length = normalizedLength },
		};
	}

	private static IIndicator CreateJurik(int length, int phase)
	{
		var jurik = new JurikMovingAverage { Length = Math.Max(1, length) };
		var property = jurik.GetType().GetProperty("Phase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		if (property != null && property.CanWrite)
		{
			var clamped = Math.Max(-100, Math.Min(100, phase));
			property.SetValue(jurik, clamped);
		}

		return jurik;
	}

	private enum RjtxSignal
	{
		None,
		Bullish,
		Bearish,
	}

	/// <summary>
	/// Supported smoothing algorithms mirroring the SmoothAlgorithms library.
	/// </summary>
	public enum SmoothingMethod
	{
		/// <summary>Simple moving average.</summary>
		Sma,
		/// <summary>Exponential moving average.</summary>
		Ema,
		/// <summary>Smoothed moving average (SMMA).</summary>
		Smma,
		/// <summary>Linear weighted moving average.</summary>
		Lwma,
		/// <summary>Jurik moving average.</summary>
		Jjma,
		/// <summary>Zero-lag exponential moving average (JurX approximation).</summary>
		Jurx,
		/// <summary>Arnaud Legoux moving average approximating ParMA.</summary>
		Parma,
		/// <summary>Tillson T3 moving average.</summary>
		T3,
		/// <summary>VIDYA approximation using exponential smoothing.</summary>
		Vidya,
		/// <summary>Kaufman adaptive moving average.</summary>
		Ama,
	}
}
