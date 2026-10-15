using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Liquidity Tracker strategy.
/// The MACD colour state produces the signals. SystemType selects the sensitivity: "Fast" follows the histogram direction,
/// "Normal" follows MACD above or below its signal line, "Safe" also requires MACD on the same side of zero and "Crossover"
/// signals only on the bar where MACD crosses its signal line. A bullish signal goes long, a bearish signal closes the long and,
/// when AllowShortTrades is enabled, goes short. Trades are taken between StartDate and EndDate, with optional percent stop loss
/// and take profit.
/// </summary>
public class MacdLiquidityTrackerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<bool> _allowShortTrades;
	private readonly StrategyParam<string> _systemType;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMacd;
	private decimal? _prevSignal;

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// MACD signal line length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Allow short positions.
	/// </summary>
	public bool AllowShortTrades
	{
		get => _allowShortTrades.Value;
		set => _allowShortTrades.Value = value;
	}

	/// <summary>
	/// Signal mode: Fast, Normal, Safe or Crossover.
	/// </summary>
	public string SystemType
	{
		get => _systemType.Value;
		set => _systemType.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
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
	/// Enable take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// First date trades are allowed.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// Last date trades are allowed.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
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
	/// Constructor.
	/// </summary>
	public MacdLiquidityTrackerStrategy()
	{
		_fastLength = Param(nameof(FastLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "MACD fast EMA length", "MACD");

		_slowLength = Param(nameof(SlowLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "MACD slow EMA length", "MACD");

		_signalLength = Param(nameof(SignalLength), 220)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "MACD signal line length", "MACD");

		_allowShortTrades = Param(nameof(AllowShortTrades), false)
			.SetDisplay("Allow Short Trades", "Allow short positions", "Trading");

		_systemType = Param(nameof(SystemType), "Normal")
			.SetDisplay("System Type", "Signal mode: Fast, Normal, Safe or Crossover", "Trading");

		_useStopLoss = Param(nameof(UseStopLoss), false)
			.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 3m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Use Take Profit", "Enable take profit", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 6m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "First date trades are allowed", "Time");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(2069, 12, 31, 23, 59, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "Last date trades are allowed", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevMacd = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacd = null;
		_prevSignal = null;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		if (UseStopLoss || UseTakeProfit)
		{
			StartProtection(
				UseTakeProfit ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
				UseStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
				useMarketOrders: true);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFormed || macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		_prevMacd = macdLine;
		_prevSignal = signalLine;

		if (prevMacd is not decimal pm || prevSignal is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime < StartDate.UtcDateTime || candle.OpenTime > EndDate.UtcDateTime)
			return;

		var histogram = macdLine - signalLine;
		var prevHistogram = pm - ps;

		bool bullish, bearish;

		switch (SystemType?.Trim().ToLowerInvariant())
		{
			case "fast":
				bullish = histogram > prevHistogram;
				bearish = histogram < prevHistogram;
				break;
			case "safe":
				bullish = macdLine > signalLine && macdLine > 0;
				bearish = macdLine < signalLine && macdLine < 0;
				break;
			case "crossover":
				bullish = pm <= ps && macdLine > signalLine;
				bearish = pm >= ps && macdLine < signalLine;
				break;
			default:
				bullish = macdLine > signalLine;
				bearish = macdLine < signalLine;
				break;
		}

		if (bullish && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (bearish)
		{
			if (AllowShortTrades && Position >= 0)
				SellMarket(Volume + Position);
			else if (Position > 0)
				SellMarket(Position);
		}
	}
}
