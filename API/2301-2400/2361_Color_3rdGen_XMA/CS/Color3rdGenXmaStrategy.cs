using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color 3rd Generation XMA strategy.
/// Uses two EMAs of different periods to approximate 3rd generation moving average.
/// Opens long when fast EMA crosses above slow EMA, short when crosses below.
/// </summary>
public class Color3RdGenXmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _minSpread;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirst = true;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal MinSpread { get => _minSpread.Value; set => _minSpread.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Color3RdGenXmaStrategy()
	{
		_fastLength = Param(nameof(FastLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "General");

		_slowLength = Param(nameof(SlowLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "General");
		_minSpread = Param(nameof(MinSpread), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Min Spread", "Minimum EMA spread in price steps", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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

		_prevFast = 0m;
		_prevSlow = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0m;
		_prevSlow = 0m;
		_isFirst = true;

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isFirst = false;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var step = Security.PriceStep ?? 1m;
		var spread = Math.Abs(fast - slow);
		if (spread < MinSpread * step)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var prevAbove = _prevFast > _prevSlow;
		var curAbove = fast > slow;

		if (!prevAbove && curAbove && Position <= 0)
			BuyMarket();
		else if (prevAbove && !curAbove && Position >= 0)
			SellMarket();

		_prevFast = fast;
		_prevSlow = slow;
	}
}
