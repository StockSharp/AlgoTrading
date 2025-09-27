using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mimics the ZigZag Climber expert advisor by instantly hedging with buy and sell market orders.
/// </summary>
public class ZigZagClimberStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private bool _ordersPlaced;
	private decimal _pipMultiplier;

	/// <summary>
	/// Candle type that triggers the entry sequence.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fixed trade volume for both market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader style pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader style pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ZigZagClimberStrategy"/>.
	/// </summary>
	public ZigZagClimberStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe that triggers the entry chain", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Fixed volume used for both hedged entries", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_stopLossPips = Param(nameof(StopLossPips), 99.9m)
		.SetGreaterThanZero()
		.SetDisplay("Stop-Loss (pips)", "Protective stop in MetaTrader pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 300m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Take-Profit (pips)", "Target in MetaTrader pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 300m, 10m);
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

		_ordersPlaced = false;
		_pipMultiplier = 1m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_pipMultiplier = CalculatePipMultiplier();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		StartProtection();

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

		if (_ordersPlaced)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		var stopPoints = CalculatePoints(StopLossPips);
		var takePoints = CalculatePoints(TakeProfitPips);

		var referencePrice = candle.ClosePrice;

		var currentPosition = Position;
		BuyMarket(volume);
		var resultingLongPosition = currentPosition + volume;

		if (takePoints > 0)
		SetTakeProfit(takePoints, referencePrice, resultingLongPosition);

		if (stopPoints > 0)
		SetStopLoss(stopPoints, referencePrice, resultingLongPosition);

		currentPosition = resultingLongPosition;
		SellMarket(volume);
		var resultingShortPosition = currentPosition - volume;

		if (takePoints > 0)
		SetTakeProfit(takePoints, referencePrice, resultingShortPosition);

		if (stopPoints > 0)
		SetStopLoss(stopPoints, referencePrice, resultingShortPosition);

		_ordersPlaced = true;
	}

	private decimal CalculatePipMultiplier()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		var decimals = Security?.Decimals ?? 0;

		if (priceStep <= 0m)
		return 1m;

		return decimals is 1 or 3 or 5 ? 10m : 1m;
	}

	private int CalculatePoints(decimal pips)
	{
		if (pips <= 0m)
		return 0;

		var points = pips * _pipMultiplier;
		if (points <= 0m)
		return 0;

		return (int)Math.Round(points, MidpointRounding.AwayFromZero);
	}
}

