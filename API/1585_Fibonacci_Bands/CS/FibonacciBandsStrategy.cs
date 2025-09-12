using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<string> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _fib1;
	private readonly StrategyParam<decimal> _fib2;
	private readonly StrategyParam<decimal> _fib3;
	private readonly StrategyParam<decimal> _kcMultiplier;
	private readonly StrategyParam<int> _kcLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _ma;
	private AverageTrueRange _atr;
	private RelativeStrengthIndex _rsi;
	private decimal _prevSrc;
	private decimal _prevMa;
	private decimal _prevFbUpper3;
	private decimal _prevFbLower3;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public string MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Fibonacci level 1.
	/// </summary>
	public decimal Fib1
	{
		get => _fib1.Value;
		set => _fib1.Value = value;
	}

	/// <summary>
	/// Fibonacci level 2.
	/// </summary>
	public decimal Fib2
	{
		get => _fib2.Value;
		set => _fib2.Value = value;
	}

	/// <summary>
	/// Fibonacci level 3.
	/// </summary>
	public decimal Fib3
	{
		get => _fib3.Value;
		set => _fib3.Value = value;
	}

	/// <summary>
	/// Keltner multiplier.
	/// </summary>
	public decimal KcMultiplier
	{
		get => _kcMultiplier.Value;
		set => _kcMultiplier.Value = value;
	}

	/// <summary>
	/// Keltner length.
	/// </summary>
	public int KcLength
	{
		get => _kcLength.Value;
		set => _kcLength.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FibonacciBandsStrategy"/>.
	/// </summary>
	public FibonacciBandsStrategy()
	{
		_maType = Param(nameof(MaType), "WMA")
			.SetDisplay("MA Type", "Type of moving average", "General");

		_maLength = Param(nameof(MaLength), 233)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "General")
			.SetCanOptimize(true);

		_fib1 = Param(nameof(Fib1), 1.618m)
			.SetDisplay("Fib Level 1", "Fibonacci level 1", "Levels")
			.SetCanOptimize(true);

		_fib2 = Param(nameof(Fib2), 2.618m)
			.SetDisplay("Fib Level 2", "Fibonacci level 2", "Levels")
			.SetCanOptimize(true);

		_fib3 = Param(nameof(Fib3), 4.236m)
			.SetDisplay("Fib Level 3", "Fibonacci level 3", "Levels")
			.SetCanOptimize(true);

		_kcMultiplier = Param(nameof(KcMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("KC Multiplier", "Keltner multiplier", "Keltner")
			.SetCanOptimize(true);

		_kcLength = Param(nameof(KcLength), 89)
			.SetGreaterThanZero()
			.SetDisplay("KC Length", "ATR length", "Keltner")
			.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		_ma = MaType switch
		{
			"SMA" => new SimpleMovingAverage { Length = MaLength },
			"EMA" => new ExponentialMovingAverage { Length = MaLength },
			"WMA" => new WeightedMovingAverage { Length = MaLength },
			"HMA" => new HullMovingAverage { Length = MaLength },
			_ => new WeightedMovingAverage { Length = MaLength }
		};
		_atr = new AverageTrueRange { Length = KcLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var src = (candle.HighPrice + candle.LowPrice) / 2m;
		var maVal = _ma.Process(src, candle.ServerTime, true).ToDecimal();
		var atrVal = _atr.Process(candle).ToDecimal();
		var rsiVal = _rsi.Process(src, candle.ServerTime, true).ToDecimal();

		if (!_ma.IsFormed || !_atr.IsFormed || !_rsi.IsFormed)
		{
			_prevSrc = src;
			_prevMa = maVal;
			_prevFbUpper3 = maVal;
			_prevFbLower3 = maVal;
			return;
		}

		var kcUpper = maVal + KcMultiplier * atrVal;
		var kcLower = maVal - KcMultiplier * atrVal;
		var fbUpper3 = maVal + Fib3 * (kcUpper - maVal);
		var fbLower3 = maVal - Fib3 * (maVal - kcLower);

		var longCond = _prevSrc <= _prevFbUpper3 && src > fbUpper3 && rsiVal > 60m;
		var shortCond = _prevSrc >= _prevFbLower3 && src < fbLower3 && rsiVal < 40m;
		var exitLong = Position > 0 && _prevSrc <= _prevMa && src > maVal;
		var exitShort = Position < 0 && _prevSrc >= _prevMa && src < maVal;

		if (longCond && Position <= 0)
			BuyMarket(Position < 0 ? Math.Abs(Position) + 1 : 1);
		else if (shortCond && Position >= 0)
			SellMarket(Position > 0 ? Position + 1 : 1);

		if (exitLong)
			SellMarket(Position);
		else if (exitShort)
			BuyMarket(Math.Abs(Position));

		_prevSrc = src;
		_prevMa = maVal;
		_prevFbUpper3 = fbUpper3;
		_prevFbLower3 = fbLower3;
	}
}
