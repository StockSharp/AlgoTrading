using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on fast/slow moving averages and Stochastic oscillator.
/// Buys when %K crosses above %D below oversold level while trend is up.
/// Sells when %K crosses below %D above overbought level while trend is down.
/// </summary>
public class StuficStochStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;
	private bool _isFirst = true;

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
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
	/// Stochastic %D period.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level for Stochastic.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Oversold level for Stochastic.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public StuficStochStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 10);

		_stochKPeriod = Param(nameof(StochKPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch %K", "%K period for Stochastic", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch %D", "%D period for Stochastic", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
			.SetRange(50, 95)
			.SetDisplay("Overbought", "Overbought level", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(70m, 90m, 5m);

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetRange(5, 50)
			.SetDisplay("Oversold", "Oversold level", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(10m, 30m, 5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(StockSharp.BusinessEntities.Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		var slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochKPeriod },
			D = { Length = StochDPeriod },
		};

		// Subscribe to candles
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastMa, slowMa, stochastic, ProcessCandle)
			.Start();

		// Enable stop loss protection
		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			isStopTrailing: false,
			useMarketOrders: true
		);

		// Setup chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			var stochArea = CreateChartArea();
			DrawIndicator(stochArea, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue stochValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure strategy is ready
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K;
		var d = stoch.D;

		if (_isFirst)
		{
			_prevK = k;
			_prevD = d;
			_isFirst = false;
			return;
		}

		// Check for bullish signal
		if (_prevK <= _prevD && k > d && k < OversoldLevel && fast > slow && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		// Check for bearish signal
		else if (_prevK >= _prevD && k < d && k > OverboughtLevel && fast < slow && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevK = k;
		_prevD = d;
	}
}

