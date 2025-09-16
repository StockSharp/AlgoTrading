using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R extreme cross strategy with optional trailing and time filters.
/// </summary>
public class ForexFraus4ForM1sStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<bool> _useProfitTrailing;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _trailingStep;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private bool _wasOversold;
	private bool _wasOverbought;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _entryPrice;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Level above which a buy signal is activated after oversold.
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Level below which a sell signal is activated after overbought.
	/// </summary>
	public decimal SellThreshold
	{
		get => _sellThreshold.Value;
		set => _sellThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trail only when position is in profit.
	/// </summary>
	public bool UseProfitTrailing
	{
		get => _useProfitTrailing.Value;
		set => _useProfitTrailing.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Minimal shift required to update trailing stop.
	/// </summary>
	public int TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Enable trading only during specified hours.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading stop hour.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="ForexFraus4ForM1sStrategy"/>.
	/// </summary>
	public ForexFraus4ForM1sStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 360)
						 .SetGreaterThanZero()
						 .SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators");

		_buyThreshold = Param(nameof(BuyThreshold), -99.9m)
							.SetDisplay("Buy Threshold", "Level crossing up triggers buy", "Trading");

		_sellThreshold = Param(nameof(SellThreshold), -0.1m)
							 .SetDisplay("Sell Threshold", "Level crossing down triggers sell", "Trading");

		_stopLoss = Param(nameof(StopLoss), 0).SetDisplay("Stop Loss", "Loss distance in steps", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0).SetDisplay("Take Profit", "Profit distance in steps", "Risk");

		_useProfitTrailing =
			Param(nameof(UseProfitTrailing), true).SetDisplay("Profit Trailing", "Trail only in profit", "Trailing");

		_trailingStop =
			Param(nameof(TrailingStop), 30).SetDisplay("Trailing Stop", "Trailing distance in steps", "Trailing");

		_trailingStep = Param(nameof(TrailingStep), 1).SetDisplay("Trailing Step", "Minimal trailing step", "Trailing");

		_useTimeFilter =
			Param(nameof(UseTimeFilter), false).SetDisplay("Use Time Filter", "Enable trading hours", "Time");

		_startHour = Param(nameof(StartHour), 7).SetRange(0, 23).SetDisplay("Start Hour", "Trading start hour", "Time");

		_stopHour = Param(nameof(StopHour), 17).SetRange(0, 23).SetDisplay("Stop Hour", "Trading stop hour", "Time");

		_volume = Param(nameof(Volume), 0.01m).SetGreaterThanZero().SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");
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
		Indicators.Clear();
		_wasOversold = false;
		_wasOverbought = false;
		_stopPrice = null;
		_takePrice = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wpr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security?.PriceStep ?? 1m;

		if (Position > 0)
		{
			if (!UseProfitTrailing || candle.ClosePrice - _entryPrice > TrailingStop * step)
			{
				var newStop = candle.ClosePrice - TrailingStop * step;
				if (_stopPrice == null || newStop > _stopPrice + TrailingStep * step)
					_stopPrice = newStop;
			}

			if (_stopPrice != null && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				ResetOrders();
			}
			else if (_takePrice != null && candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				ResetOrders();
			}
		}
		else if (Position < 0)
		{
			if (!UseProfitTrailing || _entryPrice - candle.ClosePrice > TrailingStop * step)
			{
				var newStop = candle.ClosePrice + TrailingStop * step;
				if (_stopPrice == null || newStop < _stopPrice - TrailingStep * step)
					_stopPrice = newStop;
			}

			if (_stopPrice != null && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(-Position);
				ResetOrders();
			}
			else if (_takePrice != null && candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
				ResetOrders();
			}
		}

		if (wprValue < BuyThreshold)
		{
			_wasOversold = true;
		}
		else if (wprValue > BuyThreshold && _wasOversold)
		{
			_wasOversold = false;
			if (IsAllowedTime(candle.OpenTime))
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				_entryPrice = candle.ClosePrice;
				BuyMarket(volume);

				_stopPrice = StopLoss > 0 ? candle.ClosePrice - StopLoss * step : null;
				_takePrice = TakeProfit > 0 ? candle.ClosePrice + TakeProfit * step : null;
			}
		}

		if (wprValue > SellThreshold)
		{
			_wasOverbought = true;
		}
		else if (wprValue < SellThreshold && _wasOverbought)
		{
			_wasOverbought = false;
			if (IsAllowedTime(candle.OpenTime))
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				_entryPrice = candle.ClosePrice;
				SellMarket(volume);

				_stopPrice = StopLoss > 0 ? candle.ClosePrice + StopLoss * step : null;
				_takePrice = TakeProfit > 0 ? candle.ClosePrice - TakeProfit * step : null;
			}
		}
	}

	private bool IsAllowedTime(DateTimeOffset time)
	{
		if (!UseTimeFilter)
			return true;

		var hour = time.Hour;
		return StartHour < StopHour ? hour >= StartHour && hour < StopHour : hour >= StartHour || hour < StopHour;
	}

	private void ResetOrders()
	{
		_stopPrice = null;
		_takePrice = null;
	}
}
