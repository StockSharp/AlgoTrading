using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class XkriHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _kriPeriod;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _ma;
	private ExponentialMovingAverage _smooth;

	private decimal _last;
	private decimal _prev;
	private decimal _prev2;
	private int _valueCount;

	public int KriPeriod { get => _kriPeriod.Value; set => _kriPeriod.Value = value; }
	public int SmoothPeriod { get => _smoothPeriod.Value; set => _smoothPeriod.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XkriHistogramStrategy()
	{
		_kriPeriod = Param(nameof(KriPeriod), 20)
			.SetDisplay("KRI Period", "Base moving average period", "Indicators");

		_smoothPeriod = Param(nameof(SmoothPeriod), 7)
			.SetDisplay("Smooth Period", "EMA smoothing period", "Indicators");

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetDisplay("Take Profit", "Take profit in points", "Protection");

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetDisplay("Stop Loss", "Stop loss in points", "Protection");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4)))
			.SetDisplay("Candle Type", "Time frame for candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new SimpleMovingAverage { Length = KriPeriod };
		_smooth = new ExponentialMovingAverage { Length = SmoothPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ma, ProcessCandle).Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Point),
			new Unit(StopLoss, UnitTypes.Point));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smooth);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ma.IsFormed)
			return;

		var kri = 100m * (candle.ClosePrice - maValue) / maValue;
		var smooth = _smooth.Process(kri, candle.OpenTime, true).ToDecimal();

		if (!_smooth.IsFormed)
			return;

		_prev2 = _prev;
		_prev = _last;
		_last = smooth;
		_valueCount++;

		if (_valueCount < 3)
			return;

		var longSignal = _prev < _prev2 && _last > _prev && Position <= 0;
		var shortSignal = _prev > _prev2 && _last < _prev && Position >= 0;

		if (longSignal)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal)
			SellMarket(Volume + Math.Abs(Position));
	}
}
