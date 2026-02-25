using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following breakout strategy based on Parabolic SAR reversals.
/// Opens trades after negative reversals and uses percentage-based SL/TP.
/// </summary>
public class BreakoutBarsTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _negatives;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private ParabolicSar _parabolic;
	private int _lastTrend; // -1, 0, 1
	private int _negativeCounter;

	public int Negatives
	{
		get => _negatives.Value;
		set => _negatives.Value = value;
	}

	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BreakoutBarsTrendStrategy()
	{
		_negatives = Param(nameof(Negatives), 1)
			.SetNotNegative()
			.SetDisplay("Negative Signals", "Negative reversals before entry", "General");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_lastTrend = 0;
		_negativeCounter = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_parabolic = new ParabolicSar();

		var passthrough = new SimpleMovingAverage { Length = 1 };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(passthrough, (candle, _) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var sarResult = _parabolic.Process(candle);
				if (!sarResult.IsFormed)
					return;

				var sarValue = sarResult.ToDecimal();
				var trend = sarValue < candle.ClosePrice ? 1 : -1;

				if (_lastTrend != 0 && _lastTrend != trend)
				{
					// Reversal detected
					if (trend == 1 && Position < 0)
						BuyMarket();
					else if (trend == -1 && Position > 0)
						SellMarket();

					_negativeCounter++;

					if (_negativeCounter > Negatives)
					{
						if (trend == 1 && Position <= 0)
						{
							BuyMarket();
						}
						else if (trend == -1 && Position >= 0)
						{
							SellMarket();
						}
						_negativeCounter = 0;
					}
				}

				_lastTrend = trend;
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}
