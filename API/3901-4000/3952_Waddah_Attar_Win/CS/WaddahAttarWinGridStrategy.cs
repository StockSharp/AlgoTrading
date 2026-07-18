using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy converted from the "Waddah Attar Win" MetaTrader 4 expert advisor.
/// Places paired orders around the market, pyramids positions with an optional volume increment,
/// and closes the entire exposure once the floating profit target is achieved.
/// </summary>
public class WaddahAttarWinGridStrategy : Strategy
{
	private readonly StrategyParam<int> _stepPoints;
	private readonly StrategyParam<decimal> _firstVolume;
	private readonly StrategyParam<decimal> _incrementVolume;
	private readonly StrategyParam<decimal> _minProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastBuyGridPrice;
	private decimal _lastSellGridPrice;
	private decimal _currentBuyVolume;
	private decimal _currentSellVolume;
	private decimal _referenceBalance;
	private bool _gridActive;

	/// <summary>
	/// Distance in price points between consecutive grid levels.
	/// </summary>
	public int StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Volume for the very first pair of orders.
	/// </summary>
	public decimal FirstVolume
	{
		get => _firstVolume.Value;
		set => _firstVolume.Value = value;
	}

	/// <summary>
	/// Volume increment applied to each newly stacked order.
	/// </summary>
	public decimal IncrementVolume
	{
		get => _incrementVolume.Value;
		set => _incrementVolume.Value = value;
	}

	/// <summary>
	/// Floating profit target in account currency that closes all positions.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Candle type for price data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public WaddahAttarWinGridStrategy()
	{
		_stepPoints = Param(nameof(StepPoints), 1500)
			.SetGreaterThanZero()
			.SetDisplay("Step (Points)", "Distance between grid levels in points", "Grid")
			.SetOptimize(20, 400, 10);

		_firstVolume = Param(nameof(FirstVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("First Volume", "Volume for the initial orders", "Trading");

		_incrementVolume = Param(nameof(IncrementVolume), 0m)
			.SetDisplay("Increment Volume", "Additional volume added when stacking new orders", "Trading");

		_minProfit = Param(nameof(MinProfit), 450m)
			.SetNotNegative()
			.SetDisplay("Min Profit", "Floating profit target in account currency", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for price data", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBuyGridPrice = 0m;
		_lastSellGridPrice = 0m;
		_currentBuyVolume = 0m;
		_currentSellVolume = 0m;
		_referenceBalance = 0m;
		_gridActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_referenceBalance = Portfolio?.CurrentValue ?? 0m;

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

		var priceStep = Security?.PriceStep ?? 0.01m;
		if (priceStep <= 0m)
			priceStep = 0.01m;

		var stepOffset = StepPoints * priceStep;
		if (stepOffset <= 0m)
			return;

		var price = candle.ClosePrice;

		// Check profit target
		var floatingProfit = (Portfolio?.CurrentValue ?? 0m) - _referenceBalance;
		if (MinProfit > 0m && floatingProfit >= MinProfit && _gridActive)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));

			_referenceBalance = Portfolio?.CurrentValue ?? _referenceBalance;
			_gridActive = false;
			_lastBuyGridPrice = 0m;
			_lastSellGridPrice = 0m;
			_currentBuyVolume = 0m;
			_currentSellVolume = 0m;
			return;
		}

		// Initialize grid on first candle
		if (!_gridActive)
		{
			_lastBuyGridPrice = price;
			_lastSellGridPrice = price;
			_currentBuyVolume = FirstVolume;
			_currentSellVolume = FirstVolume;
			_gridActive = true;
			_referenceBalance = Portfolio?.CurrentValue ?? _referenceBalance;
			return;
		}

		// Check if price dropped enough to trigger a buy grid level
		if (price <= _lastBuyGridPrice - stepOffset)
		{
			BuyMarket(_currentBuyVolume);
			_lastBuyGridPrice = price;
			_currentBuyVolume += IncrementVolume;
		}

		// Check if price rose enough to trigger a sell grid level
		if (price >= _lastSellGridPrice + stepOffset)
		{
			SellMarket(_currentSellVolume);
			_lastSellGridPrice = price;
			_currentSellVolume += IncrementVolume;
		}
	}
}
