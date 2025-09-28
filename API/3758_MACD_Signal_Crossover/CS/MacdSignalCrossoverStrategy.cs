using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD crossover strategy with optional partial profit taking.
/// The strategy opens positions on MACD signal line crossovers and closes them on the opposite crossover or protective levels.
/// </summary>
public class MacdSignalCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _partialProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevMacdAboveSignal;
	private decimal? _entryPrice;
	private bool _partialTaken;

	/// <summary>
	/// Fast EMA period for MACD calculation.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD calculation.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit distance in points to close half of the position.
	/// Set to zero to disable partial exits.
	/// </summary>
	public decimal PartialProfitPoints
	{
		get => _partialProfitPoints.Value;
		set => _partialProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for MACD evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MacdSignalCrossoverStrategy"/>.
	/// </summary>
	public MacdSignalCrossoverStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 23)
			.SetDisplay("Fast Period", "Fast EMA length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 40)
			.SetDisplay("Slow Period", "Slow EMA length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(30, 60, 1);

		_signalPeriod = Param(nameof(SignalPeriod), 8)
			.SetDisplay("Signal Period", "Signal line length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 800m, 50m);

		_stopLossPoints = Param(nameof(StopLossPoints), 80m)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 20m);

		_partialProfitPoints = Param(nameof(PartialProfitPoints), 70m)
			.SetDisplay("Partial Profit", "Distance in points to close half position", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for MACD", "General");
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

		_prevMacdAboveSignal = false;
		_entryPrice = null;
		_partialTaken = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
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
			.BindEx(macd, ProcessCandle)
			.Start();

		var takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Points) : null;
		var stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Points) : null;

		if (takeProfit != null || stopLoss != null)
			StartProtection(takeProfit, stopLoss);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_entryPrice = null;
			_partialTaken = false;
			return;
		}

		if (PositionPrice is decimal price && price != 0m)
			_entryPrice = price;

		if ((Position > 0m && delta > 0m) || (Position < 0m && delta < 0m))
			_partialTaken = false;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;
		var signal = macdTyped.Signal;

		var isMacdAboveSignal = macd > signal;
		var crossedAbove = isMacdAboveSignal && !_prevMacdAboveSignal;
		var crossedBelow = !isMacdAboveSignal && _prevMacdAboveSignal;

		if (crossedAbove)
		{
			if (Position < 0m)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0m)
			{
				BuyMarket(Volume);
				LogInfo($"Buy signal: MACD {macd:F5} crossed above signal {signal:F5}.");
			}
		}
		else if (crossedBelow)
		{
			if (Position > 0m)
				SellMarket(Position);

			if (Position >= 0m)
			{
				SellMarket(Volume);
				LogInfo($"Sell signal: MACD {macd:F5} crossed below signal {signal:F5}.");
			}
		}

		TryPartialExit(candle);

		_prevMacdAboveSignal = isMacdAboveSignal;
	}

	private void TryPartialExit(ICandleMessage candle)
	{
		if (_partialTaken)
			return;

		if (PartialProfitPoints <= 0m)
			return;

		if (_entryPrice is not decimal entry)
			return;

		if (Security?.PriceStep is not decimal step || step <= 0m)
			return;

		var offset = PartialProfitPoints * step;
		if (offset <= 0m)
			return;

		if (Position > 0m && candle.ClosePrice >= entry + offset)
		{
			var volume = Position / 2m;
			if (volume > 0m)
			{
				SellMarket(volume);
				_partialTaken = true;
				LogInfo($"Partial exit: closed half long position at {candle.ClosePrice:F5}.");
			}
		}
		else if (Position < 0m && candle.ClosePrice <= entry - offset)
		{
			var volume = Math.Abs(Position) / 2m;
			if (volume > 0m)
			{
				BuyMarket(volume);
				_partialTaken = true;
				LogInfo($"Partial exit: closed half short position at {candle.ClosePrice:F5}.");
			}
		}
	}
}

