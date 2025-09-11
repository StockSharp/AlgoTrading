using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified Gann Swing strategy with multi-layer confirmation.
/// Detects 1-bar swings and opens trades when three consecutive swings align.
/// </summary>
public class GannSwingMultiLayerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private bool _isInitialized;
	private decimal _prevHigh;
	private decimal _prevLow;
	private int _dirF1;
	private int _dirF2;
	private int _dirF3;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public GannSwingMultiLayerStrategy()
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
		_isInitialized = false;
		_prevHigh = default;
		_prevLow = default;
		_dirF1 = default;
		_dirF2 = default;
		_dirF3 = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_isInitialized = true;
			return;
		}

		var newDir = _dirF1;

		if (candle.HighPrice > _prevHigh && candle.LowPrice > _prevLow)
			newDir = 1;
		else if (candle.HighPrice < _prevHigh && candle.LowPrice < _prevLow)
			newDir = -1;
		else if ((candle.HighPrice >= _prevHigh && candle.LowPrice < _prevLow) ||
			(candle.HighPrice > _prevHigh && candle.LowPrice <= _prevLow))
			newDir = candle.ClosePrice > candle.OpenPrice ? 1 : -1;
		else if (candle.HighPrice <= _prevHigh && candle.LowPrice >= _prevLow)
		{
			_prevHigh = _prevHigh;
			_prevLow = _prevLow;
			return;
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;

		if (newDir != _dirF1)
		{
			_dirF3 = _dirF2;
			_dirF2 = _dirF1;
			_dirF1 = newDir;
		}

		if (_dirF1 == 1 && _dirF2 == 1 && _dirF3 == 1)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_dirF1 == -1 && _dirF2 == -1 && _dirF3 == -1)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else
		{
			if (Position > 0 && _dirF1 == -1)
				SellMarket(Position);
			else if (Position < 0 && _dirF1 == 1)
				BuyMarket(Math.Abs(Position));
		}
	}
}
