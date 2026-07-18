using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR trend-following strategy with EMA confirmation.
/// </summary>
public class ParabolicSarMultiTimeframeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarAcceleration;
	private readonly StrategyParam<decimal> _sarMaxAcceleration;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	public decimal SarAcceleration { get => _sarAcceleration.Value; set => _sarAcceleration.Value = value; }
	public decimal SarMaxAcceleration { get => _sarMaxAcceleration.Value; set => _sarMaxAcceleration.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ParabolicSarMultiTimeframeStrategy()
	{
		_sarAcceleration = Param(nameof(SarAcceleration), 0.02m)
			.SetDisplay("SAR Accel", "SAR acceleration factor", "Indicators");

		_sarMaxAcceleration = Param(nameof(SarMaxAcceleration), 0.2m)
			.SetDisplay("SAR Max", "SAR max acceleration", "Indicators");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sar = new ParabolicSar { Acceleration = SarAcceleration, AccelerationMax = SarMaxAcceleration };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sar, ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Buy when price is above both SAR and EMA
		if (price > sarValue && price > emaValue && Position <= 0)
			BuyMarket();
		// Sell when price is below both SAR and EMA
		else if (price < sarValue && price < emaValue && Position >= 0)
			SellMarket();

		// Exit on SAR flip
		if (Position > 0 && price < sarValue)
			SellMarket();
		else if (Position < 0 && price > sarValue)
			BuyMarket();
	}
}
