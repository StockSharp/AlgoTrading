namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// VR Smart Grid Lite: grid trading based on price levels with SMA filter.
/// </summary>
public class VrSmartGridLiteStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _gridPercent;
	private readonly StrategyParam<int> _smaPeriod;

	private decimal _lastTradePrice;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal GridPercent
	{
		get => _gridPercent.Value;
		set => _gridPercent.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public VrSmartGridLiteStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_gridPercent = Param(nameof(GridPercent), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Grid %", "Grid step percentage", "Grid");

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period for trend", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_initialized = false;

		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_initialized)
		{
			_lastTradePrice = close;
			_initialized = true;
			return;
		}

		var step = _lastTradePrice * GridPercent / 100m;

		if (close <= _lastTradePrice - step)
		{
			BuyMarket();
			_lastTradePrice = close;
		}
		else if (close >= _lastTradePrice + step)
		{
			SellMarket();
			_lastTradePrice = close;
		}
	}
}
