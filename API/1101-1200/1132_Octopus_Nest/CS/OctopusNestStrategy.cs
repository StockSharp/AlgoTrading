using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OctopusNestStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<DataType> _candleType;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OctopusNestStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 100).SetGreaterThanZero();
		_bbLength = Param(nameof(BbLength), 20).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var bb = new BollingerBands { Length = BbLength, Width = 2m };
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, bb, (candle, emaValue, bbValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!emaValue.IsFormed || !bbValue.IsFormed)
					return;

				var emaVal = emaValue.ToDecimal();
				var bbTyped = (BollingerBandsValue)bbValue;
				if (bbTyped.UpBand is not decimal bbUpper || bbTyped.LowBand is not decimal bbLower)
					return;

				var bbWidth = bbUpper - bbLower;
				var squeeze = bbWidth < candle.ClosePrice * 0.01m;

				if (squeeze || candle.OpenTime - lastSignal < cooldown)
					return;

				if (candle.ClosePrice > emaVal && candle.ClosePrice > bbUpper && Position <= 0)
				{
					BuyMarket();
					lastSignal = candle.OpenTime;
				}
				else if (candle.ClosePrice < emaVal && candle.ClosePrice < bbLower && Position >= 0)
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
			DrawIndicator(area, ema);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}
}
