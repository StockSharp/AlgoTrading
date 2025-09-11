using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hancock volume weighted RSI strategy.
/// Opens long when RSI trend turns up and short when it turns down.
/// </summary>
public class HancockRsiVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _useWicks;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _rmaUp;
	private decimal _rmaDown;
	private decimal _prevClose;
	private int _barIndex;
	private int _trend;

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI trend threshold factor.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Use candle wicks in volume calculation.
	/// </summary>
	public bool UseWicks
	{
		get => _useWicks.Value;
		set => _useWicks.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HancockRsiVolumeStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation period", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(7, 30, 1);

		_threshold = Param(nameof(Threshold), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold", "RSI trend threshold", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_useWicks = Param(nameof(UseWicks), true)
			.SetDisplay("Use Wicks", "Include wicks in volume", "Volume");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rmaUp = 0m;
		_rmaDown = 0m;
		_prevClose = 0m;
		_barIndex = 0;
		_trend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var volUp = GetVolumeUp(candle);
		var volDown = GetVolumeDown(candle);

		if (_barIndex == 1)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var priceDiff = Math.Abs(candle.ClosePrice - _prevClose);
		var denom = Math.Min(RsiLength, _barIndex);

		if (candle.ClosePrice > _prevClose)
		{
			_rmaUp = (priceDiff * volUp + (_rmaUp * (denom - 1))) / denom;
			_rmaDown = (_rmaDown * (denom - 1)) / denom;
		}
		else if (candle.ClosePrice < _prevClose)
		{
			_rmaUp = (_rmaUp * (denom - 1)) / denom;
			_rmaDown = (priceDiff * volDown + (_rmaDown * (denom - 1))) / denom;
		}
		else
		{
			_rmaUp = (_rmaUp * (denom - 1)) / denom;
			_rmaDown = (_rmaDown * (denom - 1)) / denom;
		}

		var sum = _rmaUp + _rmaDown;
		if (sum == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var rsi = 100m * (_rmaUp / sum);
		var top = 50m + 50m * Threshold;
		var bot = 50m - 50m * Threshold;

		var newTrend = _trend;
		if (rsi > top)
			newTrend = 1;
		else if (rsi > bot && _trend > 0)
			newTrend = 1;
		else if (rsi < bot)
			newTrend = -1;
		else if (rsi < top && _trend < 0)
			newTrend = -1;

		if (newTrend != _trend)
		{
			if (newTrend > 0 && Position <= 0)
				BuyMarket();
			else if (newTrend < 0 && Position >= 0)
				SellMarket();

			_trend = newTrend;
		}

		_prevClose = candle.ClosePrice;
	}

	private decimal GetVolumeUp(ICandleMessage candle)
	{
		if (!UseWicks)
			return candle.ClosePrice >= candle.OpenPrice ? candle.TotalVolume : 0m;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			var wickTop = candle.HighPrice - candle.ClosePrice;
			var wickBot = candle.OpenPrice - candle.LowPrice;
			var body = candle.ClosePrice - candle.OpenPrice;
			var diff = candle.TotalVolume / ((wickTop * 2m) + body + (wickBot * 2m));
			return diff * (wickTop + body + wickBot);
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			var wickTop = candle.HighPrice - candle.OpenPrice;
			var wickBot = candle.ClosePrice - candle.LowPrice;
			var body = candle.OpenPrice - candle.ClosePrice;
			var diff = candle.TotalVolume / ((wickTop * 2m) + body + (wickBot * 2m));
			return diff * (wickTop + wickBot);
		}
		return 0m;
	}

	private decimal GetVolumeDown(ICandleMessage candle)
	{
		if (!UseWicks)
			return candle.OpenPrice > candle.ClosePrice ? candle.TotalVolume : 0m;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			var wickTop = candle.HighPrice - candle.ClosePrice;
			var wickBot = candle.OpenPrice - candle.LowPrice;
			var body = candle.ClosePrice - candle.OpenPrice;
			var diff = candle.TotalVolume / ((wickTop * 2m) + body + (wickBot * 2m));
			return diff * (wickTop + wickBot);
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			var wickTop = candle.HighPrice - candle.OpenPrice;
			var wickBot = candle.ClosePrice - candle.LowPrice;
			var body = candle.OpenPrice - candle.ClosePrice;
			var diff = candle.TotalVolume / ((wickTop * 2m) + body + (wickBot * 2m));
			return diff * (wickTop + body + wickBot);
		}
		return 0m;
	}
}
