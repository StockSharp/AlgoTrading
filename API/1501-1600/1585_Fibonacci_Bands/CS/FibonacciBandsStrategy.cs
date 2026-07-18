using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci bands strategy using a moving average and Keltner channel expansions.
/// </summary>
public class FibonacciBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _fib3;
	private readonly StrategyParam<decimal> _kcMultiplier;
	private readonly StrategyParam<int> _kcLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSrc;
	private decimal _prevMa;
	private decimal _prevFbUpper3;
	private decimal _prevFbLower3;
	private bool _isReady;

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public decimal Fib3
	{
		get => _fib3.Value;
		set => _fib3.Value = value;
	}

	public decimal KcMultiplier
	{
		get => _kcMultiplier.Value;
		set => _kcMultiplier.Value = value;
	}

	public int KcLength
	{
		get => _kcLength.Value;
		set => _kcLength.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public FibonacciBandsStrategy()
	{
		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "General");

		_fib3 = Param(nameof(Fib3), 1.618m)
			.SetDisplay("Fib Level 3", "Fibonacci level 3", "Levels");

		_kcMultiplier = Param(nameof(KcMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("KC Multiplier", "Keltner multiplier", "Keltner");

		_kcLength = Param(nameof(KcLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("KC Length", "ATR length", "Keltner");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSrc = 0;
		_prevMa = 0;
		_prevFbUpper3 = 0;
		_prevFbLower3 = 0;
		_isReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var wma = new WeightedMovingAverage { Length = MaLength };
		var atr = new AverageTrueRange { Length = KcLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		_isReady = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(wma, atr, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maVal, decimal atrVal, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var src = (candle.HighPrice + candle.LowPrice) / 2m;

		if (!_isReady)
		{
			_prevSrc = src;
			_prevMa = maVal;
			_prevFbUpper3 = maVal;
			_prevFbLower3 = maVal;
			_isReady = true;
			return;
		}

		var kcUpper = maVal + KcMultiplier * atrVal;
		var kcLower = maVal - KcMultiplier * atrVal;
		var fbUpper3 = maVal + Fib3 * (kcUpper - maVal);
		var fbLower3 = maVal - Fib3 * (maVal - kcLower);

		var longCond = _prevSrc <= _prevFbUpper3 && src > fbUpper3 && rsiVal > 55m;
		var shortCond = _prevSrc >= _prevFbLower3 && src < fbLower3 && rsiVal < 45m;

		if (longCond && Position <= 0)
			BuyMarket();
		else if (shortCond && Position >= 0)
			SellMarket();

		// Exit on MA cross
		if (Position > 0 && src < maVal)
			SellMarket();
		else if (Position < 0 && src > maVal)
			BuyMarket();

		_prevSrc = src;
		_prevMa = maVal;
		_prevFbUpper3 = fbUpper3;
		_prevFbLower3 = fbLower3;
	}
}
