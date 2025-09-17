using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MasterMind3 port that relies on four Williams %R oscillators hitting extremes.
/// </summary>
public class MasterMindTripleWprStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<int> _stopLossSteps;
	private readonly StrategyParam<int> _takeProfitSteps;
	private readonly StrategyParam<int> _trailingStopSteps;
	private readonly StrategyParam<int> _trailingStepSteps;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR _wpr26 = null!;
	private WilliamsR _wpr27 = null!;
	private WilliamsR _wpr29 = null!;
	private WilliamsR _wpr30 = null!;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Target net position volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Threshold that defines oversold conditions for all oscillators.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Threshold that defines overbought conditions for all oscillators.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument price steps.
	/// </summary>
	public int StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument price steps.
	/// </summary>
	public int TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance expressed in instrument price steps.
	/// </summary>
	public int TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Minimal improvement in price steps required before trailing stop is moved.
	/// </summary>
	public int TrailingStepSteps
	{
		get => _trailingStepSteps.Value;
		set => _trailingStepSteps.Value = value;
	}

	/// <summary>
	/// Candle series processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="MasterMindTripleWprStrategy"/>.
	/// </summary>
	public MasterMindTripleWprStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Target net position volume", "Trading")
		.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), -99.99m)
		.SetDisplay("Oversold Level", "All Williams %R must be below this level", "Signals")
		.SetCanOptimize(true);

		_overboughtLevel = Param(nameof(OverboughtLevel), -0.01m)
		.SetDisplay("Overbought Level", "All Williams %R must be above this level", "Signals")
		.SetCanOptimize(true);

		_stopLossSteps = Param(nameof(StopLossSteps), 2000)
		.SetDisplay("Stop Loss (steps)", "Protective stop distance in price steps", "Risk");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 0)
		.SetDisplay("Take Profit (steps)", "Take profit distance in price steps", "Risk");

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 0)
		.SetDisplay("Trailing Stop (steps)", "Trailing stop distance in price steps", "Risk");

		_trailingStepSteps = Param(nameof(TrailingStepSteps), 1)
		.SetDisplay("Trailing Step (steps)", "Minimal improvement before trailing adjusts", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe to process", "General");
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

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		// Initialize Williams %R oscillators with the original periods.
		_wpr26 = new WilliamsR { Length = 26 };
		_wpr27 = new WilliamsR { Length = 27 };
		_wpr29 = new WilliamsR { Length = 29 };
		_wpr30 = new WilliamsR { Length = 30 };

		// Subscribe to candle data and bind all four oscillators.
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_wpr26, _wpr27, _wpr29, _wpr30, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _wpr26);
			DrawIndicator(area, _wpr27);
			DrawIndicator(area, _wpr29);
			DrawIndicator(area, _wpr30);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wpr26Value, decimal wpr27Value, decimal wpr29Value, decimal wpr30Value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_wpr26.IsFormed || !_wpr27.IsFormed || !_wpr29.IsFormed || !_wpr30.IsFormed)
		return;

		// Update trailing stops before evaluating exits.
		UpdateTrailingStops(candle);
		TryCloseByRisk(candle);

		var oversoldLevel = OversoldLevel;
		var overboughtLevel = OverboughtLevel;

		var isOversold = wpr26Value <= oversoldLevel &&
		wpr27Value <= oversoldLevel &&
		wpr29Value <= oversoldLevel &&
		wpr30Value <= oversoldLevel;

		var isOverbought = wpr26Value >= overboughtLevel &&
		wpr27Value >= overboughtLevel &&
		wpr29Value >= overboughtLevel &&
		wpr30Value >= overboughtLevel;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (isOversold)
		{
			OpenLong(candle);
		}
		else if (isOverbought)
		{
			OpenShort(candle);
		}
	}

	private void OpenLong(ICandleMessage candle)
	{
		var target = TradeVolume;
		if (target <= 0m)
		return;

		var current = Position;
		var difference = target - current;
		if (difference <= 0m)
		return;

		var existingLong = Math.Max(current, 0m);

		// A single market order flips the position when needed.
		BuyMarket(difference);

		var entryPrice = candle.ClosePrice;
		UpdateLongState(existingLong, difference, entryPrice);
	}

	private void OpenShort(ICandleMessage candle)
	{
		var target = -TradeVolume;
		if (target >= 0m)
		return;

		var current = Position;
		var difference = current - target;
		if (difference <= 0m)
		return;

		var existingShort = Math.Max(-current, 0m);

		SellMarket(difference);

		var entryPrice = candle.ClosePrice;
		UpdateShortState(existingShort, difference, entryPrice);
	}

	private void TryCloseByRisk(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				// Stop-loss for long positions.
				SellMarket(Position);
				ResetLongState();
				return;
			}

			if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				// Take-profit for long positions.
				SellMarket(Position);
				ResetLongState();
			}
		}
		else if (Position < 0m)
		{
			var shortVolume = Math.Abs(Position);

			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(shortVolume);
				ResetShortState();
				return;
			}

			if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(shortVolume);
				ResetShortState();
			}
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (TrailingStopSteps <= 0 || TrailingStepSteps <= 0)
		return;

		var step = GetStepSize();
		var trailingDistance = TrailingStopSteps * step;
		var trailingStep = TrailingStepSteps * step;

		if (Position > 0m && _longEntryPrice.HasValue)
		{
			var profit = candle.ClosePrice - _longEntryPrice.Value;
			if (profit > trailingDistance + trailingStep)
			{
				var newStop = candle.ClosePrice - trailingDistance;

				if (!_longStopPrice.HasValue || newStop > _longStopPrice.Value + trailingStep)
				{
					_longStopPrice = newStop;
				}
			}
		}
		else if (Position < 0m && _shortEntryPrice.HasValue)
		{
			var profit = _shortEntryPrice.Value - candle.ClosePrice;
			if (profit > trailingDistance + trailingStep)
			{
				var newStop = candle.ClosePrice + trailingDistance;

				if (!_shortStopPrice.HasValue || newStop < _shortStopPrice.Value - trailingStep)
				{
					_shortStopPrice = newStop;
				}
			}
		}
	}

	private void UpdateLongState(decimal existingVolume, decimal addedVolume, decimal entryPrice)
	{
		var total = existingVolume + addedVolume;
		if (total <= 0m)
		{
			ResetLongState();
			return;
		}

		if (_longEntryPrice is null || existingVolume <= 0m)
		{
			_longEntryPrice = entryPrice;
		}
		else
		{
			_longEntryPrice = ((_longEntryPrice.Value * existingVolume) + entryPrice * addedVolume) / total;
		}

		var step = GetStepSize();

		if (StopLossSteps > 0)
		{
			_longStopPrice = _longEntryPrice.Value - StopLossSteps * step;
		}
		else if (TrailingStopSteps <= 0)
		{
			_longStopPrice = null;
		}

		_longTakePrice = TakeProfitSteps > 0 ? _longEntryPrice.Value + TakeProfitSteps * step : null;

		ResetShortState();
	}

	private void UpdateShortState(decimal existingVolume, decimal addedVolume, decimal entryPrice)
	{
		var total = existingVolume + addedVolume;
		if (total <= 0m)
		{
			ResetShortState();
			return;
		}

		if (_shortEntryPrice is null || existingVolume <= 0m)
		{
			_shortEntryPrice = entryPrice;
		}
		else
		{
			_shortEntryPrice = ((_shortEntryPrice.Value * existingVolume) + entryPrice * addedVolume) / total;
		}

		var step = GetStepSize();

		if (StopLossSteps > 0)
		{
			_shortStopPrice = _shortEntryPrice.Value + StopLossSteps * step;
		}
		else if (TrailingStopSteps <= 0)
		{
			_shortStopPrice = null;
		}

		_shortTakePrice = TakeProfitSteps > 0 ? _shortEntryPrice.Value - TakeProfitSteps * step : null;

		ResetLongState();
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private decimal GetStepSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}
}
