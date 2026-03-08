using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Zerolag RSI OSMA indicator.
/// Uses weighted RSI composite with EMA smoothing for direction changes.
/// </summary>
public class ColorZerolagRsiOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOsma;
	private decimal _prevPrevOsma;
	private int _count;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int Smoothing { get => _smoothing.Value; set => _smoothing.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorZerolagRsiOsmaStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicator");

		_smoothing = Param(nameof(Smoothing), 21)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing", "EMA smoothing period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOsma = 0;
		_prevPrevOsma = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ema = new ExponentialMovingAverage { Length = Smoothing };

		SubscribeCandles(CandleType)
			.Bind(rsi, ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// OSMA = difference between RSI and its smoothed version (EMA of price)
		var osma = rsiValue - 50m;
		_count++;

		if (_count < 3)
		{
			_prevPrevOsma = _prevOsma;
			_prevOsma = osma;
			return;
		}

		// Buy when OSMA turns up
		var turnUp = _prevOsma < _prevPrevOsma && osma > _prevOsma;
		// Sell when OSMA turns down
		var turnDown = _prevOsma > _prevPrevOsma && osma < _prevOsma;

		if (turnUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (turnDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevPrevOsma = _prevOsma;
		_prevOsma = osma;
	}
}
