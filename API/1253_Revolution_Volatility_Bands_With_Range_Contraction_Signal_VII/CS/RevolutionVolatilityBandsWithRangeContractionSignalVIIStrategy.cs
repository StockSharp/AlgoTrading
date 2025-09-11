using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA envelope and range contraction signal.
/// Enters on breakout after prolonged decrease of band width.
/// </summary>
public class RevolutionVolatilityBandsWithRangeContractionSignalVIIStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private readonly ExponentialMovingAverage _emaClose = new();
	private readonly ExponentialMovingAverage _emaAbs = new();
	private readonly ExponentialMovingAverage _emaMax = new();
	private readonly ExponentialMovingAverage _emaMin = new();

	private decimal _previousRange;
	private int _fallingCount;

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RevolutionVolatilityBandsWithRangeContractionSignalVIIStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "EMA period length", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_previousRange = 0m;
		_fallingCount = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaClose.Length = Length;
		_emaAbs.Length = Length;
		_emaMax.Length = Length;
		_emaMin.Length = Length;

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(_emaClose, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaClose)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var diff = candle.ClosePrice - emaClose;
		var absDiff = Math.Abs(diff);

		var emaAbs = _emaAbs.Process(new DecimalIndicatorValue(_emaAbs, absDiff)).GetValue<decimal>();

		var upper = emaClose + emaAbs;
		var lower = emaClose - emaAbs;

		var maxVal = Math.Max(upper, candle.ClosePrice);
		var smooth = _emaMax.Process(new DecimalIndicatorValue(_emaMax, maxVal)).GetValue<decimal>();

		var minVal = Math.Min(candle.ClosePrice, lower);
		var smooth2 = _emaMin.Process(new DecimalIndicatorValue(_emaMin, minVal)).GetValue<decimal>();

		var range = smooth - smooth2;

		if (range < _previousRange)
			_fallingCount++;
		else
			_fallingCount = 0;

		if (_fallingCount >= Length)
		{
			if (candle.ClosePrice > smooth && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (candle.ClosePrice < smooth2 && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_previousRange = range;
	}
}
