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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple FX trend-following strategy converted from the MetaTrader 4 expert advisor Simple FX 2.0.
/// Uses fast and slow simple moving averages to detect bullish/bearish regimes and flips the position when the trend reverses.
/// </summary>
public class SimpleFxStrategy : Strategy
{
	private enum TrendDirection
	{
		None,
		Bullish,
		Bearish,
	}

	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _shortMa = null!;
	private SimpleMovingAverage _longMa = null!;

	private decimal? _previousShortValue;
	private decimal? _previousLongValue;
	private TrendDirection _lastTrend = TrendDirection.None;

	/// <summary>
	/// Initializes a new instance of the <see cref="SimpleFxStrategy"/> class.
	/// </summary>
	public SimpleFxStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Short MA Period", "Length of the fast moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 5);

		_longPeriod = Param(nameof(LongPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Long MA Period", "Length of the slow moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(50, 400, 10);


		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Profit target distance expressed in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to build candles for the moving averages", "General");
	}

	/// <summary>
	/// Period for the fast simple moving average.
	/// </summary>
	public int ShortPeriod
	{
		get => _shortPeriod.Value;
		set => _shortPeriod.Value = value;
	}

	/// <summary>
	/// Period for the slow simple moving average.
	/// </summary>
	public int LongPeriod
	{
		get => _longPeriod.Value;
		set => _longPeriod.Value = value;
	}


	/// <summary>
	/// Stop-loss distance measured in instrument steps. Set to zero to disable.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in instrument steps. Set to zero to disable.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for all calculations.
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

		_previousShortValue = null;
		_previousLongValue = null;
		_lastTrend = TrendDirection.None;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_shortMa = new SimpleMovingAverage { Length = ShortPeriod };
		_longMa = new SimpleMovingAverage { Length = LongPeriod };

		var stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Step) : null;
		var takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Step) : null;

		// Enable risk management identical to the original stop-loss/take-profit settings.
		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shortMa, _longMa, ProcessCandles)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _shortMa);
			DrawIndicator(area, _longMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandles(ICandleMessage candle, decimal shortMaValue, decimal longMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var previousShort = _previousShortValue;
		var previousLong = _previousLongValue;

		TrendDirection currentTrend = TrendDirection.None;

		if (previousShort is decimal prevShort && previousLong is decimal prevLong)
		{
			if (shortMaValue > longMaValue && prevShort > prevLong)
			{
				currentTrend = TrendDirection.Bullish;
			}
			else if (shortMaValue < longMaValue && prevShort < prevLong)
			{
				currentTrend = TrendDirection.Bearish;
			}
		}

		_previousShortValue = shortMaValue;
		_previousLongValue = longMaValue;

		if (currentTrend == TrendDirection.None)
			return;

		if (_lastTrend == TrendDirection.None)
		{
			_lastTrend = currentTrend;
			return;
		}

		if (currentTrend == _lastTrend)
			return;

		// Close the opposite position before opening a new trade.
		if (Position > 0 && currentTrend == TrendDirection.Bearish)
		{
			ClosePosition();
		}
		else if (Position < 0 && currentTrend == TrendDirection.Bullish)
		{
			ClosePosition();
		}

		if (currentTrend == TrendDirection.Bullish && Position <= 0)
		{
			// Fast MA crossed above the slow MA for at least two candles -> open long.
			BuyMarket(Volume);
			_lastTrend = currentTrend;
			LogInfo($"Bullish crossover detected at {candle.ClosePrice}. Opening long position.");
			return;
		}

		if (currentTrend == TrendDirection.Bearish && Position >= 0)
		{
			// Fast MA crossed below the slow MA for at least two candles -> open short.
			SellMarket(Volume);
			_lastTrend = currentTrend;
			LogInfo($"Bearish crossover detected at {candle.ClosePrice}. Opening short position.");
			return;
		}

		_lastTrend = currentTrend;
	}
}

