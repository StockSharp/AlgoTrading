using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD cross strategy with zero line filter and fixed take profit.
/// </summary>
public class MacdZeroFilterTakeProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _volumePerTrade;
	private readonly StrategyParam<decimal> _minimumCapitalPerVolume;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private decimal? _previousMacd;
	private decimal? _previousSignal;

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdZeroFilterTakeProfitStrategy"/> class.
	/// </summary>
	public MacdZeroFilterTakeProfitStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Period", "Fast EMA period used by MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(8, 20, 2);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Period", "Slow EMA period used by MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Period", "Signal smoothing period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 15, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 300)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Take profit distance expressed in price points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 600, 50);

		_volumePerTrade = Param(nameof(VolumePerTrade), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Number of lots to trade on each entry", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_minimumCapitalPerVolume = Param(nameof(MinimumCapitalPerVolume), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Capital per Volume", "Minimum portfolio value required per traded lot", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(500m, 5000m, 500m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for MACD calculations", "General");
	}

	/// <summary>
	/// Fast EMA period used by MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period used by MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal smoothing period for MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Number of lots to trade on each entry.
	/// </summary>
	public decimal VolumePerTrade
	{
		get => _volumePerTrade.Value;
		set => _volumePerTrade.Value = value;
	}

	/// <summary>
	/// Minimum portfolio value required per traded lot.
	/// </summary>
	public decimal MinimumCapitalPerVolume
	{
		get => _minimumCapitalPerVolume.Value;
		set => _minimumCapitalPerVolume.Value = value;
	}

	/// <summary>
	/// Timeframe for MACD calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_previousMacd = null;
		_previousSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = VolumePerTrade;

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Absolute),
			stopLoss: new Unit(0m),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// Use only completed candles to avoid double counting signals.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is ready to trade and has a working connector.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
			return;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		if (_previousMacd is null || _previousSignal is null)
		{
			_previousMacd = macd;
			_previousSignal = signal;
			return;
		}

		var previousMacd = _previousMacd.Value;
		var previousSignal = _previousSignal.Value;

		var crossedUp = previousMacd <= previousSignal && macd > signal;
		var crossedDown = previousMacd >= previousSignal && macd < signal;

		if (Position > 0 && crossedDown)
		{
			// Close long position on bearish crossover.
			SellMarket(Position);
		}
		else if (Position < 0 && crossedUp)
		{
			// Close short position on bullish crossover.
			BuyMarket(Math.Abs(Position));
		}

		if (Position == 0)
		{
			var requiredCapital = MinimumCapitalPerVolume * VolumePerTrade;
			if (HasEnoughCapital(requiredCapital))
			{
				if (crossedUp && macd < 0m && signal < 0m)
				{
					// Enter long when MACD crosses above signal under the zero line.
					BuyMarket(VolumePerTrade);
				}
				else if (crossedDown && macd > 0m && signal > 0m)
				{
					// Enter short when MACD crosses below signal above the zero line.
					SellMarket(VolumePerTrade);
				}
			}
		}

		_previousMacd = macd;
		_previousSignal = signal;
	}

	private bool HasEnoughCapital(decimal requiredCapital)
	{
		var currentValue = Portfolio?.CurrentValue;

		if (currentValue is null)
			return true;

		if (currentValue.Value >= requiredCapital)
			return true;

		this.AddInfoLog($"Insufficient capital: available {currentValue.Value:F2}, required {requiredCapital:F2}.");
		return false;
	}
}
