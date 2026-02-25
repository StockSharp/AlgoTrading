using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class XkriHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _kriPeriod;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _smooth;
	private decimal _last;
	private decimal _prev;
	private decimal _prev2;
	private int _valueCount;

	public int KriPeriod { get => _kriPeriod.Value; set => _kriPeriod.Value = value; }
	public int SmoothPeriod { get => _smoothPeriod.Value; set => _smoothPeriod.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XkriHistogramStrategy()
	{
		_kriPeriod = Param(nameof(KriPeriod), 20)
			.SetDisplay("KRI Period", "Base moving average period", "Indicators");

		_smoothPeriod = Param(nameof(SmoothPeriod), 7)
			.SetDisplay("Smooth Period", "EMA smoothing period", "Indicators");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Protection");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
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
		_last = 0;
		_prev = 0;
		_prev2 = 0;
		_valueCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma = new SimpleMovingAverage { Length = KriPeriod };
		_smooth = new ExponentialMovingAverage { Length = SmoothPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ma, (candle, maValue) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (maValue == 0) return;

			var kri = 100m * (candle.ClosePrice - maValue) / maValue;
			var smoothResult = _smooth.Process(kri, candle.OpenTime, true);
			if (!smoothResult.IsFormed)
				return;

			var smooth = smoothResult.ToDecimal();

			_prev2 = _prev;
			_prev = _last;
			_last = smooth;
			_valueCount++;

			if (_valueCount < 3)
				return;

			var longSignal = _prev < _prev2 && _last > _prev && Position <= 0;
			var shortSignal = _prev > _prev2 && _last < _prev && Position >= 0;

			if (longSignal)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (shortSignal)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smooth);
			DrawOwnTrades(area);
		}
	}
}
