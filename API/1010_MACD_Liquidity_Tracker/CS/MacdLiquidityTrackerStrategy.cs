using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD based strategy with multiple signal generation modes.
/// </summary>
public class MacdLiquidityTrackerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<SystemType> _systemType;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private bool _prevLongSignal;
	private bool _prevShortSignal;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Signal EMA period for MACD.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool AllowShortTrades { get => _allowShort.Value; set => _allowShort.Value = value; }

	/// <summary>
	/// System logic selection.
	/// </summary>
	public SystemType SystemLogic { get => _systemType.Value; set => _systemType.Value = value; }

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Start date for trading.
	/// </summary>
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>
	/// End date for trading.
	/// </summary>
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="MacdLiquidityTrackerStrategy"/>.
	/// </summary>
	public MacdLiquidityTrackerStrategy()
	{
		_fastLength = Param(nameof(FastLength), 25)
			.SetDisplay("Fast MA", "Fast EMA period", "MACD")
			.SetGreaterThanZero();

		_slowLength = Param(nameof(SlowLength), 60)
			.SetDisplay("Slow MA", "Slow EMA period", "MACD")
			.SetGreaterThanZero();

		_signalLength = Param(nameof(SignalLength), 220)
			.SetDisplay("Signal MA", "Signal EMA period", "MACD")
			.SetGreaterThanZero();

		_allowShort = Param(nameof(AllowShortTrades), false)
			.SetDisplay("Allow Short Trades", "Enable short trades", "General");

		_systemType = Param(nameof(SystemLogic), SystemType.Normal)
			.SetDisplay("System Type", "Logic for signals", "System");

		_useStopLoss = Param(nameof(UseStopLoss), false)
			.SetDisplay("Enable Stop Loss", "Use stop loss", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 3m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Enable Take Profit", "Use take profit", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 6m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Trading start date", "Date Range");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(2069, 12, 31, 23, 59, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "Trading end date", "Date Range");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMacd = null;
		_prevSignal = null;
		_prevLongSignal = false;
		_prevShortSignal = false;
		_macd = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new()
		{
			Macd =
			{
				ShortMa = new ExponentialMovingAverage { Length = FastLength },
				LongMa = new ExponentialMovingAverage { Length = SlowLength },
			},
			SignalMa = new ExponentialMovingAverage { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		if (UseStopLoss || UseTakeProfit)
		{
			StartProtection(
				takeProfit: UseTakeProfit ? new Unit(TakeProfitPercent, UnitTypes.Percent) : default,
				stopLoss: UseStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : default);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var m = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (m.Macd is not decimal macd || m.Signal is not decimal signal)
			return;

		var isBrightBlue = macd > 0m && (_prevMacd is null || macd > _prevMacd);
		var isDarkBlue = macd > 0m && _prevMacd is not null && macd <= _prevMacd;
		var isBrightMagenta = macd <= 0m && _prevMacd is not null && macd < _prevMacd;
		var isDarkMagenta = macd <= 0m && (_prevMacd is null || macd >= _prevMacd);

		bool longSignal = false;
		bool shortSignal = false;

		switch (SystemLogic)
		{
			case SystemType.Fast:
				longSignal = isBrightBlue || isDarkMagenta;
				shortSignal = isDarkBlue || isBrightMagenta;
				break;
			case SystemType.Normal:
				longSignal = macd > signal;
				shortSignal = macd < signal;
				break;
			case SystemType.Safe:
				longSignal = isBrightBlue;
				shortSignal = isDarkBlue || isBrightMagenta || isDarkMagenta;
				break;
			case SystemType.Crossover:
				longSignal = macd > signal && (_prevMacd <= _prevSignal);
				shortSignal = macd < signal && (_prevMacd >= _prevSignal);
				break;
		}

		var inDateRange = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;
		longSignal &= inDateRange;
		shortSignal &= inDateRange;

		var longEntry = longSignal && !_prevLongSignal;
		var shortEntry = shortSignal && !_prevShortSignal;
		var longExit = shortSignal && !_prevShortSignal;
		var shortExit = longSignal && !_prevLongSignal;

		if (longEntry && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortEntry && AllowShortTrades && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		if (longExit && Position > 0)
		{
			SellMarket(Position);
		}
		else if (shortExit && Position < 0)
		{
			BuyMarket(-Position);
		}

		_prevLongSignal = longSignal;
		_prevShortSignal = shortSignal;
		_prevMacd = macd;
		_prevSignal = signal;
	}

	/// <summary>
	/// Types of system logic.
	/// </summary>
	public enum SystemType
	{
		/// <summary>
		/// Uses MACD momentum colour states.
		/// </summary>
		Fast,

		/// <summary>
		/// Uses MACD above/below signal.
		/// </summary>
		Normal,

		/// <summary>
		/// Requires strong bullish colour for longs.
		/// </summary>
		Safe,

		/// <summary>
		/// Trades on MACD crossovers.
		/// </summary>
		Crossover
	}
}
