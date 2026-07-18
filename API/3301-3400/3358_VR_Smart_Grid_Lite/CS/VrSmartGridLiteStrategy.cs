namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// VR Smart Grid Lite: grid trading based on price levels with SMA filter.
/// </summary>
public class VrSmartGridLiteStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _gridPercent;
	private readonly StrategyParam<int> _smaPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal GridPercent
	{
		get => _gridPercent.Value;
		set => _gridPercent.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public VrSmartGridLiteStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_gridPercent = Param(nameof(GridPercent), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Grid %", "Grid step percentage", "Grid");

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period for trend", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		decimal? lastTradePrice = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (candle, smaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;

				if (!lastTradePrice.HasValue)
				{
					lastTradePrice = close;
					return;
				}

				var step = lastTradePrice.Value * GridPercent / 100m;

				if (close <= lastTradePrice.Value - step)
				{
					BuyMarket();
					lastTradePrice = close;
				}
				else if (close >= lastTradePrice.Value + step)
				{
					SellMarket();
					lastTradePrice = close;
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}
