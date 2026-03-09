using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on calculated H4 and L4 levels.
/// When price range expands, places limit orders above and below to catch breakouts.
/// </summary>
public class H4L4BreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private decimal _prevHigh;
	private decimal _prevLow;
	private int _lastSignal;
	private bool _hasPrev;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="H4L4BreakoutStrategy"/>.
	/// </summary>
	public H4L4BreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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
		_prevHigh = 0m;
		_prevLow = 0m;
		_lastSignal = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (candle, ma) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!_hasPrev)
				{
					_prevHigh = candle.HighPrice;
					_prevLow = candle.LowPrice;
					_hasPrev = true;
					return;
				}

				if (candle.ClosePrice > _prevHigh && candle.ClosePrice > ma && _lastSignal != 1 && Position <= 0)
				{
					BuyMarket();
					_lastSignal = 1;
				}
				else if (candle.ClosePrice < _prevLow && candle.ClosePrice < ma && _lastSignal != -1 && Position >= 0)
				{
					SellMarket();
					_lastSignal = -1;
				}

				_prevHigh = candle.HighPrice;
				_prevLow = candle.LowPrice;
			})
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}
