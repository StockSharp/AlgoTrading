using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI-based strategy with EMA trend filter.
/// </summary>
public class ParabolicRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ParabolicRsiStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero();
		_emaLength = Param(nameof(EmaLength), 40).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = 14 };
		var slow = new ExponentialMovingAverage { Length = EmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var prevF = 0m;
		var prevS = 0m;
		var init = false;
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, rsi, (candle, f, s, r) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!fast.IsFormed || !slow.IsFormed || !rsi.IsFormed)
					return;

				if (!init)
				{
					prevF = f;
					prevS = s;
					init = true;
					return;
				}

				if (candle.OpenTime - lastSignal >= cooldown)
				{
					if (prevF <= prevS && f > s && r > 50 && Position <= 0)
					{
						BuyMarket();
						lastSignal = candle.OpenTime;
					}
					else if (prevF >= prevS && f < s && r < 50 && Position >= 0)
					{
						SellMarket();
						lastSignal = candle.OpenTime;
					}
				}

				prevF = f;
				prevS = s;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}
}
