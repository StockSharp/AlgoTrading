using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy based on previous range.
/// </summary>
public class CidomoV1Strategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _delta;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _noLoss;
	private readonly StrategyParam<decimal> _trailing;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _tradeTime;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _longLevel;
	private decimal _shortLevel;
	private DateTime _lastTradeDay;
	private decimal _entryPrice;
	private decimal _stopPrice;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	/// <summary>
	/// Number of candles used to calculate range.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Price offset added to breakout levels.
	/// </summary>
	public decimal Delta
	{
		get => _delta.Value;
		set => _delta.Value = value;
	}

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Move stop to entry after this profit (points).
	/// </summary>
	public decimal NoLoss
	{
		get => _noLoss.Value;
		set => _noLoss.Value = value;
	}

	/// <summary>
	/// Trailing distance in points.
	/// </summary>
	public decimal Trailing
	{
		get => _trailing.Value;
		set => _trailing.Value = value;
	}

	/// <summary>
	/// Trade only after specified time.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Time to calculate breakout levels.
	/// </summary>
	public TimeSpan TradeTime
	{
		get => _tradeTime.Value;
		set => _tradeTime.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CidomoV1Strategy()
	{
		_lookback = Param(nameof(Lookback), 32)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of candles to look back", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 10);

		_delta = Param(nameof(Delta), 0m)
			.SetDisplay("Delta", "Price offset added to breakout levels", "General");

		_stopLoss = Param(nameof(StopLoss), 60m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 70m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_noLoss = Param(nameof(NoLoss), 35m)
			.SetDisplay("Break-even", "Move stop to entry after profit", "Risk");

		_trailing = Param(nameof(Trailing), 5m)
			.SetDisplay("Trailing", "Trailing distance in points", "Risk");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Trade only after specified time", "General");

		_tradeTime = Param(nameof(TradeTime), new TimeSpan(9, 0, 0))
			.SetDisplay("Trade Time", "Time to calculate breakout", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for analysis", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_longLevel = _shortLevel = 0m;
		_lastTradeDay = default;
		_entryPrice = 0m;
		_stopPrice = 0m;
		base.OnReseted();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };

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

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.PriceStep ?? 1m;
		var time = candle.OpenTime.TimeOfDay;

		if ((!UseTimeFilter || time >= TradeTime) && _lastTradeDay != candle.OpenTime.Date)
		{
			_longLevel = highest + Delta * step;
			_shortLevel = lowest - Delta * step;
			_lastTradeDay = candle.OpenTime.Date;
			LogInfo($"Levels updated: high {_longLevel}, low {_shortLevel}");
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			if (candle.HighPrice >= _longLevel)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss * step;
			}
			else if (candle.LowPrice <= _shortLevel)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss * step;
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				return;
			}

			if (TakeProfit > 0 && candle.HighPrice >= _entryPrice + TakeProfit * step)
			{
				SellMarket(Position);
				return;
			}

			if (NoLoss > 0 && _stopPrice < _entryPrice && candle.HighPrice >= _entryPrice + NoLoss * step)
				_stopPrice = _entryPrice;

			if (Trailing > 0 && candle.HighPrice >= _entryPrice + Trailing * step)
			{
				var newStop = candle.ClosePrice - Trailing * step;
				if (newStop > _stopPrice)
					_stopPrice = newStop;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket(-Position);
				return;
			}

			if (TakeProfit > 0 && candle.LowPrice <= _entryPrice - TakeProfit * step)
			{
				BuyMarket(-Position);
				return;
			}

			if (NoLoss > 0 && _stopPrice > _entryPrice && candle.LowPrice <= _entryPrice - NoLoss * step)
				_stopPrice = _entryPrice;

			if (Trailing > 0 && candle.LowPrice <= _entryPrice - Trailing * step)
			{
				var newStop = candle.ClosePrice + Trailing * step;
				if (newStop < _stopPrice)
					_stopPrice = newStop;
			}
		}
	}
}

