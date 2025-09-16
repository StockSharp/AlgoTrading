
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Murrey Math reversal strategy filtered by Bollinger Bands and Stochastic oscillator.
/// </summary>
public class MurreyBBandStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _frame;
	private readonly StrategyParam<decimal> _entryMargin;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<decimal> _bbWidthThreshold;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private BollingerBands _bollinger;
	private StochasticOscillator _stochastic;

	private decimal _levelMinus2;
	private decimal _level0;
	private decimal _level1;
	private decimal _level7;
	private decimal _level8;
	private decimal _levelPlus2;

	private bool _allowLong;
	private bool _allowShort;

	/// <summary>
	/// Initializes a new instance of <see cref="MurreyBBandStochasticStrategy"/>.
	/// </summary>
	public MurreyBBandStochasticStrategy()
	{
		_frame = Param(nameof(Frame), 64)
		.SetGreaterThanZero()
		.SetDisplay("Frame", "Murrey frame size", "General")
		.SetCanOptimize(true);

		_entryMargin = Param(nameof(EntryMargin), 0.00025m)
		.SetDisplay("Entry Margin", "Distance from line for entry", "General")
		.SetCanOptimize(true);

		_bbPeriod = Param(nameof(BbPeriod), 50)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators")
		.SetCanOptimize(true);

		_bbDeviation = Param(nameof(BbDeviation), 4m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Deviation", "Bollinger Bands deviation", "Indicators")
		.SetCanOptimize(true);

		_bbWidthThreshold = Param(nameof(BbWidthThreshold), 0.00080m)
		.SetNotNegative()
		.SetDisplay("Band Width", "Minimum band width", "Filters")
		.SetCanOptimize(true);

		_stochK = Param(nameof(StochK), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "%K length", "Indicators")
		.SetCanOptimize(true);

		_stochD = Param(nameof(StochD), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "%D length", "Indicators")
		.SetCanOptimize(true);

		_stochOversold = Param(nameof(StochOversold), 21m)
		.SetNotNegative()
		.SetDisplay("Stochastic Oversold", "Level for long setups", "Indicators")
		.SetCanOptimize(true);

		_stochOverbought = Param(nameof(StochOverbought), 79m)
		.SetNotNegative()
		.SetDisplay("Stochastic Overbought", "Level for short setups", "Indicators")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <summary>
	/// Murrey frame size.
	/// </summary>
	public int Frame { get => _frame.Value; set => _frame.Value = value; }

	/// <summary>
	/// Maximum distance from Murrey line to allow entry.
	/// </summary>
	public decimal EntryMargin { get => _entryMargin.Value; set => _entryMargin.Value = value; }

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BbDeviation { get => _bbDeviation.Value; set => _bbDeviation.Value = value; }

	/// <summary>
	/// Minimum Bollinger band width.
	/// </summary>
	public decimal BbWidthThreshold { get => _bbWidthThreshold.Value; set => _bbWidthThreshold.Value = value; }

	/// <summary>
	/// Stochastic %K length.
	/// </summary>
	public int StochK { get => _stochK.Value; set => _stochK.Value = value; }

	/// <summary>
	/// Stochastic %D length.
	/// </summary>
	public int StochD { get => _stochD.Value; set => _stochD.Value = value; }

	/// <summary>
	/// Oversold level for Stochastic.
	/// </summary>
	public decimal StochOversold { get => _stochOversold.Value; set => _stochOversold.Value = value; }

	/// <summary>
	/// Overbought level for Stochastic.
	/// </summary>
	public decimal StochOverbought { get => _stochOverbought.Value; set => _stochOverbought.Value = value; }

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = default;
		_lowest = default;
		_bollinger = default;
		_stochastic = default;
		_allowLong = false;
		_allowShort = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Frame };
		_lowest = new Lowest { Length = Frame };
		_bollinger = new BollingerBands { Length = BbPeriod, Width = BbDeviation };
		_stochastic = new StochasticOscillator
		{
			K = { Length = StochK },
			D = { Length = StochD },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_highest, _lowest, _bollinger, _stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue highValue, IIndicatorValue lowValue, IIndicatorValue bbValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!highValue.IsFinal || !lowValue.IsFinal || !bbValue.IsFinal || !stochValue.IsFinal)
		return;

		var nHigh = highValue.ToDecimal();
		var nLow = lowValue.ToDecimal();
		var range = nHigh - nLow;
		if (range == 0m)
		return;

		decimal fractal;
		if (nHigh <= 250000m && nHigh > 25000m)
		fractal = 100000m;
		else if (nHigh <= 25000m && nHigh > 2500m)
		fractal = 10000m;
		else if (nHigh <= 2500m && nHigh > 250m)
		fractal = 1000m;
		else if (nHigh <= 250m && nHigh > 25m)
		fractal = 100m;
		else if (nHigh <= 25m && nHigh > 6.25m)
		fractal = 12.5m;
		else if (nHigh <= 6.25m && nHigh > 3.125m)
		fractal = 6.25m;
		else if (nHigh <= 3.125m && nHigh > 1.5625m)
		fractal = 3.125m;
		else if (nHigh <= 1.5625m && nHigh > 0.390625m)
		fractal = 1.5625m;
		else
		fractal = 0.1953125m;

		var sum = (decimal)Math.Floor(Math.Log((double)(fractal / range), 2));
		var octave = fractal * (decimal)Math.Pow(0.5, (double)sum);
		var minimum = Math.Floor(nLow / octave) * octave;
		var maximum = minimum + 2m * octave;
		if (maximum > nHigh)
		maximum = minimum + octave;

		var diff = maximum - minimum;

		_levelMinus2 = minimum - diff / 4m;
		_level0 = minimum;
		_level1 = minimum + diff / 8m;
		_level7 = minimum + diff * 7m / 8m;
		_level8 = maximum;
		_levelPlus2 = maximum + diff / 4m;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.LowBand is not decimal lower || bb.UpBand is not decimal upper)
		return;

		var bandWidth = upper - lower;

		var stoch = (StochasticOscillatorValue)stochValue;
		var kValue = stoch.K;
		var dValue = stoch.D;

		_allowLong = kValue <= StochOversold && kValue > dValue && candle.ClosePrice < _level0;
		_allowShort = kValue >= StochOverbought && kValue < dValue && candle.ClosePrice > _level8;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position == 0)
		{
			if (_allowLong && bandWidth > BbWidthThreshold && candle.ClosePrice > _level0 && candle.ClosePrice < _level0 + EntryMargin)
			{
				BuyMarket();
			}
			else if (_allowShort && bandWidth > BbWidthThreshold && candle.ClosePrice < _level8 && candle.ClosePrice > _level8 - EntryMargin)
			{
				SellMarket();
			}
		}
		else if (Position > 0)
		{
			if (candle.ClosePrice <= _levelMinus2 || candle.ClosePrice >= _level1)
			{
				SellMarket(Position);
			}
		}
		else
		{
			if (candle.ClosePrice >= _levelPlus2 || candle.ClosePrice <= _level7)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
