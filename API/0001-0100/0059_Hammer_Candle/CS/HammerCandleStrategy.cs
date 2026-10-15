using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hammer Candle strategy.
/// Buys after a hammer: a candle whose lower shadow is at least twice its body and whose upper shadow is under half of it.
/// The stop is the hammer's low and the target lies RewardRiskRatio times that risk above the entry close.
/// </summary>
public class HammerCandleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _rewardRiskRatio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _targetPrice;

	/// <summary>
	/// Distance of the target in multiples of the distance to the stop.
	/// </summary>
	public decimal RewardRiskRatio
	{
		get => _rewardRiskRatio.Value;
		set => _rewardRiskRatio.Value = value;
	}

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HammerCandleStrategy()
	{
		_rewardRiskRatio = Param(nameof(RewardRiskRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Reward/Risk", "Target distance in multiples of the stop distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_stopPrice = default;
		_targetPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close <= _stopPrice || close >= _targetPrice)
				SellMarket(Position);

			return;
		}

		if (Position != 0)
			return;

		var body = Math.Abs(candle.OpenPrice - close);
		var lowerShadow = Math.Min(candle.OpenPrice, close) - candle.LowPrice;
		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, close);

		var isHammer = body > 0 && lowerShadow >= body * 2m && upperShadow < body * 0.5m;

		if (!isHammer || close <= candle.LowPrice)
			return;

		BuyMarket(Volume);
		_stopPrice = candle.LowPrice;
		_targetPrice = close + RewardRiskRatio * (close - candle.LowPrice);
	}
}
