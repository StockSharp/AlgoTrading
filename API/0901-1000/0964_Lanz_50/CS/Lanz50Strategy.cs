using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LANZ Strategy 5.0.
/// Three consecutive bullish candles closing above the EmaPeriod EMA go long; with EnableSell, three bearish candles below it go short,
/// reversing an opposite position. Entries are allowed only between StartHour and EndHour (UTC, the window may cross midnight), at most
/// MaxTrades per day and at least MinDistancePips price steps away from the previous entry. A fixed stop and target in price steps protect
/// each position, and any open position is closed once the window ends.
/// </summary>
public class Lanz50Strategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _minDistancePips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _lastEntryPrice;
	private int _dailyTrades;
	private DateTime _currentDay;
	private int _bullishCount;
	private int _bearishCount;

	/// <summary>
	/// EMA trend filter period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Maximum entries per day.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Minimum distance from the previous entry in price steps.
	/// </summary>
	public decimal MinDistancePips
	{
		get => _minDistancePips.Value;
		set => _minDistancePips.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Hour (UTC) the trading window opens.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour (UTC) the trading window closes and positions are closed.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
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
	/// Initialize <see cref="Lanz50Strategy"/>.
	/// </summary>
	public Lanz50Strategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");

		_maxTrades = Param(nameof(MaxTrades), 99)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum entries per day", "Risk");

		_minDistancePips = Param(nameof(MinDistancePips), 25m)
			.SetNotNegative()
			.SetDisplay("Min Distance", "Minimum distance from the previous entry in price steps", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 40m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 120m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit in price steps", "Risk");

		_startHour = Param(nameof(StartHour), 19)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Hour (UTC) the trading window opens", "Time");

		_endHour = Param(nameof(EndHour), 15)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Hour (UTC) the trading window closes and positions are closed", "Time");

		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow long entries", "Mode");

		_enableSell = Param(nameof(EnableSell), false)
			.SetDisplay("Enable Sell", "Allow short entries", "Mode");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_lastEntryPrice = null;
		_dailyTrades = 0;
		_currentDay = default;
		_bullishCount = 0;
		_bearishCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			TakeProfitPips > 0m ? new Unit(TakeProfitPips * step, UnitTypes.Absolute) : new Unit(),
			StopLossPips > 0m ? new Unit(StopLossPips * step, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_bullishCount = candle.ClosePrice > candle.OpenPrice ? _bullishCount + 1 : 0;
		_bearishCount = candle.ClosePrice < candle.OpenPrice ? _bearishCount + 1 : 0;

		var time = candle.OpenTime;
		if (time.Date != _currentDay)
		{
			_currentDay = time.Date;
			_dailyTrades = 0;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hour = time.Hour;
		var inWindow = StartHour <= EndHour
			? hour >= StartHour && hour < EndHour
			: hour >= StartHour || hour < EndHour;

		if (!inWindow)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			return;
		}

		if (_dailyTrades >= MaxTrades)
			return;

		var close = candle.ClosePrice;
		var step = Security.PriceStep ?? 1m;

		if (_lastEntryPrice is decimal lastEntry && Math.Abs(close - lastEntry) < MinDistancePips * step)
			return;

		if (EnableBuy && _bullishCount >= 3 && close > emaValue && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_dailyTrades++;
			_lastEntryPrice = close;
		}
		else if (EnableSell && _bearishCount >= 3 && close < emaValue && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_dailyTrades++;
			_lastEntryPrice = close;
		}
	}
}
