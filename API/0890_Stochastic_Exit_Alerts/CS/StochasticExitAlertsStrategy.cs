using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Stochastic oscillator crossovers with separate exit alerts.
/// </summary>
public class StochasticExitAlertsStrategy : Strategy
{
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _kLength;
	private readonly StrategyParam<int> _dLength;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	private decimal? _prevD;

	/// <summary>
	/// Main period of the Stochastic oscillator.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %K line.
	/// </summary>
	public int KLength
	{
		get => _kLength.Value;
		set => _kLength.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %D line.
	/// </summary>
	public int DLength
	{
		get => _dLength.Value;
		set => _dLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Candle type and timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticExitAlertsStrategy"/> class.
	/// </summary>
	public StochasticExitAlertsStrategy()
	{
		_stochLength = Param(nameof(StochLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Main period of the Stochastic oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_kLength = Param(nameof(KLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("%K Length", "Smoothing period for %K line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_dLength = Param(nameof(DLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Length", "Smoothing period for %D line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_stopLossTicks = Param(nameof(StopLossTicks), 600)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance in ticks", "Protection")
			.SetCanOptimize(true)
			.SetOptimize(100, 1500, 100);

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 1200)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in ticks", "Protection")
			.SetCanOptimize(true)
			.SetOptimize(200, 2000, 100);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var stochastic = new StochasticOscillator
		{
			Length = StochLength,
			K = { Length = KLength },
			D = { Length = DLength },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var step = Security.StepPrice ?? 1m;
		StartProtection(
			takeProfit: new Unit(step * TakeProfitTicks, UnitTypes.Absolute),
			stopLoss: new Unit(step * StopLossTicks, UnitTypes.Absolute)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		var kValue = stoch.K;
		var dValue = stoch.D;

		if (_prevK.HasValue && _prevD.HasValue && IsFormedAndOnlineAndAllowTrading())
		{
			var crossover = _prevK < _prevD && kValue > dValue;
			var crossunder = _prevK > _prevD && kValue < dValue;

			if (crossover)
			{
				if (kValue < 20m && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
				else if (kValue >= 20m && Position < 0)
					BuyMarket(Math.Abs(Position));
			}
			else if (crossunder)
			{
				if (kValue > 80m && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
				else if (kValue <= 80m && Position > 0)
					SellMarket(Position);
			}
		}

		_prevK = kValue;
		_prevD = dValue;
	}
}
