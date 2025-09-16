using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy that reacts to the distance from the daily open.
/// Converted from the MetaTrader Daily BreakPoint expert advisor.
/// </summary>
public class DailyBreakPointStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _closeBySignal;
	private readonly StrategyParam<decimal> _breakPointPips;
	private readonly StrategyParam<decimal> _lastBarSizeMinPips;
	private readonly StrategyParam<decimal> _lastBarSizeMaxPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _currentDayOpen;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _pipSize;

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Reverse the position when the opposite signal appears.
	/// </summary>
	public bool CloseBySignal
	{
		get => _closeBySignal.Value;
		set => _closeBySignal.Value = value;
	}

	/// <summary>
	/// Break distance from the daily open expressed in pips.
	/// </summary>
	public decimal BreakPointPips
	{
		get => _breakPointPips.Value;
		set => _breakPointPips.Value = value;
	}

	/// <summary>
	/// Minimum size of the previous bar body in pips.
	/// </summary>
	public decimal LastBarSizeMinPips
	{
		get => _lastBarSizeMinPips.Value;
		set => _lastBarSizeMinPips.Value = value;
	}

	/// <summary>
	/// Maximum size of the previous bar body in pips.
	/// </summary>
	public decimal LastBarSizeMaxPips
	{
		get => _lastBarSizeMaxPips.Value;
		set => _lastBarSizeMaxPips.Value = value;
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
	/// Trailing stop step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Fixed stop loss in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Fixed take profit in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Intraday candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DailyBreakPointStrategy"/> class.
	/// </summary>
	public DailyBreakPointStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Default order volume", "General");

		_closeBySignal = Param(nameof(CloseBySignal), true)
		.SetDisplay("Close By Signal", "Reverse existing position on opposite signal", "General");

		_breakPointPips = Param(nameof(BreakPointPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Break Point (pips)", "Distance from the daily open", "Signals");

		_lastBarSizeMinPips = Param(nameof(LastBarSizeMinPips), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Last Bar Min (pips)", "Minimum body size of the previous bar", "Signals");

		_lastBarSizeMaxPips = Param(nameof(LastBarSizeMaxPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Last Bar Max (pips)", "Maximum body size of the previous bar", "Signals");

		_trailingStopPips = Param(nameof(TrailingStopPips), 2m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 2m)
		.SetDisplay("Trailing Step (pips)", "Minimum move before trailing", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
		.SetDisplay("Stop Loss (pips)", "Fixed stop loss distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 30m)
		.SetDisplay("Take Profit (pips)", "Fixed take profit distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Intraday candle series", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentDayOpen = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		_pipSize = CalculatePipSize();

		var intradaySubscription = SubscribeCandles(CandleType);
		intradaySubscription.Bind(ProcessCandle).Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription.Bind(ProcessDailyCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, intradaySubscription);
			DrawOwnTrades(area);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		if (step <= 0m)
		step = 0.0001m;

		var decimals = Security?.Decimals;
		if (decimals == 3 || decimals == 5)
		return step * 10m;

		return step;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step is null || step.Value <= 0m)
		return price;

		var value = price / step.Value;
		var rounded = Math.Round(value, 0, MidpointRounding.AwayFromZero);
		return rounded * step.Value;
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State == CandleStates.Finished || candle.State == CandleStates.Active)
		_currentDayOpen = candle.OpenPrice;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		Volume = OrderVolume;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_pipSize <= 0m)
		_pipSize = CalculatePipSize();

		var dayOpen = _currentDayOpen;
		if (dayOpen is null)
		return;

		var breakOffset = BreakPointPips * _pipSize;
		var minBody = LastBarSizeMinPips * _pipSize;
		var maxBody = LastBarSizeMaxPips * _pipSize;
		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var stopLossOffset = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
		var takeProfitOffset = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;

		UpdateTrailing(candle, trailingStop, trailingStep);
		HandleRiskExits(candle);

		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var minPrice = Math.Min(candle.OpenPrice, candle.ClosePrice);
		var maxPrice = Math.Max(candle.OpenPrice, candle.ClosePrice);

		var breakBuy = dayOpen.Value + breakOffset;
		var breakSell = dayOpen.Value - breakOffset;

		var bullishBody = candle.ClosePrice > candle.OpenPrice;
		var bearishBody = candle.ClosePrice < candle.OpenPrice;

		var bullishSignal = bullishBody && breakOffset > 0m &&
		candle.ClosePrice - dayOpen.Value >= breakOffset &&
		bodySize >= minBody &&
		(maxBody <= 0m || bodySize <= maxBody) &&
		breakBuy >= minPrice &&
		breakBuy <= maxPrice;

		var bearishSignal = bearishBody && breakOffset > 0m &&
		dayOpen.Value - candle.ClosePrice >= breakOffset &&
		bodySize >= minBody &&
		(maxBody <= 0m || bodySize <= maxBody) &&
		breakSell <= maxPrice &&
		breakSell >= minPrice;

		if (bullishSignal)
		{
			ExecuteBullishSignal(candle.ClosePrice, stopLossOffset, takeProfitOffset);
		}
		else if (bearishSignal)
		{
			ExecuteBearishSignal(candle.ClosePrice, stopLossOffset, takeProfitOffset);
		}
	}

	private void UpdateTrailing(ICandleMessage candle, decimal trailingStop, decimal trailingStep)
	{
		if (trailingStop <= 0m)
		return;

		if (Position > 0 && _longEntryPrice.HasValue)
		{
			var profit = candle.ClosePrice - _longEntryPrice.Value;
			if (profit > trailingStop + trailingStep)
			{
				var threshold = candle.ClosePrice - (trailingStop + trailingStep);
				if (!_longStopPrice.HasValue || _longStopPrice.Value < threshold)
				_longStopPrice = NormalizePrice(candle.ClosePrice - trailingStop);
			}
		}
		else if (Position < 0 && _shortEntryPrice.HasValue)
		{
			var profit = _shortEntryPrice.Value - candle.ClosePrice;
			if (profit > trailingStop + trailingStep)
			{
				var threshold = candle.ClosePrice + (trailingStop + trailingStep);
				if (!_shortStopPrice.HasValue || _shortStopPrice.Value > threshold || _shortStopPrice.Value == 0m)
				_shortStopPrice = NormalizePrice(candle.ClosePrice + trailingStop);
			}
		}
	}

	private void HandleRiskExits(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var volume = Math.Abs(Position);
			if (volume > 0m && _longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(volume);
				ResetLongState();
				return;
			}

			if (volume > 0m && _longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(volume);
				ResetLongState();
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);
			if (volume > 0m && _shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(volume);
				ResetShortState();
				return;
			}

			if (volume > 0m && _shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(volume);
				ResetShortState();
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}
	}

	private void ExecuteBullishSignal(decimal entryPrice, decimal stopLossOffset, decimal takeProfitOffset)
	{
		if (CloseBySignal)
		{
			if (Position > 0)
			{
				var volume = Math.Abs(Position);
				SellMarket(volume);
			}

			ResetLongState();

			SellMarket(OrderVolume);

			_shortEntryPrice = entryPrice;
			_shortStopPrice = stopLossOffset > 0m ? NormalizePrice(entryPrice + stopLossOffset) : null;
			_shortTakePrice = takeProfitOffset > 0m ? NormalizePrice(entryPrice - takeProfitOffset) : null;
		}
		else
		{
			BuyMarket(OrderVolume);

			_longEntryPrice = entryPrice;
			_longStopPrice = stopLossOffset > 0m ? NormalizePrice(entryPrice - stopLossOffset) : null;
			_longTakePrice = takeProfitOffset > 0m ? NormalizePrice(entryPrice + takeProfitOffset) : null;
			ResetShortState();
		}
	}

	private void ExecuteBearishSignal(decimal entryPrice, decimal stopLossOffset, decimal takeProfitOffset)
	{
		if (CloseBySignal)
		{
			if (Position < 0)
			{
				var volume = Math.Abs(Position);
				BuyMarket(volume);
			}

			ResetShortState();

			BuyMarket(OrderVolume);

			_longEntryPrice = entryPrice;
			_longStopPrice = stopLossOffset > 0m ? NormalizePrice(entryPrice - stopLossOffset) : null;
			_longTakePrice = takeProfitOffset > 0m ? NormalizePrice(entryPrice + takeProfitOffset) : null;
		}
		else
		{
			SellMarket(OrderVolume);

			_shortEntryPrice = entryPrice;
			_shortStopPrice = stopLossOffset > 0m ? NormalizePrice(entryPrice + stopLossOffset) : null;
			_shortTakePrice = takeProfitOffset > 0m ? NormalizePrice(entryPrice - takeProfitOffset) : null;
			ResetLongState();
		}
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
}
