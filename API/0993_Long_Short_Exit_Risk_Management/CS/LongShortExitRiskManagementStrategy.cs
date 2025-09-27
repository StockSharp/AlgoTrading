namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public enum TradingDirection
{
	LongOnly,
	ShortOnly,
	All,
}

/// <summary>
/// Basic strategy demonstrating long and short entries with
/// percentage based risk management.
/// </summary>
public class LongShortExitRiskManagementStrategy : Strategy
{
	private readonly StrategyParam<TradingDirection> _tradingDirection;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _exitBars;
	private readonly StrategyParam<int> _barsToWait;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<decimal> _longValue;
	private readonly StrategyParam<decimal> _shortValue;
	private readonly StrategyParam<bool> _useTimeExit;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private int _barsSinceEntry;
	private int _barsSinceLastTrade;
	private int _tradesToday;
	private DateTime _currentDay;

	/// <summary>
	/// Allowed trading direction.
	/// </summary>
	public TradingDirection TradingDirection
	{
		get => _tradingDirection.Value;
		set => _tradingDirection.Value = value;
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
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Exit position after N bars.
	/// </summary>
	public int ExitBars
	{
		get => _exitBars.Value;
		set => _exitBars.Value = value;
	}

	/// <summary>
	/// Minimum bars between trades.
	/// </summary>
	public int BarsToWait
	{
		get => _barsToWait.Value;
		set => _barsToWait.Value = value;
	}

	/// <summary>
	/// Maximum trades per day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTradesPerDay.Value;
		set => _maxTradesPerDay.Value = value;
	}

	/// <summary>
	/// Value triggering long entry.
	/// </summary>
	public decimal LongValue
	{
		get => _longValue.Value;
		set => _longValue.Value = value;
	}

	/// <summary>
	/// Value triggering short entry.
	/// </summary>
	public decimal ShortValue
	{
		get => _shortValue.Value;
		set => _shortValue.Value = value;
	}

	/// <summary>
	/// Use time based exit.
	/// </summary>
	public bool UseTimeExit
	{
		get => _useTimeExit.Value;
		set => _useTimeExit.Value = value;
	}

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public LongShortExitRiskManagementStrategy()
	{
		_tradingDirection = Param(nameof(TradingDirection), TradingDirection.All)
			.SetDisplay("Trading Direction", "Allowed trading direction", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_exitBars = Param(nameof(ExitBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Exit Bars", "Bars before time based exit", "Risk");

		_barsToWait = Param(nameof(BarsToWait), 10)
			.SetGreaterThanZero()
			.SetDisplay("Bars Between Trades", "Cooldown bars", "General");

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades Per Day", "Maximum daily trades", "General");

		_longValue = Param(nameof(LongValue), 0m)
			.SetDisplay("Long Value", "Long signal price", "Signals");

		_shortValue = Param(nameof(ShortValue), 0m)
			.SetDisplay("Short Value", "Short signal price", "Signals");

		_useTimeExit = Param(nameof(UseTimeExit), false)
			.SetDisplay("Use Time Exit", "Enable time based exit", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_entryPrice = 0m;
		_barsSinceEntry = 0;
		_barsSinceLastTrade = 0;
		_tradesToday = 0;
		_currentDay = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			isStopTrailing: UseTrailingStop,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime.Date != _currentDay)
		{
			_currentDay = candle.OpenTime.Date;
			_tradesToday = 0;
		}

		if (Position == 0)
		{
			if (_barsSinceLastTrade < BarsToWait)
			{
				_barsSinceLastTrade++;
				return;
			}

			if (_tradesToday >= MaxTradesPerDay)
				return;

			var allowLong = TradingDirection == TradingDirection.All || TradingDirection == TradingDirection.LongOnly;
			var allowShort = TradingDirection == TradingDirection.All || TradingDirection == TradingDirection.ShortOnly;

			var longSignal = candle.ClosePrice == LongValue;
			var shortSignal = candle.ClosePrice == ShortValue;

			if (allowLong && longSignal)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_barsSinceEntry = 0;
				_barsSinceLastTrade = 0;
				_tradesToday++;
			}
			else if (allowShort && shortSignal)
			{
				SellMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_barsSinceEntry = 0;
				_barsSinceLastTrade = 0;
				_tradesToday++;
			}
		}
		else
		{
			_barsSinceEntry++;

			if (UseTimeExit && _barsSinceEntry >= ExitBars)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else if (Position < 0)
					BuyMarket(Math.Abs(Position));

				_barsSinceLastTrade = 0;
			}
		}
	}
}
