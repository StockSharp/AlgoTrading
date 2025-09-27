namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the Dematus expert advisor with DeMarker crossovers and equity protection.
/// </summary>
public class DematusStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<decimal> _trailingEquity;
	private readonly StrategyParam<decimal> _virtualStopEquity;
	private readonly StrategyParam<decimal> _trailingStartEquity;
	private readonly StrategyParam<int> _demarkerLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<bool> _resetEntryPrice;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _nextOrderVolume;
	private decimal _virtualPosition;
	private decimal? _lastEntryPrice;

	private decimal _balanceReference;
	private decimal _virtualStopLevel;

	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;
	private decimal _longPositionVolume;
	private decimal _shortPositionVolume;
	private bool _equityExitRequested;

	private decimal? _demarkerCurrent;
	private decimal? _demarkerPrevious;
	private decimal? _demarkerTwoAgo;

	/// <summary>
	/// Base order volume used for the first position.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// DeMarker indicator averaging period.
	/// </summary>
	public int DemarkerLength
	{
		get => _demarkerLength.Value;
		set => _demarkerLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional profit distance required before the trailing stop is tightened.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Minimum price distance in pips before adding to a position.
	/// </summary>
	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	/// <summary>
	/// Equity offset used when trailing account equity.
	/// </summary>
	public decimal TrailingEquity
	{
		get => _trailingEquity.Value;
		set => _trailingEquity.Value = value;
	}

	/// <summary>
	/// Initial equity buffer below balance before emergency liquidation is triggered.
	/// </summary>
	public decimal VirtualStopEquity
	{
		get => _virtualStopEquity.Value;
		set => _virtualStopEquity.Value = value;
	}

	/// <summary>
	/// Gain above balance required before equity trailing becomes active.
	/// </summary>
	public decimal TrailingStartEquity
	{
		get => _trailingStartEquity.Value;
		set => _trailingStartEquity.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the last executed volume for pyramiding.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Oversold level used for DeMarker crossovers.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level used for DeMarker crossovers.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Reset the last entry price after exits when enabled.
	/// </summary>
	public bool ResetEntryPrice
	{
		get => _resetEntryPrice.Value;
		set => _resetEntryPrice.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DematusStrategy"/> class.
	/// </summary>
	public DematusStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Starting order volume.", "Trading");

		_demarkerLength = Param(nameof(DemarkerLength), 13)
		.SetGreaterThanZero()
		.SetDisplay("DeMarker Length", "DeMarker indicator period.", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_stopLossPips = Param(nameof(StopLossPips), 999m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips.", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Extra profit required to tighten trailing stop.", "Risk");

		_distancePips = Param(nameof(DistancePips), 50m)
		.SetNotNegative()
		.SetDisplay("Re-entry Distance (pips)", "Minimum distance before scaling the position.", "Trading");

		_trailingEquity = Param(nameof(TrailingEquity), 5m)
		.SetNotNegative()
		.SetDisplay("Equity Trailing Offset", "Offset between equity peak and liquidation threshold.", "Risk");

		_virtualStopEquity = Param(nameof(VirtualStopEquity), 99999m)
		.SetNotNegative()
		.SetDisplay("Virtual Stop Equity", "Initial buffer below balance before closing all positions.", "Risk");

		_trailingStartEquity = Param(nameof(TrailingStartEquity), 20m)
		.SetNotNegative()
		.SetDisplay("Equity Trailing Trigger", "Gain above balance needed to activate equity trailing.", "Risk");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
		.SetGreaterOrEqual(1m)
		.SetDisplay("Volume Multiplier", "Multiplier applied to the last executed volume.", "Trading");

		_oversoldLevel = Param(nameof(OversoldLevel), 0.3m)
		.SetRange(0m, 1m)
		.SetDisplay("Oversold Level", "Oversold threshold for DeMarker crossovers.", "Signals");

		_overboughtLevel = Param(nameof(OverboughtLevel), 0.7m)
		.SetRange(0m, 1m)
		.SetDisplay("Overbought Level", "Overbought threshold for DeMarker crossovers.", "Signals");

		_resetEntryPrice = Param(nameof(ResetEntryPrice), false)
		.SetDisplay("Reset Entry Price", "Reset the last entry price whenever an exit happens.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for calculations.", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_nextOrderVolume = 0m;
		_virtualPosition = 0m;
		_lastEntryPrice = null;
		_balanceReference = 0m;
		_virtualStopLevel = 0m;
		_longStop = null;
		_shortStop = null;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;
		_longPositionVolume = 0m;
		_shortPositionVolume = 0m;
		_demarkerCurrent = null;
		_demarkerPrevious = null;
		_demarkerTwoAgo = null;
		_equityExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		StartProtection();

		_pipSize = ComputePipSize();
		_nextOrderVolume = EnsureVolume(InitialVolume);
		_balanceReference = Portfolio?.CurrentValue ?? 0m;
		_virtualStopLevel = _balanceReference - VirtualStopEquity;

	var deMarker = new DeMarkerIndicator { Length = DemarkerLength };
		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(deMarker, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, deMarker);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal demarkerValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateEquityTrailing();
		if (UpdateTrailingStops(candle))
		return;

		UpdateDemarkerHistory(demarkerValue);
		if (_demarkerCurrent is not decimal current || _demarkerTwoAgo is not decimal twoAgo)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var oversoldCross = twoAgo < OversoldLevel && current > OversoldLevel;
		var overboughtCross = twoAgo > OverboughtLevel && current < OverboughtLevel;
		var distance = ToPrice(DistancePips);
		var price = candle.ClosePrice;

		if (_virtualPosition == 0m)
		{
			if (oversoldCross)
			{
				EnterLong();
			}
			else if (overboughtCross)
			{
				EnterShort();
			}
		}
		else
		{
			if (oversoldCross && _lastEntryPrice is decimal lastLong && lastLong - price > distance)
			{
				EnterLong();
			}
			else if (overboughtCross && _lastEntryPrice is decimal lastShort && price - lastShort > distance)
			{
				EnterShort();
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null || trade.Trade == null)
		return;

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
		return;

		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			var prevLong = _longPositionVolume;
			var prevShort = _shortPositionVolume;

			_virtualPosition += volume;

			var currLong = Math.Max(0m, _virtualPosition);
			var currShort = Math.Max(0m, -_virtualPosition);

			var addedLong = Math.Max(0m, currLong - prevLong);
			var reducedShort = Math.Max(0m, prevShort - currShort);

			if (addedLong > 0m)
			RegisterLongEntry(price, addedLong, currLong);

			if (reducedShort > 0m && ResetEntryPrice)
			_lastEntryPrice = null;

			_longPositionVolume = currLong;
			_shortPositionVolume = currShort;

			if (currLong == 0m)
			_longAveragePrice = 0m;
			if (currShort == 0m)
			_shortAveragePrice = 0m;
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			var prevLong = _longPositionVolume;
			var prevShort = _shortPositionVolume;

			_virtualPosition -= volume;

			var currLong = Math.Max(0m, _virtualPosition);
			var currShort = Math.Max(0m, -_virtualPosition);

			var addedShort = Math.Max(0m, currShort - prevShort);
			var reducedLong = Math.Max(0m, prevLong - currLong);

			if (addedShort > 0m)
			RegisterShortEntry(price, addedShort, currShort);

			if (reducedLong > 0m && ResetEntryPrice)
			_lastEntryPrice = null;

			_longPositionVolume = currLong;
			_shortPositionVolume = currShort;

			if (currLong == 0m)
			_longAveragePrice = 0m;
			if (currShort == 0m)
			_shortAveragePrice = 0m;
		}

		if (_virtualPosition == 0m)
		ResetAfterFlat();
	}

	private void RegisterLongEntry(decimal price, decimal volumeAdded, decimal totalVolume)
	{
		var previousVolume = totalVolume - volumeAdded;
		_longAveragePrice = previousVolume > 0m
		? (_longAveragePrice * previousVolume + price * volumeAdded) / totalVolume
		: price;

		_longStop = StopLossPips > 0m ? price - ToPrice(StopLossPips) : null;
		_shortStop = null;
		_lastEntryPrice = price;

		UpdateNextVolume(volumeAdded);
	}

	private void RegisterShortEntry(decimal price, decimal volumeAdded, decimal totalVolume)
	{
		var previousVolume = totalVolume - volumeAdded;
		_shortAveragePrice = previousVolume > 0m
		? (_shortAveragePrice * previousVolume + price * volumeAdded) / totalVolume
		: price;

		_shortStop = StopLossPips > 0m ? price + ToPrice(StopLossPips) : null;
		_longStop = null;
		_lastEntryPrice = price;

		UpdateNextVolume(volumeAdded);
	}

	private void UpdateDemarkerHistory(decimal value)
	{
		_demarkerTwoAgo = _demarkerPrevious;
		_demarkerPrevious = _demarkerCurrent;
		_demarkerCurrent = value;
	}

	private void UpdateEquityTrailing()
	{
		var equity = Portfolio?.CurrentValue ?? 0m;

		if (_virtualPosition == 0m)
		{
			_balanceReference = equity;
			_virtualStopLevel = _balanceReference - VirtualStopEquity;
			_equityExitRequested = false;
		}

		if (equity - TrailingEquity - TrailingStartEquity > _balanceReference)
		{
			var candidate = equity - TrailingEquity;
			if (candidate > _virtualStopLevel)
			_virtualStopLevel = candidate;
		}

		if (_virtualPosition != 0m && equity < _virtualStopLevel && !_equityExitRequested)
		{
			CloseAllPositions();
			_equityExitRequested = true;
		}
	}

	private bool UpdateTrailingStops(ICandleMessage candle)
	{
		var trailingStop = ToPrice(TrailingStopPips);
		var trailingStep = ToPrice(TrailingStepPips);

		if (_virtualPosition > 0m)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(_virtualPosition);
				return true;
			}

			if (TrailingStopPips > 0m)
			{
				var profitDistance = candle.ClosePrice - _longAveragePrice;
				if (profitDistance > trailingStop + trailingStep)
				{
					var desiredStop = candle.ClosePrice - trailingStop;
					if (!_longStop.HasValue || desiredStop > _longStop.Value + trailingStep)
					_longStop = desiredStop;
				}

				if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
				{
					SellMarket(_virtualPosition);
					return true;
				}
			}
		}
		else if (_virtualPosition < 0m)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(-_virtualPosition);
				return true;
			}

			if (TrailingStopPips > 0m)
			{
				var profitDistance = _shortAveragePrice - candle.ClosePrice;
				if (profitDistance > trailingStop + trailingStep)
				{
					var desiredStop = candle.ClosePrice + trailingStop;
					if (!_shortStop.HasValue || desiredStop < _shortStop.Value - trailingStep)
					_shortStop = desiredStop;
				}

				if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
				{
					BuyMarket(-_virtualPosition);
					return true;
				}
			}
		}
		else
		{
			_longStop = null;
			_shortStop = null;
		}

		return false;
	}

	private void EnterLong()
	{
		var volume = _nextOrderVolume;
		if (volume <= 0m)
		volume = EnsureVolume(InitialVolume);

		if (volume > 0m)
		BuyMarket(volume);
	}

	private void EnterShort()
	{
		var volume = _nextOrderVolume;
		if (volume <= 0m)
		volume = EnsureVolume(InitialVolume);

		if (volume > 0m)
		SellMarket(volume);
	}

	private void CloseAllPositions()
	{
		if (_virtualPosition > 0m)
		{
			SellMarket(_virtualPosition);
		}
		else if (_virtualPosition < 0m)
		{
			BuyMarket(-_virtualPosition);
		}
	}

	private void ResetAfterFlat()
	{
		_virtualPosition = 0m;
		_longPositionVolume = 0m;
		_shortPositionVolume = 0m;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;
		_longStop = null;
		_shortStop = null;
		_lastEntryPrice = null;
		_nextOrderVolume = EnsureVolume(InitialVolume);
		_equityExitRequested = false;
	}

	private void UpdateNextVolume(decimal executedVolume)
	{
		var next = executedVolume * VolumeMultiplier;
		_nextOrderVolume = EnsureVolume(next);
	}

	private decimal EnsureVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var result = volume;
		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		result = Math.Floor(result / step) * step;

		var min = Security?.VolumeMin;
		if (min is decimal minVolume && minVolume > 0m && result < minVolume)
		result = minVolume;

		var max = Security?.VolumeMax;
		if (max is decimal maxVolume && maxVolume > 0m && result > maxVolume)
		result = maxVolume;

		return result;
	}

	private decimal ToPrice(decimal pips)
	{
		return pips * _pipSize;
	}

	private decimal ComputePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		var decimals = GetDecimalPlaces(step);
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0x7F;
	}
}
