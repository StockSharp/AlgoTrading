using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull Moving Average + Stochastic Oscillator strategy.
/// Strategy enters when HMA trend direction changes with Stochastic confirming oversold/overbought conditions.
/// </summary>
public class HullMaStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	// Indicators
	private HullMovingAverage _hma;
	private StochasticOscillator _stochastic;
	private AverageTrueRange _atr;

	// Previous HMA value for trend detection
	private decimal _prevHmaValue;

	/// <summary>
	/// Hull Moving Average period.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochD
	{
		get => _stochD.Value;
		set => _stochD.Value = value;
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
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HullMaStochasticStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Hull Moving Average period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(4, 30, 2);

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Stochastic oscillator period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_stochK = Param(nameof(StochK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Stochastic %K period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_stochD = Param(nameof(StochD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Stochastic %D period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2.0m, 0.5m);
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

		_hma = null;
		_stochastic = null;
		_atr = null;
		_prevHmaValue = 0;
	}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	// Create indicators
	_hma = new HullMovingAverage { Length = HmaPeriod };

		_stochastic = new StochasticOscillator
		{
			K = { Length = StochK },
			D = { Length = StochD },
		};

		_atr = new AverageTrueRange { Length = 14 };

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		
		subscription
			.BindEx(_hma, _stochastic, _atr, ProcessCandle)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _hma);
			
			var secondArea = CreateChartArea();
			if (secondArea != null)
			{
				DrawIndicator(secondArea, _stochastic);
			}
			
			DrawOwnTrades(area);
		}

		StartProtection(
			new(),
			new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(
		ICandleMessage candle, 
		IIndicatorValue hmaValue, 
		IIndicatorValue stochValue, 
		IIndicatorValue atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Get indicator values
		decimal hma = hmaValue.ToDecimal();
		
		var stochTyped = (StochasticOscillatorValue)stochValue;

		if (stochTyped.K is not decimal stochK)
			return;

		decimal atr = atrValue.ToDecimal();

		// Skip first candle after initialization
		if (_prevHmaValue == 0)
		{
			_prevHmaValue = hma;
			return;
		}

		// Detect HMA trend direction
		bool hmaIncreasing = hma > _prevHmaValue;
		bool hmaDecreasing = hma < _prevHmaValue;

		// Trading logic:
		// Buy when HMA starts increasing (trend changes up) and Stochastic shows oversold condition
		if (hmaIncreasing && !hmaDecreasing && stochK < 20 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Long entry: Price={candle.ClosePrice}, HMA={hma}, Prev HMA={_prevHmaValue}, Stochastic %K={stochK}");
		}
		// Sell when HMA starts decreasing (trend changes down) and Stochastic shows overbought condition
		else if (hmaDecreasing && !hmaIncreasing && stochK > 80 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Short entry: Price={candle.ClosePrice}, HMA={hma}, Prev HMA={_prevHmaValue}, Stochastic %K={stochK}");
		}
		// Exit when HMA trend changes direction
		else if (Position > 0 && hmaDecreasing)
		{
			SellMarket(Math.Abs(Position));
			LogInfo($"Long exit: Price={candle.ClosePrice}, HMA={hma}, Prev HMA={_prevHmaValue}");
		}
		else if (Position < 0 && hmaIncreasing)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Short exit: Price={candle.ClosePrice}, HMA={hma}, Prev HMA={_prevHmaValue}");
		}

		// Save current HMA value for next candle
		_prevHmaValue = hma;
	}
}