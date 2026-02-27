using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading price level breakouts with trailing stop loss.
/// Uses SMA as dynamic level, enters on breakout, trails stop on winning positions.
/// </summary>
public class LevelsWithTrailStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _trailPct;

	private decimal _entryPrice;
	private decimal _bestPrice;
	private decimal _prevPrice;
	private decimal _prevMa;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public decimal TrailPct
	{
		get => _trailPct.Value;
		set => _trailPct.Value = value;
	}

	public LevelsWithTrailStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");

		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetDisplay("MA Period", "Moving average period for level", "Parameters");

		_trailPct = Param(nameof(TrailPct), 1m)
			.SetDisplay("Trail %", "Trailing stop percent", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_bestPrice = 0;
		_prevPrice = 0;
		_prevMa = 0;
		_hasPrev = false;

		var ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Trailing stop management
		if (Position > 0)
		{
			if (price > _bestPrice)
				_bestPrice = price;

			var stopLevel = _bestPrice * (1 - TrailPct / 100m);
			if (price <= stopLevel)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (price < _bestPrice)
				_bestPrice = price;

			var stopLevel = _bestPrice * (1 + TrailPct / 100m);
			if (price >= stopLevel)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (!_hasPrev)
		{
			_prevPrice = price;
			_prevMa = maValue;
			_hasPrev = true;
			return;
		}

		// Entry: price crosses above MA
		if (_prevPrice < _prevMa && price >= maValue && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_entryPrice = price;
			_bestPrice = price;
		}
		// Entry: price crosses below MA
		else if (_prevPrice > _prevMa && price <= maValue && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_entryPrice = price;
			_bestPrice = price;
		}

		_prevPrice = price;
		_prevMa = maValue;
	}
}
