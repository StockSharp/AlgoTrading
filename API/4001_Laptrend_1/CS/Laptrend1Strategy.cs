using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the Laptrend_1 MetaTrader expert advisor.
/// Combines LabTrend channel direction, Fisher transform momentum and ADX filter on multiple timeframes.
/// </summary>
public class Laptrend1Strategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _fisherLength;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<decimal> _risk;
	private readonly StrategyParam<decimal> _delta;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<DataType> _trendCandleType;

	private AverageDirectionalIndex _adx = null!;
	private FisherYur4ikIndicator _fisher = null!;
	private readonly LabTrendState _signalTrend = new();
	private readonly LabTrendState _trendTrend = new();
	private readonly Queue<decimal> _fisherHistory = new();

	private bool _fisherBullish;
	private bool _fisherBearish;
	private bool _fisherExitLong;
	private bool _fisherExitShort;

	private decimal? _previousAdx;
	private decimal _pointValue;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private int _lastPositionSign;


	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Fisher transform length.
	/// </summary>
	public int FisherLength
	{
		get => _fisherLength.Value;
		set => _fisherLength.Value = value;
	}

	/// <summary>
	/// LabTrend channel lookback.
	/// </summary>
	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	/// <summary>
	/// LabTrend risk factor (1..10 in the original code).
	/// </summary>
	public decimal RiskFactor
	{
		get => _risk.Value;
		set => _risk.Value = value;
	}

	/// <summary>
	/// Maximum distance between ADX and DI values before the market is considered flat.
	/// </summary>
	public decimal Delta
	{
		get => _delta.Value;
		set => _delta.Value = value;
	}

	/// <summary>
	/// Stop loss in points (MetaTrader style).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit in points (MetaTrader style).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop in points (MetaTrader style).
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations (default 15 minutes).
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type for the LabTrend filter (default 1 hour).
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="Laptrend1Strategy"/>.
	/// </summary>
	public Laptrend1Strategy()
	{

		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "Average Directional Index length", "Indicators");

		_fisherLength = Param(nameof(FisherLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Fisher Length", "Fisher transform window", "Indicators");

		_channelLength = Param(nameof(ChannelLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Channel Length", "LabTrend channel lookback", "Indicators");

		_risk = Param(nameof(RiskFactor), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Factor", "LabTrend risk factor", "Indicators");

		_delta = Param(nameof(Delta), 7m)
		.SetGreaterThanZero()
		.SetDisplay("ADX Delta", "Maximum spread between ADX and DI before flat exit", "Filters");

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 40m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take profit distance in points", "Risk Management");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 100m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk Management");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Signal Candle", "Primary timeframe for signals", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Trend Candle", "Higher timeframe for LabTrend filter", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, SignalCandleType);
		yield return (Security, TrendCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_adx = null!;
		_fisher = null!;
		_signalTrend.Reset();
		_trendTrend.Reset();
		_fisherHistory.Clear();
		_fisherBullish = false;
		_fisherBearish = false;
		_fisherExitLong = false;
		_fisherExitShort = false;
		_previousAdx = null;
		_pointValue = 0m;
		ResetLongState();
		ResetShortState();
		_lastPositionSign = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 0m;
		if (_pointValue <= 0m)
			_pointValue = 1m;

		_fisherHistory.Clear();
		_fisherBullish = false;
		_fisherBearish = false;
		_fisherExitLong = false;
		_fisherExitShort = false;
		_previousAdx = null;
		ResetLongState();
		ResetShortState();
		_lastPositionSign = Math.Sign(Position);

		_fisher = new FisherYur4ikIndicator
		{
			Length = FisherLength
		};

		_adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		var signalSubscription = SubscribeCandles(SignalCandleType);
		signalSubscription.Bind(_fisher, _adx, ProcessSignalCandle).Start();

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription.Bind(ProcessTrendCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, signalSubscription);
			DrawIndicator(area, _fisher);
			DrawOwnTrades(area);
		}
	}

	private void ProcessSignalCandle(ICandleMessage candle, IIndicatorValue fisherValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update LabTrend state on the signal timeframe.
		_signalTrend.Process(candle, ChannelLength, RiskFactor);

		// Keep Fisher state in sync whenever a final value is available.
		if (fisherValue.IsFinal && _fisher.IsFormed)
		{
			var fisher = fisherValue.ToDecimal();
			UpdateFisherFlags(fisher);
		}

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (!adxValue.IsFinal || !_adx.IsFormed || adxValue is not AverageDirectionalIndexValue adxData ||
			adxData.MovingAverage is not decimal adxCurrent ||
			adxData.Dx.Plus is not decimal plusDi ||
			adxData.Dx.Minus is not decimal minusDi)
		{
			ManagePosition(candle, canTrade);
			return;
		}

		var previousAdx = _previousAdx;
		_previousAdx = adxCurrent;

		var adxRising = previousAdx.HasValue && adxCurrent > previousAdx.Value;
		var bullDirectional = plusDi > minusDi;
		var bearDirectional = minusDi > plusDi;

		var flat = Math.Abs(plusDi - minusDi) < Delta &&
			Math.Abs(adxCurrent - plusDi) < Delta &&
			Math.Abs(adxCurrent - minusDi) < Delta;

		if (flat && canTrade)
		{
			// Reset momentum flags and close any open trades in ranging conditions.
			_fisherBullish = false;
			_fisherBearish = false;
			_fisherExitLong = false;
			_fisherExitShort = false;

			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));

			ManagePosition(candle, canTrade);
			return;
		}

		if (canTrade)
		{
			if (Position <= 0 && _trendTrend.IsUpTrend && _signalTrend.IsUpTrend && _fisherBullish && bullDirectional && adxRising)
			{
				var volume = Volume + Math.Abs(Position);
				if (volume > 0m)
					BuyMarket(volume);
			}
			else if (Position >= 0 && _trendTrend.IsDownTrend && _signalTrend.IsDownTrend && _fisherBearish && bearDirectional && adxRising)
			{
				var volume = Volume + Math.Abs(Position);
				if (volume > 0m)
					SellMarket(volume);
			}

			if (Position > 0 && (_signalTrend.IsDownTrend || _fisherBearish || _fisherExitLong))
				SellMarket(Position);

			if (Position < 0 && (_signalTrend.IsUpTrend || _fisherBullish || _fisherExitShort))
				BuyMarket(Math.Abs(Position));
		}

		ManagePosition(candle, canTrade);
	}

	private void ProcessTrendCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track the higher timeframe trend for directional filtering.
		_trendTrend.Process(candle, ChannelLength, RiskFactor);
	}

	private void ManagePosition(ICandleMessage candle, bool canTrade)
	{
		var position = Position;
		var positionSign = Math.Sign(position);

		if (!canTrade)
		{
			_lastPositionSign = positionSign;
			return;
		}

		var step = _pointValue > 0m ? _pointValue : 1m;
		var stopOffset = StopLossPoints > 0m ? StopLossPoints * step : 0m;
		var takeOffset = TakeProfitPoints > 0m ? TakeProfitPoints * step : 0m;
		var trailingOffset = TrailingStopPoints > 0m ? TrailingStopPoints * step : 0m;

		if (positionSign > 0)
		{
			// Capture the entry price when switching from short or flat to long.
			if (_lastPositionSign <= 0)
			{
				_longEntryPrice = candle.ClosePrice;
				_longTrailingStop = trailingOffset > 0m ? candle.ClosePrice - trailingOffset : null;
				ResetShortState();
			}

			if (_longEntryPrice.HasValue)
			{
				var entry = _longEntryPrice.Value;
				var volume = position;

				if (stopOffset > 0m && candle.LowPrice <= entry - stopOffset)
				{
					SellMarket(volume);
					ResetLongState();
					_lastPositionSign = 0;
					return;
				}

				if (takeOffset > 0m && candle.HighPrice >= entry + takeOffset)
				{
					SellMarket(volume);
					ResetLongState();
					_lastPositionSign = 0;
					return;
				}

				if (trailingOffset > 0m)
				{
					var candidate = candle.ClosePrice - trailingOffset;
					if (!_longTrailingStop.HasValue || candidate > _longTrailingStop.Value)
						_longTrailingStop = candidate;

					if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
					{
						SellMarket(volume);
						ResetLongState();
						_lastPositionSign = 0;
						return;
					}
				}
			}
		}
		else if (positionSign < 0)
		{
			// Capture the entry price when switching from long or flat to short.
			if (_lastPositionSign >= 0)
			{
				_shortEntryPrice = candle.ClosePrice;
				_shortTrailingStop = trailingOffset > 0m ? candle.ClosePrice + trailingOffset : null;
				ResetLongState();
			}

			if (_shortEntryPrice.HasValue)
			{
				var entry = _shortEntryPrice.Value;
				var volume = Math.Abs(position);

				if (stopOffset > 0m && candle.HighPrice >= entry + stopOffset)
				{
					BuyMarket(volume);
					ResetShortState();
					_lastPositionSign = 0;
					return;
				}

				if (takeOffset > 0m && candle.LowPrice <= entry - takeOffset)
				{
					BuyMarket(volume);
					ResetShortState();
					_lastPositionSign = 0;
					return;
				}

				if (trailingOffset > 0m)
				{
					var candidate = candle.ClosePrice + trailingOffset;
					if (!_shortTrailingStop.HasValue || candidate < _shortTrailingStop.Value)
						_shortTrailingStop = candidate;

					if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
					{
						BuyMarket(volume);
						ResetShortState();
						_lastPositionSign = 0;
						return;
					}
				}
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}

		_lastPositionSign = positionSign;
	}

	private void UpdateFisherFlags(decimal value)
	{
		_fisherHistory.Enqueue(value);
		while (_fisherHistory.Count > 3)
			_fisherHistory.Dequeue();

		if (_fisherHistory.Count < 3)
			return;

		var values = _fisherHistory.ToArray();
		var fx0n = values[^1];
		var fx1n = values[^2];
		var fx2n = values[^3];

		var fx0 = (fx0n + fx1n) / 2m;
		var fx1 = (fx1n + fx2n) / 2m;

		if (fx1 < 0m && fx0 > 0m)
		{
			// Fisher crossed above zero -> bullish momentum.
			_fisherBullish = true;
			_fisherBearish = false;
		}
		else if (fx1 > 0m && fx0 < 0m)
		{
			// Fisher crossed below zero -> bearish momentum.
			_fisherBearish = true;
			_fisherBullish = false;
		}

		if (fx1 > 0.25m && fx0 < 0.25m)
		{
			// Fisher dropped back under +0.25 -> exit long.
			_fisherExitLong = true;
			_fisherExitShort = false;
		}
		else if (fx1 < -0.25m && fx0 > -0.25m)
		{
			// Fisher climbed back above -0.25 -> exit short.
			_fisherExitShort = true;
			_fisherExitLong = false;
		}
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longTrailingStop = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortTrailingStop = null;
	}

	private sealed class LabTrendState
	{
		private readonly Queue<decimal> _highs = new();
		private readonly Queue<decimal> _lows = new();
		private decimal _trend;

		public bool IsUpTrend => _trend > 0m;
		public bool IsDownTrend => _trend < 0m;

		public void Reset()
		{
			_highs.Clear();
			_lows.Clear();
			_trend = 0m;
		}

		public void Process(ICandleMessage candle, int length, decimal risk)
		{
			var lookback = Math.Max(1, length);

			_highs.Enqueue(candle.HighPrice);
			_lows.Enqueue(candle.LowPrice);

			if (_highs.Count > lookback)
			{
				_highs.Dequeue();
				_lows.Dequeue();
			}

			if (_highs.Count < lookback)
				return;

			var highest = _highs.Max();
			var lowest = _lows.Min();
			var range = highest - lowest;

			if (range <= 0m)
				return;

			var safeRisk = risk;
			if (safeRisk < 0m)
				safeRisk = 0m;
			else if (safeRisk > 33m)
				safeRisk = 33m;

			var coefficient = (33m - safeRisk) / 100m;
			var upper = highest - range * coefficient;
			var lower = lowest + range * coefficient;

			if (_trend <= 0m && candle.ClosePrice > upper)
				_trend = 1m;
			else if (_trend >= 0m && candle.ClosePrice < lower)
				_trend = -1m;
		}
	}

	private sealed class FisherYur4ikIndicator : Indicator<ICandleMessage>
	{
		public int Length { get; set; } = 10;

		private readonly Queue<decimal> _medians = new();
		private decimal _previousValue;
		private decimal _previousFish;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			if (candle == null)
				return new DecimalIndicatorValue(this, 0m, input.Time);

			var length = Math.Max(1, Length);
			var median = (candle.HighPrice + candle.LowPrice) / 2m;

			_medians.Enqueue(median);
			if (_medians.Count > length)
			{
				_medians.Dequeue();
			}

			if (_medians.Count < length)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			var highest = _medians.Max();
			var lowest = _medians.Min();
			var range = highest - lowest;

			decimal fish;
			if (range == 0m)
			{
				fish = _previousFish;
			}
			else
			{
				var value = 0.66m * ((median - lowest) / range - 0.5m) + 0.67m * _previousValue;
				if (value > 0.999m)
					value = 0.999m;
				else if (value < -0.999m)
					value = -0.999m;

				var ratio = (1m + value) / (1m - value);
				fish = 0.5m * (decimal)Math.Log((double)ratio) + 0.5m * _previousFish;

				_previousValue = value;
				_previousFish = fish;
			}

			IsFormed = _medians.Count >= length;

			return new DecimalIndicatorValue(this, fish, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_medians.Clear();
			_previousValue = 0m;
			_previousFish = 0m;
		}
	}
}
