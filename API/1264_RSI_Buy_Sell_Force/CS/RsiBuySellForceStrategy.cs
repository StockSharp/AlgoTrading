using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RSI buy/sell force.
/// Calculates RSI, smooths with EMA, and trades on cc/bb cross.
/// </summary>
public class RsiBuySellForceStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private decimal _prevCc;
	private decimal _prevBb;
	private bool _isFirst = true;

	/// <summary>
	/// RSI and EMA length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RsiBuySellForceStrategy"/>.
	/// </summary>
	public RsiBuySellForceStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period for RSI and EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_ema = null;
		_prevCc = 0m;
		_prevBb = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = Length };
		_ema = new ExponentialMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.ToDecimal();
		var emaValue = _ema.Process(new DecimalIndicatorValue(_ema, rsi)).ToDecimal();

		var d = (rsi - emaValue) * 5m;
		var bb = (rsi - d + emaValue) / 2m;
		var cc = (rsi + d + emaValue) / 2m;

		if (_isFirst)
		{
			_prevCc = cc;
			_prevBb = bb;
			_isFirst = false;
			return;
		}

		if (_prevCc <= _prevBb && cc > bb && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevCc >= _prevBb && cc < bb && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevCc = cc;
		_prevBb = bb;
	}
}
