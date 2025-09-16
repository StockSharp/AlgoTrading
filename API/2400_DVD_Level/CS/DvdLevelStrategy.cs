namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// RAVI based level strategy. Opens long when the RAVI goes negative and short when it becomes positive.
/// </summary>
public class DvdLevelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;

	private readonly ExponentialMovingAverage _emaFast = new() { Length = 2 };
	private readonly ExponentialMovingAverage _emaSlow = new() { Length = 24 };
	private decimal _prevRavi;
	private bool _hasPrev;

	/// <summary>
	/// Order volume for operations.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="DvdLevelStrategy"/>.
	/// </summary>
	public DvdLevelStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(TimeSpan.FromHours(1));
		subscription.Bind(_emaFast, _emaSlow, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow)
	{
		if (candle.State != CandleStates.Finished || emaSlow == 0)
			return;

		var ravi = (emaFast - emaSlow) / emaSlow * 100m;

		if (!_hasPrev)
		{
			_prevRavi = ravi;
			_hasPrev = true;
			return;
		}

		var crossAbove = _prevRavi <= 0 && ravi > 0;
		var crossBelow = _prevRavi >= 0 && ravi < 0;

		if (crossBelow && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (crossAbove && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}

		_prevRavi = ravi;
	}
}
