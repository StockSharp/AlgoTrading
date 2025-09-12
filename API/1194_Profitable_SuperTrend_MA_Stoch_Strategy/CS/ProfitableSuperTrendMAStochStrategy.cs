using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining SuperTrend, EMA crossover and Stochastic oscillator.
/// </summary>
public class ProfitableSuperTrendMAStochStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _maFastPeriod;
	private readonly StrategyParam<int> _maSlowPeriod;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _superTrend;
	private bool _trendUp;
	private bool _initialized;
	private decimal _longStop;
	private decimal _longTarget;
	private decimal _shortStop;
	private decimal _shortTarget;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int MaFastPeriod
	{
		get => _maFastPeriod.Value;
		set => _maFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int MaSlowPeriod
	{
		get => _maSlowPeriod.Value;
		set => _maSlowPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initializes a new instance of the <see cref="ProfitableSuperTrendMAStochStrategy"/>.
	/// </summary>
	public ProfitableSuperTrendMAStochStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Period", "ATR period", "Indicators")
			.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetDisplay("ATR Multiplier", "ATR multiplier", "Indicators")
			.SetCanOptimize(true);

		_maFastPeriod = Param(nameof(MaFastPeriod), 9)
			.SetDisplay("Fast MA Period", "Fast EMA period", "Indicators")
			.SetCanOptimize(true);

		_maSlowPeriod = Param(nameof(MaSlowPeriod), 21)
			.SetDisplay("Slow MA Period", "Slow EMA period", "Indicators")
			.SetCanOptimize(true);

		_stochKPeriod = Param(nameof(StochKPeriod), 14)
			.SetDisplay("Stoch %K", "%K period", "Indicators")
			.SetCanOptimize(true);

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetDisplay("Stoch %D", "%D smoothing", "Indicators")
			.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
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
		_prevFast = 0m;
		_prevSlow = 0m;
		_superTrend = 0m;
		_trendUp = true;
		_initialized = false;
		_longStop = 0m;
		_longTarget = 0m;
		_shortStop = 0m;
		_shortTarget = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var emaFast = new ExponentialMovingAverage { Length = MaFastPeriod };
		var emaSlow = new ExponentialMovingAverage { Length = MaSlowPeriod };
		var stochastic = new StochasticOscillator
		{
			Length = StochKPeriod,
			K = { Length = 1 },
			D = { Length = StochDPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(emaFast, emaSlow, stochastic, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			var stochArea = CreateChartArea();
			DrawIndicator(stochArea, stochastic);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue stochValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k)
			return;
		var atr = atrValue.ToDecimal();
		var price = candle.ClosePrice;

		var median = (candle.HighPrice + candle.LowPrice) / 2;
		var upperBand = median + AtrMultiplier * atr;
		var lowerBand = median - AtrMultiplier * atr;

		if (!_initialized)
		{
			_superTrend = upperBand;
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var prevSuper = _superTrend;
		_superTrend = price > prevSuper ? Math.Max(lowerBand, prevSuper) : price < prevSuper ? Math.Min(upperBand, prevSuper) : prevSuper;
		var prevTrend = _trendUp;
		_trendUp = price > prevSuper ? true : price < prevSuper ? false : prevTrend;

		var maBullish = _prevFast <= _prevSlow && fast > slow;
		var maBearish = _prevFast >= _prevSlow && fast < slow;

		var longCondition = _trendUp && maBullish && k < 80m;
		var shortCondition = !_trendUp && maBearish && k > 20m;

		if (longCondition && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_longStop = price * (1 - StopLossPercent / 100m);
			_longTarget = price * (1 + TakeProfitPercent / 100m);
		}
		else if (shortCondition && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_shortStop = price * (1 + StopLossPercent / 100m);
			_shortTarget = price * (1 - TakeProfitPercent / 100m);
		}

		if (maBearish && Position > 0)
		{
			SellMarket(Position);
			_longStop = 0m;
			_longTarget = 0m;
		}
		else if (maBullish && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_shortStop = 0m;
			_shortTarget = 0m;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTarget)
			{
				SellMarket(Position);
				_longStop = 0m;
				_longTarget = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTarget)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = 0m;
				_shortTarget = 0m;
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
