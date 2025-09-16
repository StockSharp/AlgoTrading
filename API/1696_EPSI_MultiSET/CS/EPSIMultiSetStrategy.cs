using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy converted from the MQL4 expert e-PSI@MultiSET.
/// Opens a position when price moves a specified distance from the candle open.
/// </summary>
public class EPSIMultiSetStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minDistance;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _openHour;
	private readonly StrategyParam<int> _closeHour;

	private decimal _entryPrice;
	private bool _isLong;

	/// <summary>
	/// Distance from candle open in points to trigger a trade.
	/// </summary>
	public decimal MinDistance
	{
		get => _minDistance.Value;
		set => _minDistance.Value = value;
	}

	/// <summary>
	/// Take profit in points from entry price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in points from entry price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Hour when trading is allowed to start.
	/// </summary>
	public int OpenHour
	{
		get => _openHour.Value;
		set => _openHour.Value = value;
	}

	/// <summary>
	/// Hour when trading is stopped.
	/// </summary>
	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public EPSIMultiSetStrategy()
	{
		_minDistance = Param(nameof(MinDistance), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Min Distance", "Breakout distance in points", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target in points", "Risk Management");

		_stopLoss = Param(nameof(StopLoss), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Loss limit in points", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_openHour = Param(nameof(OpenHour), 2)
			.SetDisplay("Open Hour", "Start hour for trading", "Schedule");

		_closeHour = Param(nameof(CloseHour), 20)
			.SetDisplay("Close Hour", "End hour for trading", "Schedule");
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
		_entryPrice = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var localTime = candle.OpenTime.ToLocalTime();
		if (localTime.Hour < OpenHour || localTime.Hour >= CloseHour)
			return;

		if (Position == 0)
		{
			if (candle.HighPrice - candle.OpenPrice >= MinDistance * Security.PriceStep)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_isLong = true;
			}
			else if (candle.OpenPrice - candle.LowPrice >= MinDistance * Security.PriceStep)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_isLong = false;
			}
		}
		else
		{
			var volume = Math.Abs(Position);

			if (_isLong)
			{
				if (candle.LowPrice <= _entryPrice - StopLoss * Security.PriceStep ||
					candle.HighPrice >= _entryPrice + TakeProfit * Security.PriceStep)
					SellMarket(volume);
			}
			else
			{
				if (candle.HighPrice >= _entryPrice + StopLoss * Security.PriceStep ||
					candle.LowPrice <= _entryPrice - TakeProfit * Security.PriceStep)
					BuyMarket(volume);
			}
		}
	}
}
