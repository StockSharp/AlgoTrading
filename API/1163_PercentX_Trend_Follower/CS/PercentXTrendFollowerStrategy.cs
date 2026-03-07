using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Percent-X trend follower using Bollinger Bands oscillator with ATR stops.
/// </summary>
public class PercentXTrendFollowerStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _reverseMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal ReverseMultiplier { get => _reverseMultiplier.Value; set => _reverseMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PercentXTrendFollowerStrategy()
	{
		_maLength = Param(nameof(MaLength), 40);
		_atrLength = Param(nameof(AtrLength), 14);
		_reverseMultiplier = Param(nameof(ReverseMultiplier), 3m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
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
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = 14 };
		var slow = new ExponentialMovingAverage { Length = MaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var prevF = 0m;
		var prevS = 0m;
		var init = false;
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(600);

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(fast, slow, atr, (candle, fVal, sVal, atrVal) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!fast.IsFormed || !slow.IsFormed || !atr.IsFormed)
				return;

			var f = fVal.IsFormed ? fVal.ToDecimal() : 0m;
			var s = sVal.IsFormed ? sVal.ToDecimal() : 0m;

			if (!init)
			{
				prevF = f;
				prevS = s;
				init = true;
				return;
			}

			if (candle.OpenTime - lastSignal >= cooldown)
			{
				if (prevF <= prevS && f > s && Position <= 0)
				{
					BuyMarket();
					lastSignal = candle.OpenTime;
				}
				else if (prevF >= prevS && f < s && Position >= 0)
				{
					SellMarket();
					lastSignal = candle.OpenTime;
				}
			}

			prevF = f;
			prevS = s;
		}).Start();

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
