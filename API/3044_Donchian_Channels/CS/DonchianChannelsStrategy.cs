using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian Channel breakout strategy with multi-timeframe confirmation and trailing protection.
/// </summary>
public class DonchianChannelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _donchianCandleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<int> _donchianLength;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<decimal> _maDistance;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingTrigger;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<decimal> _trailingPadding;
	private readonly StrategyParam<bool> _useCandleTrail;
	private readonly StrategyParam<int> _candleTrailLength;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTrigger;
	private readonly StrategyParam<decimal> _breakEvenOffset;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _equityRisk;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _tradeVolume;

	private decimal? _donchianUpper;
	private decimal? _donchianLower;
	private decimal? _donchianMiddle;

	private decimal? _prevHigherOpen;
	private decimal? _prevHigherLow;
	private decimal? _prevHigherHigh;

	private decimal? _currentHigherOpen;
	private decimal? _currentHigherLow;
	private decimal? _currentHigherHigh;

	private decimal? _momentumDiff0;
	private decimal? _momentumDiff1;
	private decimal? _momentumDiff2;

	private decimal _prevFastMa;
	private decimal _prevSlowMa;

	private decimal? _prevBaseHigh1;
	private decimal? _prevBaseHigh2;
	private decimal? _prevBaseLow1;
	private decimal? _prevBaseLow2;

	private decimal _entryPrice;
	private decimal? _trailingStop;

	private decimal[] _recentLows = Array.Empty<decimal>();
	private decimal[] _recentHighs = Array.Empty<decimal>();
	private int _recentCount;
	private int _recentIndex;

	private MovingAverageConvergenceDivergence _macd = null!;
	private decimal? _macdMain;
	private decimal? _macdSignal;

	private decimal _equityPeak;

	/// <summary>
	/// Base candle type for execution logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for Donchian Channel and momentum filter.
	/// </summary>
	public DataType DonchianCandleType
	{
		get => _donchianCandleType.Value;
		set => _donchianCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe for MACD trend confirmation.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Donchian Channel length.
	/// </summary>
	public int DonchianLength
	{
		get => _donchianLength.Value;
		set => _donchianLength.Value = value;
	}

	/// <summary>
	/// Fast LWMA length on the trading timeframe.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow LWMA length on the trading timeframe.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Maximum distance between fast and slow MA measured in price steps.
	/// </summary>
	public decimal MaDistance
	{
		get => _maDistance.Value;
		set => _maDistance.Value = value;
	}

	/// <summary>
	/// Momentum period on the higher timeframe.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute deviation from 100 for bullish momentum.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum absolute deviation from 100 for bearish momentum.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Enable trailing stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Distance that must be travelled before the trailing stop activates.
	/// </summary>
	public decimal TrailingTrigger
	{
		get => _trailingTrigger.Value;
		set => _trailingTrigger.Value = value;
	}

	/// <summary>
	/// Step used when updating the trailing stop.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Additional padding applied to candle-based trailing stops.
	/// </summary>
	public decimal TrailingPadding
	{
		get => _trailingPadding.Value;
		set => _trailingPadding.Value = value;
	}

	/// <summary>
	/// Use recent candle extremes for trailing stop calculation.
	/// </summary>
	public bool UseCandleTrail
	{
		get => _useCandleTrail.Value;
		set => _useCandleTrail.Value = value;
	}

	/// <summary>
	/// Number of candles considered by the candle-based trailing stop.
	/// </summary>
	public int CandleTrailLength
	{
		get => _candleTrailLength.Value;
		set => _candleTrailLength.Value = value;
	}

	/// <summary>
	/// Enable move-to-break-even logic.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Distance required before moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	/// <summary>
	/// Offset applied when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenOffset
	{
		get => _breakEvenOffset.Value;
		set => _breakEvenOffset.Value = value;
	}

	/// <summary>
	/// Enables drawdown-based emergency exit.
	/// </summary>
	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	/// <summary>
	/// Maximum tolerated drawdown in strategy P&L units.
	/// </summary>
	public decimal TotalEquityRisk
	{
		get => _equityRisk.Value;
		set => _equityRisk.Value = value;
	}

	/// <summary>
	/// Maximum number of pyramid entries allowed.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Volume used for each order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DonchianChannelsStrategy"/>.
	/// </summary>
	public DonchianChannelsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Base Candle", "Primary timeframe", "General");

		_donchianCandleType = Param(nameof(DonchianCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Donchian Candle", "Higher timeframe for Donchian channel", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Trend Candle", "Timeframe for MACD trend filter", "General");

		_donchianLength = Param(nameof(DonchianLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Donchian Length", "Channel lookback length", "Indicators");

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Length of the fast LWMA", "Indicators");

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Length of the slow LWMA", "Indicators");

		_maDistance = Param(nameof(MaDistance), 5m)
		.SetNotNegative()
		.SetDisplay("MA Distance", "Allowed deviation between fast and slow MA", "Filters");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Momentum lookback on higher timeframe", "Indicators");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetNotNegative()
		.SetDisplay("Momentum Buy", "Required momentum deviation for longs", "Filters");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetNotNegative()
		.SetDisplay("Momentum Sell", "Required momentum deviation for shorts", "Filters");

		_stopLoss = Param(nameof(StopLoss), 20m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop loss distance in steps", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 50m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take profit distance in steps", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailingTrigger = Param(nameof(TrailingTrigger), 40m)
		.SetNotNegative()
		.SetDisplay("Trailing Trigger", "Activation distance for trailing", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 40m)
		.SetNotNegative()
		.SetDisplay("Trailing Step", "Step used when updating trailing stop", "Risk");

		_trailingPadding = Param(nameof(TrailingPadding), 10m)
		.SetNotNegative()
		.SetDisplay("Trailing Padding", "Padding for candle-based trailing", "Risk");

		_useCandleTrail = Param(nameof(UseCandleTrail), true)
		.SetDisplay("Candle Trail", "Use candle extremes for trailing", "Risk");

		_candleTrailLength = Param(nameof(CandleTrailLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Trail Candles", "Number of candles for trailing", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use BreakEven", "Enable break-even logic", "Risk");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 30m)
		.SetNotNegative()
		.SetDisplay("BreakEven Trigger", "Distance before break-even activates", "Risk");

		_breakEvenOffset = Param(nameof(BreakEvenOffset), 30m)
		.SetNotNegative()
		.SetDisplay("BreakEven Offset", "Offset applied when moving to break-even", "Risk");

		_useEquityStop = Param(nameof(UseEquityStop), true)
		.SetDisplay("Use Equity Stop", "Enable drawdown protection", "Risk");

		_equityRisk = Param(nameof(TotalEquityRisk), 1m)
		.SetNotNegative()
		.SetDisplay("Equity Risk", "Maximum tolerated drawdown", "Risk");

		_maxTrades = Param(nameof(MaxTrades), 10)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades", "Maximum number of open trades", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (DonchianCandleType != CandleType)
		yield return (Security, DonchianCandleType);

		if (TrendCandleType != CandleType && TrendCandleType != DonchianCandleType)
		yield return (Security, TrendCandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_recentLows = new decimal[Math.Max(1, CandleTrailLength)];
		_recentHighs = new decimal[Math.Max(1, CandleTrailLength)];
		_recentCount = 0;
		_recentIndex = 0;
		_trailingStop = null;
		_equityPeak = 0m;

		var fastMa = new WeightedMovingAverage { Length = FastMaLength };
		var slowMa = new WeightedMovingAverage { Length = SlowMaLength };

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription.Bind(fastMa, slowMa, ProcessBaseCandle).Start();

		var donchian = new DonchianChannels { Length = DonchianLength };
		var momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			FastLength = 12,
			SlowLength = 26,
			SignalLength = 9
		};

		var higherSubscription = SubscribeCandles(DonchianCandleType);

		if (TrendCandleType == DonchianCandleType)
		{
			higherSubscription.BindEx(donchian, momentum, _macd, ProcessHigherCandle).Start();
		}
		else
		{
			higherSubscription.BindEx(donchian, momentum, ProcessHigherCandle).Start();

			var trendSubscription = SubscribeCandles(TrendCandleType);
			trendSubscription.BindEx(_macd, ProcessTrendCandle).Start();
		}
	}

	private void ProcessTrendCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateMacd(macdValue);
	}

	private void ProcessHigherCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue momentumValue)
	{
		ProcessHigherCandleInternal(candle, donchianValue, momentumValue, null);
	}

	private void ProcessHigherCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue momentumValue, IIndicatorValue macdValue)
	{
		ProcessHigherCandleInternal(candle, donchianValue, momentumValue, macdValue);
	}

	private void ProcessHigherCandleInternal(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue momentumValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_prevHigherOpen = _currentHigherOpen;
		_prevHigherLow = _currentHigherLow;
		_prevHigherHigh = _currentHigherHigh;

		_currentHigherOpen = candle.OpenPrice;
		_currentHigherLow = candle.LowPrice;
		_currentHigherHigh = candle.HighPrice;

		if (donchianValue is DonchianChannelsValue channelValue)
		{
			if (channelValue.UpBand is decimal upper && channelValue.LowBand is decimal lower && channelValue.MovingAverage is decimal middle)
			{
				_donchianUpper = upper;
				_donchianLower = lower;
				_donchianMiddle = middle;
			}
		}

		if (momentumValue.IsFinal)
		{
			var diff = Math.Abs(momentumValue.GetValue<decimal>() - 100m);
			_momentumDiff2 = _momentumDiff1;
			_momentumDiff1 = _momentumDiff0;
			_momentumDiff0 = diff;
		}

		if (macdValue is not null)
		UpdateMacd(macdValue);
	}

	private void UpdateMacd(IIndicatorValue macdValue)
	{
		if (!macdValue.IsFinal)
		return;

		var macd = macdValue.GetValue<MacdValue>();
		_macdMain = macd.Macd;
		_macdSignal = macd.Signal;
	}

	private void ProcessBaseCandle(ICandleMessage candle, decimal fastMa, decimal slowMa)
	{
		if (candle.State != CandleStates.Finished)
		return;

		CheckEquityStop();

		var prevHigh1 = _prevBaseHigh1;
		var prevHigh2 = _prevBaseHigh2;
		var prevLow1 = _prevBaseLow1;
		var prevLow2 = _prevBaseLow2;

		var step = GetPriceStep();
		var distance = MaDistance * step;

		var canEvaluate = _donchianUpper.HasValue && _donchianLower.HasValue && _donchianMiddle.HasValue &&
		_prevHigherOpen.HasValue && _prevHigherLow.HasValue && _prevHigherHigh.HasValue &&
		_momentumDiff0.HasValue && _momentumDiff1.HasValue && _momentumDiff2.HasValue &&
		_macdMain.HasValue && _macdSignal.HasValue &&
		prevHigh1.HasValue && prevHigh2.HasValue && prevLow1.HasValue && prevLow2.HasValue;

		if (canEvaluate)
		{
			var structureLong =
			(_donchianLower!.Value >= _prevHigherLow!.Value && _donchianLower.Value < _prevHigherOpen!.Value) ||
			(_donchianMiddle!.Value <= _prevHigherLow.Value && _donchianMiddle.Value > _prevHigherOpen.Value);

			var structureShort =
			(_donchianUpper!.Value <= _prevHigherHigh!.Value && _donchianUpper.Value > _prevHigherOpen!.Value) ||
			(_donchianMiddle!.Value >= _prevHigherHigh.Value && _donchianMiddle.Value > _prevHigherOpen.Value);

			var swingLong = prevLow2!.Value < prevHigh1!.Value;
			var swingShort = prevLow1!.Value < prevHigh2!.Value;

			var momentumLong = _momentumDiff0!.Value >= MomentumBuyThreshold || _momentumDiff1!.Value >= MomentumBuyThreshold || _momentumDiff2!.Value >= MomentumBuyThreshold;
			var momentumShort = _momentumDiff0!.Value >= MomentumSellThreshold || _momentumDiff1!.Value >= MomentumSellThreshold || _momentumDiff2!.Value >= MomentumSellThreshold;

			var macdLong = (_macdMain!.Value > 0 && _macdMain.Value > _macdSignal!.Value) || (_macdMain.Value < 0 && _macdMain.Value > _macdSignal.Value);
			var macdShort = (_macdMain!.Value > 0 && _macdMain.Value < _macdSignal!.Value) || (_macdMain.Value < 0 && _macdMain.Value < _macdSignal.Value);

			var maLong = fastMa <= slowMa + distance;
			var maShort = fastMa <= slowMa - distance;

			var currentTrades = TradeVolume > 0 ? Math.Abs(Position) / TradeVolume : Math.Abs(Position);
			var canAdd = currentTrades < MaxTrades - 1e-6m;

			if (structureLong && swingLong && momentumLong && macdLong && maLong && Position <= 0 && canAdd)
			{
				BuyMarket(TradeVolume);
				_entryPrice = candle.ClosePrice;
				ResetStops();
			}
			else if (structureShort && swingShort && momentumShort && macdShort && maShort && Position >= 0 && canAdd)
			{
				SellMarket(TradeVolume);
				_entryPrice = candle.ClosePrice;
				ResetStops();
			}
		}

		ManageRisk(candle);

		_prevFastMa = fastMa;
		_prevSlowMa = slowMa;

		_prevBaseHigh2 = _prevBaseHigh1;
		_prevBaseLow2 = _prevBaseLow1;
		_prevBaseHigh1 = candle.HighPrice;
		_prevBaseLow1 = candle.LowPrice;

		UpdateRecentExtremes(candle);
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (Position == 0)
		return;

		var step = GetPriceStep();

		if (Position > 0)
		{
			var stopDistance = StopLoss * step;
			if (StopLoss > 0 && candle.ClosePrice <= _entryPrice - stopDistance)
			{
				CloseLong();
				return;
			}

			var takeDistance = TakeProfit * step;
			if (TakeProfit > 0 && candle.ClosePrice >= _entryPrice + takeDistance)
			{
				CloseLong();
				return;
			}

			if (UseBreakEven && BreakEvenTrigger > 0)
			{
				var trigger = BreakEvenTrigger * step;
				var offset = BreakEvenOffset * step;

				if (candle.ClosePrice - _entryPrice >= trigger)
				{
					var candidate = _entryPrice + offset;
					if (!_trailingStop.HasValue || candidate > _trailingStop.Value)
					_trailingStop = candidate;
				}
			}

			if (UseTrailingStop)
			{
				var trailingPadding = TrailingPadding * step;
				var trailingTrigger = TrailingTrigger * step;
				var trailingStep = TrailingStep * step;

				if (UseCandleTrail && _recentCount >= Math.Max(1, CandleTrailLength))
				{
					var lowest = GetRecentExtreme(_recentLows, true);
					var candidate = lowest - trailingPadding;

					if (candidate < candle.ClosePrice && (!_trailingStop.HasValue || candidate > _trailingStop.Value))
					_trailingStop = candidate;
				}
				else if (!UseCandleTrail && trailingTrigger > 0 && candle.ClosePrice - _entryPrice >= trailingTrigger)
				{
					var candidate = candle.ClosePrice - trailingStep;
					if (!_trailingStop.HasValue || candidate > _trailingStop.Value)
					_trailingStop = candidate;
				}
			}

			if (_trailingStop.HasValue && candle.ClosePrice <= _trailingStop.Value)
			CloseLong();
		}
		else
		{
			var stopDistance = StopLoss * step;
			if (StopLoss > 0 && candle.ClosePrice >= _entryPrice + stopDistance)
			{
				CloseShort();
				return;
			}

			var takeDistance = TakeProfit * step;
			if (TakeProfit > 0 && candle.ClosePrice <= _entryPrice - takeDistance)
			{
				CloseShort();
				return;
			}

			if (UseBreakEven && BreakEvenTrigger > 0)
			{
				var trigger = BreakEvenTrigger * step;
				var offset = BreakEvenOffset * step;

				if (_entryPrice - candle.ClosePrice >= trigger)
				{
					var candidate = _entryPrice - offset;
					if (!_trailingStop.HasValue || candidate < _trailingStop.Value)
					_trailingStop = candidate;
				}
			}

			if (UseTrailingStop)
			{
				var trailingPadding = TrailingPadding * step;
				var trailingTrigger = TrailingTrigger * step;
				var trailingStep = TrailingStep * step;

				if (UseCandleTrail && _recentCount >= Math.Max(1, CandleTrailLength))
				{
					var highest = GetRecentExtreme(_recentHighs, false);
					var candidate = highest + trailingPadding;

					if (candidate > candle.ClosePrice && (!_trailingStop.HasValue || candidate < _trailingStop.Value))
					_trailingStop = candidate;
				}
				else if (!UseCandleTrail && trailingTrigger > 0 && _entryPrice - candle.ClosePrice >= trailingTrigger)
				{
					var candidate = candle.ClosePrice + trailingStep;
					if (!_trailingStop.HasValue || candidate < _trailingStop.Value)
					_trailingStop = candidate;
				}
			}

			if (_trailingStop.HasValue && candle.ClosePrice >= _trailingStop.Value)
			CloseShort();
		}
	}

	private void CheckEquityStop()
	{
		if (!UseEquityStop)
		return;

		_equityPeak = Math.Max(_equityPeak, PnL);
		var drawdown = _equityPeak - PnL;

		if (drawdown >= TotalEquityRisk && Position != 0)
		{
			ClosePosition();
			LogInfo($"Equity stop activated. Drawdown {drawdown:F2} exceeded threshold {TotalEquityRisk:F2}.");
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		ResetStops();
	}

	private void CloseLong()
	{
		if (Position <= 0)
		return;

		SellMarket(Position);
		ResetStops();
	}

	private void CloseShort()
	{
		if (Position >= 0)
		return;

		BuyMarket(Math.Abs(Position));
		ResetStops();
	}

	private void ResetStops()
	{
		_trailingStop = null;
	}

	private void UpdateRecentExtremes(ICandleMessage candle)
	{
		if (CandleTrailLength <= 0)
		return;

		_recentLows[_recentIndex] = candle.LowPrice;
		_recentHighs[_recentIndex] = candle.HighPrice;
		_recentIndex = (_recentIndex + 1) % _recentLows.Length;

		if (_recentCount < _recentLows.Length)
		_recentCount++;
	}

	private decimal GetRecentExtreme(decimal[] buffer, bool useMin)
	{
		var limit = Math.Min(_recentCount, buffer.Length);
		if (limit == 0)
		return useMin ? decimal.MaxValue : decimal.MinValue;

		var extreme = useMin ? decimal.MaxValue : decimal.MinValue;

		for (var i = 0; i < buffer.Length && i < limit; i++)
		{
			var value = buffer[i];
			extreme = useMin ? Math.Min(extreme, value) : Math.Max(extreme, value);
		}

		return extreme;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.Step;
		if (!step.HasValue || step.Value <= 0)
		return 1m;

		return step.Value;
	}
}
