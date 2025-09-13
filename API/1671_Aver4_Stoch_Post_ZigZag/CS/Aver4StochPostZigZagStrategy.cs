using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Averages four Stochastic oscillators and confirms pivots with a ZigZag style logic.
/// Buys near oversold lows and sells near overbought highs.
/// </summary>
public class Aver4StochPostZigZagStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _midLength1;
	private readonly StrategyParam<int> _midLength2;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stoch1 = null!;
	private StochasticOscillator _stoch2 = null!;
	private StochasticOscillator _stoch3 = null!;
	private StochasticOscillator _stoch4 = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevZigzag;
	private decimal _prev2Zigzag;
	private int _osc;
	private int _prevOsc;

	/// <summary>
	/// Short Stochastic length.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// First medium Stochastic length.
	/// </summary>
	public int MidLength1
	{
		get => _midLength1.Value;
		set => _midLength1.Value = value;
	}

	/// <summary>
	/// Second medium Stochastic length.
	/// </summary>
	public int MidLength2
	{
		get => _midLength2.Value;
		set => _midLength2.Value = value;
	}

	/// <summary>
	/// Long Stochastic length.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// ZigZag depth in candles.
	/// </summary>
	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	/// <summary>
	/// Oversold threshold.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// Overbought threshold.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
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
	/// Initializes a new instance of <see cref="Aver4StochPostZigZagStrategy"/>.
	/// </summary>
	public Aver4StochPostZigZagStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 26)
			.SetDisplay("Short Length", "%K period for fastest Stochastic", "Stochastic")
			.SetCanOptimize(true);
		_midLength1 = Param(nameof(MidLength1), 72)
			.SetDisplay("Mid Length 1", "%K period for second Stochastic", "Stochastic")
			.SetCanOptimize(true);
		_midLength2 = Param(nameof(MidLength2), 144)
			.SetDisplay("Mid Length 2", "%K period for third Stochastic", "Stochastic")
			.SetCanOptimize(true);
		_longLength = Param(nameof(LongLength), 288)
			.SetDisplay("Long Length", "%K period for slowest Stochastic", "Stochastic")
			.SetCanOptimize(true);
		_zigZagDepth = Param(nameof(ZigZagDepth), 14)
			.SetDisplay("ZigZag Depth", "Lookback for pivot detection", "ZigZag")
			.SetCanOptimize(true);
		_oversold = Param(nameof(Oversold), 5m)
			.SetDisplay("Oversold", "Lower averaged Stochastic level", "Signals")
			.SetCanOptimize(true);
		_overbought = Param(nameof(Overbought), 95m)
			.SetDisplay("Overbought", "Upper averaged Stochastic level", "Signals")
			.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevUpper = 0m;
		_prevLower = 0m;
		_prevZigzag = 0m;
		_prev2Zigzag = 0m;
		_osc = 0;
		_prevOsc = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stoch1 = new StochasticOscillator { Length = ShortLength, K = { Length = 3 }, D = { Length = 3 } };
		_stoch2 = new StochasticOscillator { Length = MidLength1, K = { Length = 3 }, D = { Length = 3 } };
		_stoch3 = new StochasticOscillator { Length = MidLength2, K = { Length = 3 }, D = { Length = 3 } };
		_stoch4 = new StochasticOscillator { Length = LongLength, K = { Length = 3 }, D = { Length = 3 } };
		_highest = new Highest { Length = ZigZagDepth };
		_lowest = new Lowest { Length = ZigZagDepth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stoch1, _stoch2, _stoch3, _stoch4, _highest, _lowest, ProcessCandle)
			.Start();

		StartProtection(new Unit(2, UnitTypes.Percent), new Unit(2, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stoch1);
			DrawIndicator(area, _stoch2);
			DrawIndicator(area, _stoch3);
			DrawIndicator(area, _stoch4);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue s1Val, IIndicatorValue s2Val, IIndicatorValue s3Val, IIndicatorValue s4Val, IIndicatorValue upVal, IIndicatorValue lowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!s1Val.IsFinal || !s2Val.IsFinal || !s3Val.IsFinal || !s4Val.IsFinal || !upVal.IsFinal || !lowVal.IsFinal)
			return;

		var k1 = ((StochasticOscillatorValue)s1Val).K;
		var k2 = ((StochasticOscillatorValue)s2Val).K;
		var k3 = ((StochasticOscillatorValue)s3Val).K;
		var k4 = ((StochasticOscillatorValue)s4Val).K;
		var avg = (k1 + k2 + k3 + k4) / 4m;

		var upper = upVal.GetValue<decimal>();
		var lower = lowVal.GetValue<decimal>();

		var crossUpper = false;
		var crossLower = false;

		if (_prevUpper != 0m && _prevLower != 0m && _prev2Zigzag != 0m)
		{
			crossUpper = Cross(_prev2Zigzag, _prevZigzag, _prevUpper, upper);
			crossLower = Cross(_prev2Zigzag, _prevZigzag, _prevLower, lower);
		}

		_prevOsc = _osc;
		if (crossUpper)
			_osc = -1;
		else if (crossLower)
			_osc = 1;

		if (_osc != _prevOsc && IsFormedAndOnlineAndAllowTrading())
		{
			if (avg < Oversold && _osc == 1 && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (avg > Overbought && _osc == -1 && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prev2Zigzag = _prevZigzag;
		_prevZigzag = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}

	private static bool Cross(decimal prevX, decimal currX, decimal prevY, decimal currY)
	{
		return (prevX < prevY && currX > currY) || (prevX > prevY && currX < currY);
	}
}
