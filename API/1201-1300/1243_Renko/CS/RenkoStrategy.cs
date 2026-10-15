using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Native Renko brick reversal strategy.
/// </summary>
public class RenkoStrategy : Strategy
{
	private readonly StrategyParam<decimal> _boxSize;
	private readonly StrategyParam<DataType> _priceSource;
	private int _previousDirection;

	public decimal BoxSize { get => _boxSize.Value; set => _boxSize.Value = value; }
	public DataType PriceSource { get => _priceSource.Value; set => _priceSource.Value = value; }
	public DataType CandleType => new Unit(BoxSize).Renko();

	public RenkoStrategy()
	{
		_boxSize = Param(nameof(BoxSize), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Box Size", "Renko brick size.", "Renko");
		_priceSource = Param(nameof(PriceSource), DataType.Level1)
			.SetDisplay("Price Source", "Level1 best bid or trade ticks used to build Renko bricks.", "Renko");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousDirection = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (PriceSource != DataType.Level1 && PriceSource != DataType.Ticks)
			throw new ArgumentOutOfRangeException(nameof(PriceSource));
		var subscription = new Subscription(CandleType, Security);
		subscription.MarketData.BuildMode = MarketDataBuildModes.Build;
		subscription.MarketData.BuildFrom = PriceSource;
		if (PriceSource == DataType.Level1)
			subscription.MarketData.BuildField = Level1Fields.BestBidPrice;
		SubscribeCandles(subscription)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentDirection = candle.ClosePrice > candle.OpenPrice
			? 1
			: candle.ClosePrice < candle.OpenPrice ? -1 : 0;

		if (currentDirection == 0)
			return;

		var signal = GetSignal(_previousDirection, currentDirection);

		if (signal > 0 && Position <= 0m)
			BuyMarket(Volume + Math.Abs(Position));
		else if (signal < 0 && Position >= 0m)
			SellMarket(Volume + Math.Abs(Position));

		_previousDirection = currentDirection;
	}

	internal static int GetSignal(int previousDirection, int currentDirection)
	{
		if (previousDirection < 0 && currentDirection > 0)
			return 1;

		if (previousDirection > 0 && currentDirection < 0)
			return -1;

		return 0;
	}
}
