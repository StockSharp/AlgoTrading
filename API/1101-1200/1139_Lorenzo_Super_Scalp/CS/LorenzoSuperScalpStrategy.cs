using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class LorenzoSuperScalpStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<DataType> _candleType;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LorenzoSuperScalpStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero();
		_bbLength = Param(nameof(BbLength), 20).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var bb = new BollingerBands { Length = BbLength, Width = 2m };

		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, bb, (candle, rsiVal, bbVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!rsiVal.IsFormed || !bbVal.IsFormed)
					return;

				var r = rsiVal.ToDecimal();

				var bbTyped = (BollingerBandsValue)bbVal;
				if (bbTyped.UpBand is not decimal upper || bbTyped.LowBand is not decimal lower)
					return;

				if (candle.OpenTime - lastSignal < cooldown)
					return;

				if (r < 45m && candle.ClosePrice <= lower && Position <= 0)
				{
					BuyMarket();
					lastSignal = candle.OpenTime;
				}
				else if (r > 55m && candle.ClosePrice >= upper && Position >= 0)
				{
					SellMarket();
					lastSignal = candle.OpenTime;
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}
}
