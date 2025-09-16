using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot point and Heikin-Ashi based strategy with optional trailing stop.
/// </summary>
public class PivotHeikenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;

	private decimal _pivot;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevClose;
	private bool _dailyInitialized;

	private decimal _haOpen;
	private decimal _haClose;
	private bool _haInitialized;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _trailingStop;
	private decimal _step;
	private decimal _trailingDistance;

	/// <summary>
	/// Candle type used for trading logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Set to 0 to disable.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PivotHeikenStrategy"/>.
	/// </summary>
	public PivotHeikenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle type", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");
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

		_pivot = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_prevClose = 0m;
		_dailyInitialized = false;

		_haOpen = 0m;
		_haClose = 0m;
		_haInitialized = false;

		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_trailingStop = 0m;
		_step = 0m;
		_trailingDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_step = Security?.PriceStep ?? 1m;
		_trailingDistance = TrailingStopPips * _step;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription.Bind(ProcessDailyCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_dailyInitialized)
		{
			_pivot = (_prevHigh + _prevLow + _prevClose) / 3m;
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
		_dailyInitialized = true;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pivot == 0m)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;

		if (!_haInitialized)
		{
			_haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			_haClose = haClose;
			_haInitialized = true;
			return;
		}

		var haOpen = (_haOpen + _haClose) / 2m;
		_haOpen = haOpen;
		_haClose = haClose;

		var isBullish = haClose > haOpen;
		var isBearish = haClose < haOpen;

		if (isBullish && candle.ClosePrice > _pivot && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - StopLossPips * _step;
			_takePrice = _entryPrice + TakeProfitPips * _step;
			_trailingStop = _stopPrice;
		}
		else if (isBearish && candle.ClosePrice < _pivot && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + StopLossPips * _step;
			_takePrice = _entryPrice - TakeProfitPips * _step;
			_trailingStop = _stopPrice;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.LowPrice <= _trailingStop)
			SellMarket(Math.Abs(Position));
			else if (candle.HighPrice >= _takePrice)
			SellMarket(Math.Abs(Position));
			else if (TrailingStopPips > 0)
			{
			var newStop = candle.ClosePrice - _trailingDistance;
			if (newStop > _trailingStop)
			_trailingStop = newStop;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.HighPrice >= _trailingStop)
			BuyMarket(Math.Abs(Position));
			else if (candle.LowPrice <= _takePrice)
			BuyMarket(Math.Abs(Position));
			else if (TrailingStopPips > 0)
			{
			var newStop = candle.ClosePrice + _trailingDistance;
			if (newStop < _trailingStop)
			_trailingStop = newStop;
			}
		}
	}
}
