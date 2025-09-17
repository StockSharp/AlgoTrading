using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fraktrak XonaX conversion that replicates the original MetaTrader logic.
/// Trades fractal breakouts with optional reverse mode, position netting and trailing stop control.
/// </summary>
public class FraktrakXonaxAdvancedStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<bool> _reverseMode;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<MoneyManagementMode> _moneyManagementMode;
	private readonly StrategyParam<decimal> _moneyManagementValue;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	private decimal? _upperFractal;
	private decimal? _lowerFractal;
	private decimal? _lastProcessedUpper;
	private decimal? _lastProcessedLower;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal? _longEntry;
	private decimal? _shortEntry;

	private decimal _tickSize;
	private decimal _pipSize;
	private decimal _volumeStep;

	/// <summary>
	/// Stop loss distance in pips. Zero disables the stop loss.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips. Zero disables the take profit.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Requires <see cref="TrailingStepPips"/> to be positive.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal price improvement in pips required to move the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Reverse trading direction: buy on lower fractals and sell on upper fractals.
	/// </summary>
	public bool ReverseMode
	{
		get => _reverseMode.Value;
		set => _reverseMode.Value = value;
	}

	/// <summary>
	/// Close opposite positions before opening a new trade.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Selected position sizing approach.
	/// </summary>
	public MoneyManagementMode ManagementMode
	{
		get => _moneyManagementMode.Value;
		set => _moneyManagementMode.Value = value;
	}

	/// <summary>
	/// Value used by the selected money management mode.
	/// For <see cref="MoneyManagementMode.FixedLot"/> it is the fixed order volume.
	/// For <see cref="MoneyManagementMode.RiskPercent"/> it is the risk percentage per trade.
	/// </summary>
	public decimal ManagementValue
	{
		get => _moneyManagementValue.Value;
		set => _moneyManagementValue.Value = value;
	}

	/// <summary>
	/// Candle source for fractal detection and trading decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Available money management configurations.
	/// </summary>
	public enum MoneyManagementMode
	{
		/// <summary>
		/// Always trade the specified fixed volume.
		/// </summary>
		FixedLot,

		/// <summary>
		/// Use risk percentage per trade to derive the position size.
		/// </summary>
		RiskPercent,
	}

	/// <summary>
	/// Initializes <see cref="FraktrakXonaxAdvancedStrategy"/>.
	/// </summary>
	public FraktrakXonaxAdvancedStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 140)
		.SetDisplay("Take Profit", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetDisplay("Trailing Step", "Minimal improvement required to trail", "Risk");

		_reverseMode = Param(nameof(ReverseMode), false)
		.SetDisplay("Reverse Mode", "Invert trading direction", "Trading");

		_closeOpposite = Param(nameof(CloseOpposite), false)
		.SetDisplay("Close Opposite", "Close opposite positions before entering", "Trading");

		_moneyManagementMode = Param(nameof(ManagementMode), MoneyManagementMode.RiskPercent)
		.SetDisplay("Money Management", "Volume calculation mode", "Trading");

		_moneyManagementValue = Param(nameof(ManagementValue), 3m)
		.SetDisplay("Management Value", "Risk percent or fixed lot size", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Source candles for the strategy", "General");
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

		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_upperFractal = _lowerFractal = _lastProcessedUpper = _lastProcessedLower = null;
		_longStop = _longTake = _shortStop = _shortTake = null;
		_longEntry = _shortEntry = null;
		_tickSize = _pipSize = _volumeStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_tickSize = Security?.PriceStep ?? 0m;
		if (_tickSize <= 0m)
			_tickSize = 0.0001m;

		_volumeStep = Security?.VolumeStep ?? 0m;
		if (_volumeStep <= 0m)
			_volumeStep = 0.01m;

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Shift the last five highs and lows to maintain the fractal window.
		_h1 = _h2;
		_h2 = _h3;
		_h3 = _h4;
		_h4 = _h5;
		_h5 = candle.HighPrice;

		_l1 = _l2;
		_l2 = _l3;
		_l3 = _l4;
		_l4 = _l5;
		_l5 = candle.LowPrice;

		if (candle.State != CandleStates.Finished)
			return;

		DetectFractals();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		EvaluateEntries(candle);
		ManagePositions(candle);
	}

	private void DetectFractals()
	{
		// Check the middle candle against its neighbors to define a fractal.
		if (_h3 > 0m && _h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
			_upperFractal = _h3;

		if (_l3 > 0m && _l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
			_lowerFractal = _l3;
	}

	private void EvaluateEntries(ICandleMessage candle)
	{
		var upperBreak = _upperFractal is decimal up && candle.HighPrice >= up;
		var lowerBreak = _lowerFractal is decimal down && candle.LowPrice <= down;

		if (upperBreak)
		{
			if (!ReverseMode)
				TryEnterLong(candle, _upperFractal!.Value);
			else
				TryEnterShort(candle, _upperFractal!.Value);
		}

		if (lowerBreak)
		{
			if (!ReverseMode)
				TryEnterShort(candle, _lowerFractal!.Value);
			else
				TryEnterLong(candle, _lowerFractal!.Value);
		}
	}

	private void TryEnterLong(ICandleMessage candle, decimal breakoutLevel)
	{
		if (_lastProcessedUpper is decimal last && last == breakoutLevel)
			return;

		if (Position > 0m)
			return;

		if (Position < 0m)
		{
			if (CloseOpposite)
			{
				BuyMarket(-Position);
				ResetStops();
			}

			return;
		}

		var entryPrice = Security?.BestAsk ?? candle.ClosePrice;
		var stopPrice = StopLossPips > 0 ? entryPrice - StopLossPips * _pipSize : (decimal?)null;
		var volume = CalculateOrderVolume(entryPrice, stopPrice);
		volume = RoundVolume(volume);

		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_lastProcessedUpper = breakoutLevel;
		_longEntry = entryPrice;
		SetupInitialLongLevels(entryPrice);
	}

	private void TryEnterShort(ICandleMessage candle, decimal breakoutLevel)
	{
		if (_lastProcessedLower is decimal last && last == breakoutLevel)
			return;

		if (Position < 0m)
			return;

		if (Position > 0m)
		{
			if (CloseOpposite)
			{
				SellMarket(Position);
				ResetStops();
			}

			return;
		}

		var entryPrice = Security?.BestBid ?? candle.ClosePrice;
		var stopPrice = StopLossPips > 0 ? entryPrice + StopLossPips * _pipSize : (decimal?)null;
		var volume = CalculateOrderVolume(entryPrice, stopPrice);
		volume = RoundVolume(volume);

		if (volume <= 0m)
			return;

		SellMarket(volume);

		_lastProcessedLower = breakoutLevel;
		_shortEntry = entryPrice;
		SetupInitialShortLevels(entryPrice);
	}

	private void ManagePositions(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			_longEntry ??= PositionAvgPrice;

			if (_longEntry is decimal entry)
			{
				if (_longStop is null && StopLossPips > 0)
					_longStop = entry - StopLossPips * _pipSize;

				if (_longTake is null && TakeProfitPips > 0)
					_longTake = entry + TakeProfitPips * _pipSize;

				if (_longTake is decimal take && candle.HighPrice >= take)
				{
					SellMarket(Position);
					ResetStops();
					return;
				}

				if (_longStop is decimal stop && candle.LowPrice <= stop)
				{
					SellMarket(Position);
					ResetStops();
					return;
				}

				UpdateLongTrailing(candle, entry);
			}
		}
		else if (Position < 0m)
		{
			_shortEntry ??= PositionAvgPrice;

			if (_shortEntry is decimal entry)
			{
				if (_shortStop is null && StopLossPips > 0)
					_shortStop = entry + StopLossPips * _pipSize;

				if (_shortTake is null && TakeProfitPips > 0)
					_shortTake = entry - TakeProfitPips * _pipSize;

				if (_shortTake is decimal take && candle.LowPrice <= take)
				{
					BuyMarket(-Position);
					ResetStops();
					return;
				}

				if (_shortStop is decimal stop && candle.HighPrice >= stop)
				{
					BuyMarket(-Position);
					ResetStops();
					return;
				}

				UpdateShortTrailing(candle, entry);
			}
		}
		else
		{
			ResetStops();
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle, decimal entry)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		var gain = candle.ClosePrice - entry;
		if (gain < trailingDistance + trailingStep)
			return;

		var desiredStop = candle.ClosePrice - trailingDistance;
		if (_longStop is decimal currentStop && desiredStop <= currentStop + trailingStep / 2m)
			return;

		_longStop = desiredStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle, decimal entry)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		var gain = entry - candle.ClosePrice;
		if (gain < trailingDistance + trailingStep)
			return;

		var desiredStop = candle.ClosePrice + trailingDistance;
		if (_shortStop is decimal currentStop && desiredStop >= currentStop - trailingStep / 2m)
			return;

		_shortStop = desiredStop;
	}

	private decimal CalculateOrderVolume(decimal entryPrice, decimal? stopPrice)
	{
		if (ManagementMode == MoneyManagementMode.FixedLot || stopPrice is null)
			return ManagementValue;

		var stopDistance = Math.Abs(entryPrice - stopPrice.Value);
		if (stopDistance <= 0m)
			return ManagementValue;

		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return ManagementValue;

		var riskAmount = equity * (ManagementValue / 100m);
		if (riskAmount <= 0m)
			return ManagementValue;

		var stepCost = Security?.PriceStepCost ?? 0m;
		var step = Security?.PriceStep ?? 0m;

		if (step > 0m && stepCost > 0m)
		{
			var costPerUnit = stopDistance / step * stepCost;
			if (costPerUnit > 0m)
			{
				var volumeByCost = riskAmount / costPerUnit;
				if (volumeByCost > 0m)
					return volumeByCost;
			}
		}

		var volume = riskAmount / stopDistance;
		return volume > 0m ? volume : ManagementValue;
	}

	private decimal RoundVolume(decimal volume)
	{
		var absVolume = Math.Abs(volume);
		if (absVolume <= 0m)
			return 0m;

		if (_volumeStep <= 0m)
			return absVolume;

		var steps = Math.Round(absVolume / _volumeStep, MidpointRounding.AwayFromZero);
		if (steps <= 0m)
			steps = 1m;

		return steps * _volumeStep;
	}

	private void SetupInitialLongLevels(decimal entryPrice)
	{
		_longStop = StopLossPips > 0 ? entryPrice - StopLossPips * _pipSize : null;
		_longTake = TakeProfitPips > 0 ? entryPrice + TakeProfitPips * _pipSize : null;
		_shortStop = _shortTake = null;
	}

	private void SetupInitialShortLevels(decimal entryPrice)
	{
		_shortStop = StopLossPips > 0 ? entryPrice + StopLossPips * _pipSize : null;
		_shortTake = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * _pipSize : null;
		_longStop = _longTake = null;
	}

	private void ResetStops()
	{
		_longStop = _longTake = _shortStop = _shortTake = null;
		_longEntry = _shortEntry = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			_longEntry = PositionAvgPrice;
			_shortEntry = null;
			SetupInitialLongLevels(PositionAvgPrice);
		}
		else if (Position < 0m)
		{
			_shortEntry = PositionAvgPrice;
			_longEntry = null;
			SetupInitialShortLevels(PositionAvgPrice);
		}
		else
		{
			ResetStops();
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		var digits = CountDecimalDigits(step);
		return digits is 3 or 5 ? step * 10m : step;
	}

	private static int CountDecimalDigits(decimal value)
	{
		var digits = 0;
		var normalized = value;

		while (digits < 10 && normalized != Math.Truncate(normalized))
		{
			normalized *= 10m;
			digits++;
		}

		return digits;
	}
}
