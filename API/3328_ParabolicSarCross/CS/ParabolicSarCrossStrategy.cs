using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR Cross strategy: PSAR crossover.
/// Buys when close crosses above PSAR. Sells when close crosses below PSAR.
/// </summary>
public class ParabolicSarCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevSar;
	private decimal? _prevClose;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ParabolicSarCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSar = null;
		_prevClose = null;

		var sar = new ParabolicSar { Acceleration = 0.02m, AccelerationStep = 0.02m, AccelerationMax = 0.2m };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (_prevSar.HasValue && _prevClose.HasValue)
		{
			var crossUp = _prevClose.Value <= _prevSar.Value && close > sar;
			var crossDown = _prevClose.Value >= _prevSar.Value && close < sar;

			if (crossUp && Position <= 0)
				BuyMarket();
			else if (crossDown && Position >= 0)
				SellMarket();
		}

		_prevSar = sar;
		_prevClose = close;
	}
}
