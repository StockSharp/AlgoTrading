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
/// Port of the NTK 07 MetaTrader strategy that trades stop orders around a recent range.
/// </summary>
public class Ntk07RangeTraderStrategy : Strategy
{
	public enum TradeModeOptions
	{
		EdgesOfRange,
		CenterOfRange,
	}

	private readonly StrategyParam<decimal> _entryVolume;
	private readonly StrategyParam<decimal> _totalVolumeLimit;
	private readonly StrategyParam<decimal> _netStepPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<bool> _trailHighLow;
	private readonly StrategyParam<bool> _trailMa;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<int> _rangeBars;
	private readonly StrategyParam<TradeModeOptions> _tradeMode;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _movingAverage;
	private Highest _rangeHighIndicator;
	private Lowest _rangeLowIndicator;

	private ICandleMessage _previousCandle;
	private decimal _priceStep;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Base volume used for each entry order.
	/// </summary>
	public decimal EntryVolume
	{
		get => _entryVolume.Value;
		set => _entryVolume.Value = value;
	}

	/// <summary>
	/// Maximum total exposure allowed for the strategy. Set to zero for unlimited exposure.
	/// </summary>
	public decimal TotalVolumeLimit
	{
		get => _totalVolumeLimit.Value;
		set => _totalVolumeLimit.Value = value;
	}

