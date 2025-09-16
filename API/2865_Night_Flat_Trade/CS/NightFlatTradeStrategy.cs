using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Night session flat trading strategy that enters near range extremes.
/// </summary>
public class NightFlatTradeStrategy : Strategy
{
	private const int RangeLength = 3;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _diffMinPips;
	private readonly StrategyParam<decimal> _diffMaxPips;
	private readonly StrategyParam<int> _openHour;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public decimal DiffMinPips
	{
		get => _diffMinPips.Value;
		set => _diffMinPips.Value = value;
	}

	public decimal DiffMaxPips
	{
		get => _diffMaxPips.Value;
		set => _diffMaxPips.Value = value;
	}

	public int OpenHour
	{
		get => _openHour.Value;
		set => _openHour.Value = value;
	}

	public NightFlatTradeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for the setup", "General");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetRange(0m, 500m)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
			.SetRange(0m, 200m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetRange(0m, 200m)
			.SetDisplay("Trailing Step (pips)", "Extra advance required to shift the trailing stop", "Risk");

		_diffMinPips = Param(nameof(DiffMinPips), 18m)
			.SetGreaterThanZero()
			.SetDisplay("Min Range (pips)", "Minimum three-candle range in pips", "Setup");

		_diffMaxPips = Param(nameof(DiffMaxPips), 28m)
			.SetGreaterThanZero()
			.SetDisplay("Max Range (pips)", "Maximum three-candle range in pips", "Setup");

		_openHour = Param(nameof(OpenHour), 0)
			.SetRange(0, 23)
			.SetDisplay("Open Hour", "Hour (exchange time) when entries become active", "Schedule");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = null!;
		_lowest = null!;
		_pipSize = 0m;
		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = RangeLength };
		_lowest = new Lowest { Length = RangeLength };

		var priceStep = Security?.PriceStep ?? 0m;
		var decimals = Security?.Decimals;

		if (priceStep <= 0m)
			priceStep = 0.0001m;

		_pipSize = priceStep;

		if (decimals.HasValue && (decimals.Value == 3 || decimals.Value == 5))
			_pipSize *= 10m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage active trades before scanning for new setups.
		HandleExistingPosition(candle);

		if (Position != 0m)
			return;

		if (_highest == null || _lowest == null)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hour = candle.OpenTime.Hour;
		if (hour < OpenHour || hour > OpenHour + 1)
			return;

		var diff = highestValue - lowestValue;
		if (diff <= 0m)
			return;

		var minRange = ToPrice(DiffMinPips);
		var maxRange = ToPrice(DiffMaxPips);

		if (diff <= minRange || diff >= maxRange)
			return;

		var quarter = diff / 4m;
		var closePrice = candle.ClosePrice;

		if (closePrice > lowestValue && closePrice <= lowestValue + quarter)
		{
			BuyMarket();
			_entryPrice = closePrice;
			_stopPrice = lowestValue - diff / 3m;
			_takeProfitPrice = TakeProfitPips > 0m ? closePrice + ToPrice(TakeProfitPips) : null;
			return;
		}

		if (closePrice < highestValue && closePrice >= highestValue - quarter)
		{
			SellMarket();
			_entryPrice = closePrice;
			_stopPrice = highestValue + diff / 3m;
			_takeProfitPrice = TakeProfitPips > 0m ? closePrice - ToPrice(TakeProfitPips) : null;
		}
	}

	private void HandleExistingPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			UpdateTrailingForLong(candle);

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}
		else if (Position < 0m)
		{
			UpdateTrailingForShort(candle);

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || _stopPrice == null)
			return;

		var trailingDistance = ToPrice(TrailingStopPips);
		var stepDistance = ToPrice(TrailingStepPips);

		var advance = candle.HighPrice - _entryPrice;
		if (advance < trailingDistance + stepDistance)
			return;

		var newStop = candle.HighPrice - trailingDistance;

		if (newStop <= _stopPrice.Value || newStop - _stopPrice.Value < stepDistance)
			return;

		// Raise the stop only after price travels an additional step distance.
		_stopPrice = newStop;
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || _stopPrice == null)
			return;

		var trailingDistance = ToPrice(TrailingStopPips);
		var stepDistance = ToPrice(TrailingStepPips);

		var advance = _entryPrice - candle.LowPrice;
		if (advance < trailingDistance + stepDistance)
			return;

		var newStop = candle.LowPrice + trailingDistance;

		if (newStop >= _stopPrice.Value || _stopPrice.Value - newStop < stepDistance)
			return;

		// Lower the stop only after price moves the additional step distance in favor of the trade.
		_stopPrice = newStop;
	}

	private decimal ToPrice(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var pip = _pipSize > 0m ? _pipSize : 0.0001m;
		return pips * pip;
	}

	private void ResetTradeState()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
	}
}
