using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Zonal Trading strategy based on Awesome and Accelerator oscillators.
/// </summary>
public class ZonalTradingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _aoPrev1;
	private decimal _aoPrev2;
	private decimal _acPrev1;
	private decimal _acPrev2;
	private int _historyCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZonalTradingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_aoPrev1 = _aoPrev2 = _acPrev1 = _acPrev2 = 0m;
		_historyCount = 0;

		var ao = new AwesomeOscillator();
		var aoSma = new SimpleMovingAverage { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ao, (candle, aoValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				// Calculate AC = AO - SMA(AO, 5)
				var smaResult = aoSma.Process(aoValue, candle.OpenTime, true);
				if (!aoSma.IsFormed)
				{
					_aoPrev2 = _aoPrev1;
					_aoPrev1 = aoValue;
					return;
				}

				var smaValue = smaResult.GetValue<decimal>();
				var acValue = aoValue - smaValue;

				if (_historyCount < 2)
				{
					_aoPrev2 = _aoPrev1;
					_aoPrev1 = aoValue;
					_acPrev2 = _acPrev1;
					_acPrev1 = acValue;
					_historyCount++;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var buySignal = aoValue > _aoPrev1 && acValue > _acPrev1 &&
					(_acPrev1 < _acPrev2 || _aoPrev1 < _aoPrev2) &&
					aoValue > 0 && acValue > 0;

				var sellSignal = aoValue < _aoPrev1 && acValue < _acPrev1 &&
					(_acPrev1 > _acPrev2 || _aoPrev1 > _aoPrev2) &&
					aoValue < 0 && acValue < 0;

				if (buySignal && Position <= 0)
					BuyMarket();

				if (sellSignal && Position >= 0)
					SellMarket();

				// Exit conditions
				if (Position > 0 && aoValue < _aoPrev1 && acValue < _acPrev1)
					SellMarket();

				if (Position < 0 && aoValue > _aoPrev1 && acValue > _acPrev1)
					BuyMarket();

				_aoPrev2 = _aoPrev1;
				_aoPrev1 = aoValue;
				_acPrev2 = _acPrev1;
				_acPrev1 = acValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ao);
			DrawOwnTrades(area);
		}
	}
}
