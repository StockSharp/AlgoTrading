using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid based strategy converted from the original MQL4 VR---SETKAp2 expert.
/// </summary>
public class VrSetkaP2Strategy : Strategy
{
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<decimal> _lot;
	private readonly StrategyParam<decimal> _percent;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<int> _slippage;
	private readonly StrategyParam<int> _correlation;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _prevOpen;
	private decimal _prevClose;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public VrSetkaP2Strategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 300)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit in price steps", "General")
			.SetCanOptimize(true)
			.SetOptimize(100, 500, 100);

		_lot = Param(nameof(Lot), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot", "Order volume", "General");

		_percent = Param(nameof(Percent), 1.3m)
			.SetGreaterThanZero()
			.SetDisplay("Percent", "Threshold percentage", "General");

		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Use Martingale", "Increase volume after loss", "General");

		_slippage = Param(nameof(Slippage), 2)
			.SetGreaterOrEqual(0)
			.SetDisplay("Slippage", "Allowed slippage", "General");

		_correlation = Param(nameof(Correlation), 50)
			.SetDisplay("Correlation", "Offset for grid levels", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}

	/// <summary>
	/// Distance to the profit target in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Base volume for orders.
	/// </summary>
	public decimal Lot
	{
		get => _lot.Value;
		set => _lot.Value = value;
	}

	/// <summary>
	/// Percentage threshold derived from the daily range.
	/// </summary>
	public decimal Percent
	{
		get => _percent.Value;
		set => _percent.Value = value;
	}

	/// <summary>
	/// Enable simple martingale position sizing.
	/// </summary>
	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	/// <summary>
	/// Allowed order slippage.
	/// </summary>
	public int Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <summary>
	/// Offset used for grid levels.
	/// </summary>
	public int Correlation
	{
		get => _correlation.Value;
		set => _correlation.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_prevOpen = 0m;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var max = candle.HighPrice;
		var min = candle.LowPrice;
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		var x = close > min ? Math.Round(close * 100m / min - 100m, 2) : 0m;
		var y = close < max ? Math.Round(close * 100m / max - 100m, 2) : 0m;

		var step = Security?.PriceStep ?? 1m;
		var targetDistance = TakeProfit * step;

		if (Position <= 0 && -Percent <= y && _prevClose > _prevOpen)
		{
			var volume = Lot;

			if (UseMartingale && Position < 0)
				volume *= Math.Abs(Position) + 1;

			BuyMarket(volume);
			_entryPrice = close;
		}
		else if (Position >= 0 && Percent <= x && _prevClose < _prevOpen)
		{
			var volume = Lot;

			if (UseMartingale && Position > 0)
				volume *= Math.Abs(Position) + 1;

			SellMarket(volume);
			_entryPrice = close;
		}
		else if (Position > 0 && close >= _entryPrice + targetDistance)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && close <= _entryPrice - targetDistance)
		{
			BuyMarket(-Position);
		}

		_prevOpen = open;
		_prevClose = close;
	}
}
