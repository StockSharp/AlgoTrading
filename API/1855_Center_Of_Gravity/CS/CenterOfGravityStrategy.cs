namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Center of Gravity Strategy.
/// Uses product of SMA and WMA compared to a smoothed average.
/// Opens long when the center line crosses above the smoothed line and short on opposite cross.
/// </summary>
public class CenterOfGravityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _smoothPeriod;

	private SimpleMovingAverage _sma;
	private WeightedMovingAverage _wma;
	private SimpleMovingAverage _signal;

	private decimal? _prevColor;

	public CenterOfGravityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculation", "General");

		_period = Param(nameof(Period), 10)
			.SetDisplay("Period", "Center of Gravity averaging period", "Indicators");

		_smoothPeriod = Param(nameof(SmoothPeriod), 3)
			.SetDisplay("Smooth Period", "Signal line smoothing period", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public int SmoothPeriod
	{
		get => _smoothPeriod.Value;
		set => _smoothPeriod.Value = value;
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_prevColor = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage { Length = Period };
		_wma = new WeightedMovingAverage { Length = Period };
		_signal = new SimpleMovingAverage { Length = SmoothPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_sma, _wma, Process).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _signal);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal smaValue, decimal wmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed || !_wma.IsFormed)
			return;

		var center = smaValue * wmaValue;
		var signalVal = _signal.Process(center, candle.CloseTime, true);

		if (!signalVal.IsFinal)
			return;

		var signal = signalVal.ToDecimal();
		var color = center >= signal ? 1m : 2m;

		if (_prevColor is null)
		{
			_prevColor = color;
			if (color == 1m && Position < 0)
				BuyMarket(Math.Abs(Position));
			else if (color == 2m && Position > 0)
				SellMarket(Math.Abs(Position));
			return;
		}

		if (color == 1m && Position < 0)
			BuyMarket(Math.Abs(Position));
		else if (color == 2m && Position > 0)
			SellMarket(Math.Abs(Position));

		var crossUp = _prevColor == 2m && color == 1m;
		var crossDown = _prevColor == 1m && color == 2m;

		if (crossUp && Position <= 0)
			BuyMarket(Volume);
		else if (crossDown && Position >= 0)
			SellMarket(Volume);

		_prevColor = color;
	}
}
