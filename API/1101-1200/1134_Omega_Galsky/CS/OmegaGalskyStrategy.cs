using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OmegaGalskyStrategy : Strategy
{
	private readonly StrategyParam<int> _ema8Period;
	private readonly StrategyParam<int> _ema21Period;
	private readonly StrategyParam<int> _ema89Period;
	private readonly StrategyParam<DataType> _candleType;

	public int Ema8Period { get => _ema8Period.Value; set => _ema8Period.Value = value; }
	public int Ema21Period { get => _ema21Period.Value; set => _ema21Period.Value = value; }
	public int Ema89Period { get => _ema89Period.Value; set => _ema89Period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OmegaGalskyStrategy()
	{
		_ema8Period = Param(nameof(Ema8Period), 14).SetGreaterThanZero();
		_ema21Period = Param(nameof(Ema21Period), 40).SetGreaterThanZero();
		_ema89Period = Param(nameof(Ema89Period), 89).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema8 = new ExponentialMovingAverage { Length = Ema8Period };
		var ema21 = new ExponentialMovingAverage { Length = Ema21Period };
		var ema89 = new ExponentialMovingAverage { Length = Ema89Period };

		var prevE8 = 0m;
		var prevE21 = 0m;
		var init = false;
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema8, ema21, ema89, (candle, e8, e21, e89) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!ema8.IsFormed || !ema21.IsFormed || !ema89.IsFormed)
					return;

				if (!init)
				{
					prevE8 = e8;
					prevE21 = e21;
					init = true;
					return;
				}

				if (candle.OpenTime - lastSignal >= cooldown)
				{
					if (prevE8 <= prevE21 && e8 > e21 && candle.ClosePrice > e89 && Position <= 0)
					{
						BuyMarket();
						lastSignal = candle.OpenTime;
					}
					else if (prevE8 >= prevE21 && e8 < e21 && candle.ClosePrice < e89 && Position >= 0)
					{
						SellMarket();
						lastSignal = candle.OpenTime;
					}
				}

				prevE8 = e8;
				prevE21 = e21;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema8);
			DrawIndicator(area, ema21);
			DrawIndicator(area, ema89);
			DrawOwnTrades(area);
		}
	}
}
