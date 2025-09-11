using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Builds previous day's OHLC values without requesting separate security data.
/// Enters long on breakout above previous high and short on breakdown below previous low.
/// </summary>
public class SecurityFreeMtfExampleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal _currentOpen;
	private decimal _currentHigh;
	private decimal _currentLow;

	private decimal _prevOpen;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevClose;
	private bool _isFirstDay = true;

	/// <summary>
	/// The type of candles to use for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the class.
	/// </summary>
	public SecurityFreeMtfExampleStrategy()
	{
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

		_currentDay = default;
		_currentOpen = _currentHigh = _currentLow = 0m;
		_prevOpen = _prevHigh = _prevLow = _prevClose = 0m;
		_isFirstDay = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var date = candle.OpenTime.Date;

		if (date != _currentDay)
		{
			if (!_isFirstDay)
			{
				_prevOpen = _currentOpen;
				_prevHigh = _currentHigh;
				_prevLow = _currentLow;
				LogInfo($"Prev day OHLC: O={_prevOpen}, H={_prevHigh}, L={_prevLow}, C={_prevClose}");
			}

			_currentDay = date;
			_currentOpen = candle.OpenPrice;
			_currentHigh = candle.HighPrice;
			_currentLow = candle.LowPrice;
			_isFirstDay = false;
		}
		else
		{
			if (candle.HighPrice > _currentHigh)
				_currentHigh = candle.HighPrice;
			if (candle.LowPrice < _currentLow)
				_currentLow = candle.LowPrice;
		}

		if (_prevHigh != 0 && _prevLow != 0 && IsFormedAndOnlineAndAllowTrading())
		{
			if (candle.ClosePrice > _prevHigh && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (candle.ClosePrice < _prevLow && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevClose = candle.ClosePrice;
	}
}