	/// <summary>
	/// Distance of stop orders from the market in price steps.
	/// </summary>
	public decimal NetStepPoints
	{
		get => _netStepPoints.Value;
		set => _netStepPoints.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Additional volume multiplier used when pyramiding into an existing position.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Enables trailing based on previous candle extremes.
	/// </summary>
	public bool UseTrailingAtHighLow
	{
		get => _trailHighLow.Value;
		set => _trailHighLow.Value = value;
	}

	/// <summary>
	/// Enables trailing based on a moving average.
	/// </summary>
	public bool UseTrailingMa
	{
		get => _trailMa.Value;
		set => _trailMa.Value = value;
	}

	/// <summary>
	/// Inclusive starting hour for trading (platform time).
	/// </summary>
	public int TradingStartHour
	{
		get => _tradingStartHour.Value;
		set => _tradingStartHour.Value = value;
	}

	/// <summary>
	/// Inclusive ending hour for trading (platform time).
	/// </summary>
	public int TradingEndHour
	{
		get => _tradingEndHour.Value;
		set => _tradingEndHour.Value = value;
	}

	/// <summary>
	/// Number of completed candles that define the reference range. Set to zero to disable range filtering.
	/// </summary>
	public int RangeBars
	{
		get => _rangeBars.Value;
		set => _rangeBars.Value = value;
	}

	/// <summary>
	/// Range interaction mode for entry logic.
	/// </summary>
	public TradeModeOptions TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Length of the moving average used for trailing stops.
	/// </summary>
	public int MovingAveragePeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public Ntk07RangeTraderStrategy()
	{
		_entryVolume = Param(nameof(EntryVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Entry Volume", "Base volume for each entry order", "Risk");

		_totalVolumeLimit = Param(nameof(TotalVolumeLimit), 7m)
		.SetNotNegative()
		.SetDisplay("Total Volume Limit", "Maximum aggregated volume (0 disables the limit)", "Risk");

		_netStepPoints = Param(nameof(NetStepPoints), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Net Step", "Offset for stop entries measured in price steps", "Entries");

		_stopLossPoints = Param(nameof(StopLossPoints), 11m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Initial stop distance measured in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 30m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take-profit distance measured in price steps", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 8m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop", "Distance used for trailing calculations in price steps", "Risk");

		_lotMultiplier = Param(nameof(LotMultiplier), 1.7m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Multiplier", "Volume multiplier when pyramiding", "Risk");

		_trailHighLow = Param(nameof(UseTrailingAtHighLow), true)
		.SetDisplay("Trail High/Low", "Use previous candle extremes for trailing", "Risk");

		_trailMa = Param(nameof(UseTrailingMa), false)
		.SetDisplay("Trail Moving Average", "Use moving average value for trailing", "Risk");

		_tradingStartHour = Param(nameof(TradingStartHour), 0)
		.SetDisplay("Trading Start Hour", "Trading window opening hour", "Sessions");

		_tradingEndHour = Param(nameof(TradingEndHour), 23)
		.SetDisplay("Trading End Hour", "Trading window closing hour", "Sessions");

		_rangeBars = Param(nameof(RangeBars), 0)
		.SetNotNegative()
		.SetDisplay("Range Bars", "Number of completed candles used for the range", "Entries");

		_tradeMode = Param(nameof(TradeMode), TradeModeOptions.EdgesOfRange)
		.SetDisplay("Trade Mode", "How price interacts with the range before placing orders", "Entries");

		_maPeriod = Param(nameof(MovingAveragePeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Moving average length for trailing", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");
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

		_previousCandle = null;
		_movingAverage = null;
		_rangeHighIndicator = null;
		_rangeLowIndicator = null;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (TradingStartHour < 0 || TradingStartHour > 23)
		throw new InvalidOperationException("TradingStartHour must be between 0 and 23.");

		if (TradingEndHour < 0 || TradingEndHour > 23)
		throw new InvalidOperationException("TradingEndHour must be between 0 and 23.");

		if (TradingStartHour >= TradingEndHour)
		throw new InvalidOperationException("TradingStartHour must be strictly less than TradingEndHour.");

		if (UseTrailingAtHighLow && UseTrailingMa)
		throw new InvalidOperationException("Only one trailing mode can be enabled at a time.");

		_priceStep = Security?.PriceStep ?? 1m;
		_movingAverage = new SimpleMovingAverage { Length = MovingAveragePeriod };

		var subscription = SubscribeCandles(CandleType);

		if (RangeBars > 0)
		{
			_rangeHighIndicator = new Highest { Length = Math.Max(2, RangeBars) };
			_rangeLowIndicator = new Lowest { Length = Math.Max(2, RangeBars) };

			subscription
			.Bind(_movingAverage, _rangeHighIndicator, _rangeLowIndicator, ProcessCandleWithRange)
			.Start();
		}
		else
		{
			subscription
			.Bind(_movingAverage, ProcessCandleWithoutRange)
			.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);

			if (UseTrailingMa)
			DrawIndicator(area, _movingAverage);

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandleWithoutRange(ICandleMessage candle, decimal maValue)
	{
		ProcessCandleInternal(candle, maValue, null, null);
	}

	private void ProcessCandleWithRange(ICandleMessage candle, decimal maValue, decimal rangeHigh, decimal rangeLow)
	{
		var highValue = _rangeHighIndicator != null && _rangeHighIndicator.IsFormed ? rangeHigh : (decimal?)null;
		var lowValue = _rangeLowIndicator != null && _rangeLowIndicator.IsFormed ? rangeLow : (decimal?)null;

		ProcessCandleInternal(candle, maValue, highValue, lowValue);
	}

	private void ProcessCandleInternal(ICandleMessage candle, decimal maValue, decimal? rangeHigh, decimal? rangeLow)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var hour = candle.CloseTime.Hour;
		if (hour < TradingStartHour || hour > TradingEndHour)
		{
			_previousCandle = candle;
			return;
		}

		// Check SL/TP first.
		CheckProtection(candle);

		var netOffset = ToPrice(NetStepPoints);

		if (Position == 0 && netOffset > 0m)
		{
			// Flat - check if candle broke through entry levels.
			var allowEntries = true;

			if (rangeHigh.HasValue && rangeLow.HasValue && rangeHigh.Value > rangeLow.Value)
			{
				allowEntries = TradeMode switch
				{
					TradeModeOptions.EdgesOfRange => candle.ClosePrice >= rangeHigh.Value || candle.ClosePrice <= rangeLow.Value,
					TradeModeOptions.CenterOfRange => Math.Abs(candle.ClosePrice - ((rangeHigh.Value + rangeLow.Value) / 2m)) <= _priceStep,
					_ => true,
				};
			}

			if (allowEntries && _previousCandle != null)
			{
				var buyLevel = _previousCandle.ClosePrice + netOffset;
				var sellLevel = _previousCandle.ClosePrice - netOffset;

				if (candle.HighPrice >= buyLevel)
				{
					BuyMarket(EntryVolume);
					_entryPrice = candle.ClosePrice;
					SetProtectionLevels(true, candle, maValue);
				}
				else if (candle.LowPrice <= sellLevel)
				{
					SellMarket(EntryVolume);
					_entryPrice = candle.ClosePrice;
					SetProtectionLevels(false, candle, maValue);
				}
			}
		}
		else if (Position > 0)
		{
			// Update trailing stop for longs.
			UpdateLongTrailing(candle, maValue);
		}
		else if (Position < 0)
		{
			// Update trailing stop for shorts.
			UpdateShortTrailing(candle, maValue);
		}

		_previousCandle = candle;
	}

	private void SetProtectionLevels(bool isLong, ICandleMessage candle, decimal maValue)
	{
		var stopLossOffset = ToPrice(StopLossPoints);
		var takeProfitOffset = ToPrice(TakeProfitPoints);

		if (isLong)
		{
			_stopPrice = stopLossOffset > 0m ? _entryPrice - stopLossOffset : null;
			_takePrice = takeProfitOffset > 0m ? _entryPrice + takeProfitOffset : null;
		}
		else
		{
			_stopPrice = stopLossOffset > 0m ? _entryPrice + stopLossOffset : null;
			_takePrice = takeProfitOffset > 0m ? _entryPrice - takeProfitOffset : null;
		}
	}

	private void CheckProtection(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Position);
				ResetProtection();
				return;
			}
			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Position);
				ResetProtection();
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}
			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
			}
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle, decimal maValue)
	{
		var trailingOffset = ToPrice(TrailingStopPoints);
		decimal? newStop = _stopPrice;

		if (UseTrailingAtHighLow && _previousCandle != null)
		{
			var candidate = _previousCandle.LowPrice;
			if (candidate > 0m && (newStop == null || candidate > newStop.Value))
				newStop = candidate;
		}
		else if (UseTrailingMa && maValue > 0m)
		{
			if (newStop == null || maValue > newStop.Value)
				newStop = maValue;
		}
		else if (trailingOffset > 0m)
		{
			var candidate = candle.ClosePrice - trailingOffset;
			if (newStop == null || candidate > newStop.Value)
				newStop = candidate;
		}

		if (newStop.HasValue)
		{
			var maxStop = candle.ClosePrice - _priceStep;
			newStop = Math.Min(newStop.Value, maxStop);
			newStop = Math.Max(newStop.Value, 0m);
		}

		_stopPrice = newStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle, decimal maValue)
	{
		var trailingOffset = ToPrice(TrailingStopPoints);
		decimal? newStop = _stopPrice;

		if (UseTrailingAtHighLow && _previousCandle != null)
		{
			var candidate = _previousCandle.HighPrice;
			if (candidate > 0m && (newStop == null || candidate < newStop.Value))
				newStop = candidate;
		}
		else if (UseTrailingMa && maValue > 0m)
		{
			if (newStop == null || maValue < newStop.Value)
				newStop = maValue;
		}
		else if (trailingOffset > 0m)
		{
			var candidate = candle.ClosePrice + trailingOffset;
			if (newStop == null || candidate < newStop.Value)
				newStop = candidate;
		}

		if (newStop.HasValue)
		{
			var minStop = candle.ClosePrice + _priceStep;
			newStop = Math.Max(newStop.Value, minStop);
		}

		_stopPrice = newStop;
	}

	private void ResetProtection()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	private decimal ToPrice(decimal points)
	{
		if (points <= 0m)
			return 0m;

		var step = _priceStep > 0m ? _priceStep : 1m;
		return points * step;
	}
}