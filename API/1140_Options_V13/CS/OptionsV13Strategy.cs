using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OptionsV13Strategy : Strategy
{
	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	public int EmaShortLength { get => _emaShortLength.Value; set => _emaShortLength.Value = value; }
	public int EmaLongLength { get => _emaLongLength.Value; set => _emaLongLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OptionsV13Strategy()
	{
		_emaShortLength = Param(nameof(EmaShortLength), 14).SetGreaterThanZero();
		_emaLongLength = Param(nameof(EmaLongLength), 40).SetGreaterThanZero();
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaShort = new ExponentialMovingAverage { Length = EmaShortLength };
		var emaLong = new ExponentialMovingAverage { Length = EmaLongLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var prevF = 0m;
		var prevS = 0m;
		var init = false;
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaShort, emaLong, rsi, (candle, f, s, r) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!emaShort.IsFormed || !emaLong.IsFormed || !rsi.IsFormed)
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
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaLong);
			DrawOwnTrades(area);
		}
	}
}
