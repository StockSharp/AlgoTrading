namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Bollinger Bands breakout strategy translated from the MQL5 version with N-position control.
/// Opens positions when price closes outside the Bollinger envelope and manages exits via fixed and trailing stops.
/// </summary>
public class BollingerBandsNPositionsStrategy : Strategy
{
	private const decimal VolumeTolerance = 0.00000001m;

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Maximum allowed net position expressed as multiples of <see cref="Volume"/>.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing-step increment in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
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
	/// Initializes <see cref="BollingerBandsNPositionsStrategy"/>.
	/// </summary>
	public BollingerBandsNPositionsStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_maxPositions = Param(nameof(MaxPositions), 9)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Net position limit in multiples of Volume", "Risk");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Moving average length", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Indicators");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing-stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Trailing adjustment increment", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Source candles", "General");
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

		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerWidth
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(bollinger, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (HandleActivePosition(candle))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (TryEnterLong(candle, upper))
		return;

		TryEnterShort(candle, lower);
	}

	private bool HandleActivePosition(ICandleMessage candle)
	{
		if (Position > VolumeTolerance)
		return ManageLong(candle);

		if (Position < -VolumeTolerance)
		return ManageShort(candle);

		if (_longEntryPrice.HasValue || _shortEntryPrice.HasValue)
		{
			ResetLongState();
			ResetShortState();
		}

		return false;
	}

	private bool ManageLong(ICandleMessage candle)
	{
		if (_longEntryPrice is null)
		_longEntryPrice = candle.ClosePrice;

		var entry = _longEntryPrice.Value;
		var step = GetPriceStep();

		if (StopLossPips > 0m)
		{
			var stopLevel = entry - StopLossPips * step;
			if (candle.LowPrice <= stopLevel)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}
		}

		if (TakeProfitPips > 0m)
		{
			var targetLevel = entry + TakeProfitPips * step;
			if (candle.HighPrice >= targetLevel)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}
		}

		if (TrailingStopPips > 0m && TrailingStepPips > 0m)
		{
			var trailingDistance = TrailingStopPips * step;
			var trailingStep = TrailingStepPips * step;
			var activationDistance = trailingDistance + trailingStep;

			if (candle.ClosePrice - entry > activationDistance)
			{
				var candidate = candle.ClosePrice - trailingDistance;

				if (_longTrailingStop is null || candidate - _longTrailingStop.Value > trailingStep)
				_longTrailingStop = candidate;
			}

			if (_longTrailingStop is decimal trailing && candle.LowPrice <= trailing)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}
		}

		return false;
	}

	private bool ManageShort(ICandleMessage candle)
	{
		if (_shortEntryPrice is null)
		_shortEntryPrice = candle.ClosePrice;

		var entry = _shortEntryPrice.Value;
		var step = GetPriceStep();

		if (StopLossPips > 0m)
		{
			var stopLevel = entry + StopLossPips * step;
			if (candle.HighPrice >= stopLevel)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}
		}

		if (TakeProfitPips > 0m)
		{
			var targetLevel = entry - TakeProfitPips * step;
			if (candle.LowPrice <= targetLevel)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}
		}

		if (TrailingStopPips > 0m && TrailingStepPips > 0m)
		{
			var trailingDistance = TrailingStopPips * step;
			var trailingStep = TrailingStepPips * step;
			var activationDistance = trailingDistance + trailingStep;

			if (entry - candle.ClosePrice > activationDistance)
			{
				var candidate = candle.ClosePrice + trailingDistance;

				if (_shortTrailingStop is null || _shortTrailingStop.Value - candidate > trailingStep)
				_shortTrailingStop = candidate;
			}

			if (_shortTrailingStop is decimal trailing && candle.HighPrice >= trailing)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}
		}

		return false;
	}

	private bool TryEnterLong(ICandleMessage candle, decimal upper)
	{
		if (candle.ClosePrice <= upper)
		return false;

		if (!HasCapacity())
		return false;

		if (Position < -VolumeTolerance)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return true;
		}

		if (Position > VolumeTolerance)
		{
			SellMarket(Math.Abs(Position));
			ResetLongState();
			return true;
		}

		BuyMarket(Volume);
		_longEntryPrice = candle.ClosePrice;
		_longTrailingStop = null;
		ResetShortState();
		return true;
	}

	private bool TryEnterShort(ICandleMessage candle, decimal lower)
	{
		if (candle.ClosePrice >= lower)
		return false;

		if (!HasCapacity())
		return false;

		if (Position > VolumeTolerance)
		{
			SellMarket(Math.Abs(Position));
			ResetLongState();
			return true;
		}

		if (Position < -VolumeTolerance)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return true;
		}

		SellMarket(Volume);
		_shortEntryPrice = candle.ClosePrice;
		_shortTrailingStop = null;
		ResetLongState();
		return true;
	}

	private bool HasCapacity()
	{
		if (Volume <= 0m || MaxPositions <= 0)
		return false;

		var limitVolume = MaxPositions * Volume;
		return Math.Abs(Position) < limitVolume - VolumeTolerance;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step <= 0m ? 1m : step;
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
}
