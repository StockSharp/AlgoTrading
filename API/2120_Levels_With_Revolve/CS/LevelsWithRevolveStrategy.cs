using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens a position when price crosses a moving average level.
/// Reverses the position when price crosses in the opposite direction.
/// </summary>
public class LevelsWithRevolveStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<decimal> _takePct;

	private decimal _prevPrice;
	private decimal _prevMa;
	private bool _hasPrev;
	private decimal _entryPrice;

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

	public decimal StopPct
	{
		get => _stopPct.Value;
		set => _stopPct.Value = value;
	}

	public decimal TakePct
	{
		get => _takePct.Value;
		set => _takePct.Value = value;
	}

	public LevelsWithRevolveStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");

		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetDisplay("MA Period", "Moving average period for the level", "Parameters");

		_stopPct = Param(nameof(StopPct), 1.5m)
			.SetDisplay("Stop %", "Stop loss percent from entry", "Risk");

		_takePct = Param(nameof(TakePct), 3m)
			.SetDisplay("Take %", "Take profit percent from entry", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPrice = 0;
		_prevMa = 0;
		_hasPrev = false;
		_entryPrice = 0;

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

		// Check stop/take
		if (Position > 0 && _entryPrice > 0)
		{
			var pnlPct = (price - _entryPrice) / _entryPrice * 100m;
			if (pnlPct >= TakePct || pnlPct <= -StopPct)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var pnlPct = (_entryPrice - price) / _entryPrice * 100m;
			if (pnlPct >= TakePct || pnlPct <= -StopPct)
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

		// Cross above MA - buy (or reverse short)
		if (_prevPrice < _prevMa && price >= maValue)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
			{
				BuyMarket();
				_entryPrice = price;
			}
		}
		// Cross below MA - sell (or reverse long)
		else if (_prevPrice > _prevMa && price <= maValue)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
			{
				SellMarket();
				_entryPrice = price;
			}
		}

		_prevPrice = price;
		_prevMa = maValue;
	}
}
