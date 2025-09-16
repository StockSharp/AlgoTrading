using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor Robot_MACD_12.26.9.
/// Trades MACD signal-line crossovers, but only enters longs while both lines stay below zero and shorts while they stay above zero.
/// Includes an optional balance filter and a fixed take-profit expressed in instrument points.
/// </summary>
public class MacdZeroFilteredCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _lotVolume;
	private readonly StrategyParam<decimal> _minimumBalancePerVolume;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private decimal? _previousMacd;
	private decimal? _previousSignal;

	/// <summary>
	/// Fast EMA length used by MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length used by MACD.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line smoothing length for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Base trading volume that mirrors the "Lots" setting in the original robot.
	/// </summary>
	public decimal LotVolume
	{
		get => _lotVolume.Value;
		set => _lotVolume.Value = value;
	}

	/// <summary>
	/// Minimum account value required per traded volume unit before opening new positions.
	/// </summary>
	public decimal MinimumBalancePerVolume
	{
		get => _minimumBalancePerVolume.Value;
		set => _minimumBalancePerVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MacdZeroFilteredCrossStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Short EMA period for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Long EMA period for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line length for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(6, 12, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 300m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Fixed take-profit distance in price points", "Risk Management");

		_lotVolume = Param(nameof(LotVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Volume", "Trading volume per order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_minimumBalancePerVolume = Param(nameof(MinimumBalancePerVolume), 1000m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Balance per Volume", "Required balance per volume unit before opening trades", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe that drives MACD calculations", "General");
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

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		if (TakeProfitPoints > 0m)
		{
			StartProtection(takeProfit: new Unit(TakeProfitPoints, UnitTypes.Point));
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// Work only with completed candles to avoid premature signals.
		if (candle.State != CandleStates.Finished)
			return;

		// Skip processing when the strategy is not ready or trading is disabled.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		// Ensure both MACD and signal components are available before calculating.
		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal signalLine)
			return;

		if (_previousMacd is decimal prevMacd && _previousSignal is decimal prevSignal)
		{
			var crossUp = prevMacd < prevSignal && macdLine > signalLine;
			var crossDown = prevMacd > prevSignal && macdLine < signalLine;

			// Close existing long position when MACD crosses below the signal line.
			if (crossDown && Position > 0m)
			{
				ClosePosition();
				_previousMacd = macdLine;
				_previousSignal = signalLine;
				return;
			}

			// Close existing short position when MACD crosses above the signal line.
			if (crossUp && Position < 0m)
			{
				ClosePosition();
				_previousMacd = macdLine;
				_previousSignal = signalLine;
				return;
			}

			// Enter long only when the crossover happens below zero (momentum still negative).
			if (crossUp && macdLine < 0m && signalLine < 0m && Position <= 0m && HasRequiredBalance())
			{
				var volume = LotVolume;
				BuyMarket(volume);
			}

			// Enter short only when the crossover happens above zero (momentum still positive).
			else if (crossDown && macdLine > 0m && signalLine > 0m && Position >= 0m && HasRequiredBalance())
			{
				var volume = LotVolume;
				SellMarket(volume);
			}
		}

		_previousMacd = macdLine;
		_previousSignal = signalLine;
	}

	private bool HasRequiredBalance()
	{
		// If portfolio information is not available, assume requirements are met.
		var balance = Portfolio?.CurrentValue;
		if (balance is null)
			return true;

		var required = MinimumBalancePerVolume * LotVolume;
		if (required <= 0m)
			return true;

		if (balance.Value >= required)
			return true;

		LogInfo($"Balance filter blocked entry. Current balance={balance.Value}, required={required}.");
		return false;
	}
}
