namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy combining Moving Average, Parabolic SAR and ADX indicators.
/// Buys when price is above MA and SAR with +DI above -DI.
/// Sells when price is below MA and SAR with +DI below -DI.
/// Closes position when price crosses SAR.
/// </summary>
public class MaSarAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<DataType> _candleType;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaSarAdxStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Simple moving average period", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Average Directional Index period", "Indicators");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Initial acceleration factor", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum acceleration factor", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var sar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMax
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, adx, sar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, sar);

			var adxArea = CreateChartArea();
			if (adxArea != null)
				DrawIndicator(adxArea, adx);

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue adxValue, IIndicatorValue sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFinal || !sarValue.IsFinal || !adxValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var ma = maValue.ToDecimal();
		var sar = sarValue.ToDecimal();
		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		var dx = adxTyped.Dx;

		if (dx.Plus is not decimal diPlus || dx.Minus is not decimal diMinus)
			return;

		if (Position == 0)
		{
			if (price > ma && diPlus >= diMinus && price > sar)
				BuyMarket();
			else if (price < ma && diPlus <= diMinus && price < sar)
				SellMarket();
		}
		else if (Position > 0)
		{
			if (price < sar)
				ClosePosition();
		}
		else
		{
			if (price > sar)
				ClosePosition();
		}
	}
}
