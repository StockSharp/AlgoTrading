using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ICT Master Suite session breakout strategy.
/// Trades breakouts of the daily session high and low with ATR-based trailing stop.
/// </summary>
public class IctMasterSuiteTradingIqStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _sessionHigh;
	private decimal _sessionLow;
	private DateTime _sessionDate;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _isLong;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
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
	public IctMasterSuiteTradingIqStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Enable long trades", "General");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Enable short trades", "General");

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

		_sessionHigh = 0m;
		_sessionLow = 0m;
		_sessionDate = default;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new ATR { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(atr, (candle, atrValue) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (_sessionDate != candle.OpenTime.Date)
			{
				_sessionDate = candle.OpenTime.Date;
				_sessionHigh = candle.HighPrice;
				_sessionLow = candle.LowPrice;
				return;
			}

			_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
			_sessionLow = Math.Min(_sessionLow, candle.LowPrice);

			if (Position <= 0 && AllowLong && candle.ClosePrice > _sessionHigh)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - atrValue * AtrMultiplier;
				_isLong = true;
				BuyMarket(Volume + Math.Abs(Position));
				return;
			}

			if (Position >= 0 && AllowShort && candle.ClosePrice < _sessionLow)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + atrValue * AtrMultiplier;
				_isLong = false;
				SellMarket(Volume + Math.Abs(Position));
				return;
			}

			if (Position > 0)
			{
				var newStop = candle.ClosePrice - atrValue * AtrMultiplier;
				if (newStop > _stopPrice)
					_stopPrice = newStop;

				if (candle.LowPrice <= _stopPrice)
					SellMarket(Position);
			}
			else if (Position < 0)
			{
				var newStop = candle.ClosePrice + atrValue * AtrMultiplier;
				if (newStop < _stopPrice)
					_stopPrice = newStop;

				if (candle.HighPrice >= _stopPrice)
					BuyMarket(-Position);
			}
		}).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}
}
