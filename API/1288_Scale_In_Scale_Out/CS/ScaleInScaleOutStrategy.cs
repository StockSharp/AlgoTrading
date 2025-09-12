using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ScaleInScaleOutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _buyScalingSize;
	private readonly StrategyParam<decimal> _takeProfitLevel;
	private readonly StrategyParam<decimal> _takeProfitSize;
	private readonly StrategyParam<decimal> _retainProfitPortion;
	private readonly StrategyParam<decimal> _minPositionValue;
	private readonly StrategyParam<decimal> _minBuyValue;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _deployableCash;
	private decimal _retainedCash;
	private decimal _totalCost;

	public decimal BuyScalingSize
	{
		get => _buyScalingSize.Value;
		set => _buyScalingSize.Value = value;
	}

	public decimal TakeProfitLevel
	{
		get => _takeProfitLevel.Value;
		set => _takeProfitLevel.Value = value;
	}

	public decimal TakeProfitSize
	{
		get => _takeProfitSize.Value;
		set => _takeProfitSize.Value = value;
	}

	public decimal RetainProfitPortion
	{
		get => _retainProfitPortion.Value;
		set => _retainProfitPortion.Value = value;
	}

	public decimal MinPositionValue
	{
		get => _minPositionValue.Value;
		set => _minPositionValue.Value = value;
	}

	public decimal MinBuyValue
	{
		get => _minBuyValue.Value;
		set => _minBuyValue.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ScaleInScaleOutStrategy()
	{
		_buyScalingSize = Param(nameof(BuyScalingSize), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Buy Scaling Size %", "Percentage of cash used for each scale-in", "General");

		_takeProfitLevel = Param(nameof(TakeProfitLevel), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Level %", "Profit percentage to start scaling out", "General");

		_takeProfitSize = Param(nameof(TakeProfitSize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Size %", "Portion of position to sell when taking profit", "General");

		_retainProfitPortion = Param(nameof(RetainProfitPortion), 50m)
			.SetDisplay("Retain Profit Portion %", "Fraction of profit kept outside trading cash", "General");

		_minPositionValue = Param(nameof(MinPositionValue), 200000m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Position Value", "Minimum position value before profit taking", "General");

		_minBuyValue = Param(nameof(MinBuyValue), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Buy Value", "Minimum cash value per buy", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_deployableCash = 1_000_000m;
		_retainedCash = 0m;
		_totalCost = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_deployableCash = 1_000_000m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var position = Position;

		if (position > 0)
		{
			var positionValue = position * price;
			if (positionValue >= MinPositionValue)
			{
				var avgPrice = _totalCost / position;
				var profitPercent = (price - avgPrice) / avgPrice * 100m;
				if (profitPercent >= TakeProfitLevel)
				{
					var sellQty = position * (TakeProfitSize / 100m);
					SellMarket(sellQty);

					var costPortion = avgPrice * sellQty;
					var saleProceeds = price * sellQty;
					var profit = saleProceeds - costPortion;

					_totalCost -= costPortion;

					var retain = profit * (RetainProfitPortion / 100m);
					_retainedCash += retain;
					_deployableCash += costPortion + (profit - retain);
				}
			}
		}

		if (_deployableCash > MinBuyValue)
		{
			var buyValue = Math.Max(_deployableCash * (BuyScalingSize / 100m), MinBuyValue);
			buyValue = Math.Min(buyValue, _deployableCash);

			var qty = buyValue / price;
			if (qty > 0)
			{
				BuyMarket(qty);
				_deployableCash -= buyValue;
				_totalCost += buyValue;
			}
		}
	}
}
