using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on comparing current candle open with previous close on a 30-minute timeframe.
/// </summary>
public class ThirtyMinuteCandleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousClose;
	private decimal? _stopLoss;
	private DateTimeOffset? _lastCandleTime;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ThirtyMinuteCandleStrategy"/>.
	/// </summary>
	public ThirtyMinuteCandleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var tf = (TimeSpan)CandleType.Arg;
		var exitTime = candle.OpenTime + tf - TimeSpan.FromMinutes(1);

		if (CurrentTime >= exitTime && Position != 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else
				BuyMarket(Math.Abs(Position));

			return;
		}

		if (_lastCandleTime != candle.OpenTime)
		{
			if (_previousClose.HasValue)
			{
				if (candle.OpenPrice > _previousClose && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					_stopLoss = _previousClose;
				}
				else if (Position > 0 && candle.OpenPrice < _previousClose)
				{
					SellMarket(Volume + Math.Abs(Position));
					_stopLoss = _previousClose;
				}
			}

			_lastCandleTime = candle.OpenTime;
		}

		if (candle.State == CandleStates.Finished)
			_previousClose = candle.ClosePrice;
	}
}
