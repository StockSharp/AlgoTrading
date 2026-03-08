using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reversal candle pattern strategy with EMA filter.
/// </summary>
public class ETurboFxStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private int _bearCount;
	private int _bullCount;
	private decimal _prevBody;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ETurboFxStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_bearCount = 0;
		_bullCount = 0;
		_prevBody = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished) return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		if (candle.ClosePrice < candle.OpenPrice)
		{
			_bearCount++;
			if (_hasPrev && body > _prevBody)
				_bearCount = Math.Min(_bearCount, 10);
			else
				_bearCount = 1;

			_bullCount = 0;
		}
		else if (candle.ClosePrice > candle.OpenPrice)
		{
			_bullCount++;
			if (_hasPrev && body > _prevBody)
				_bullCount = Math.Min(_bullCount, 10);
			else
				_bullCount = 1;

			_bearCount = 0;
		}
		else
		{
			_bearCount = 0;
			_bullCount = 0;
		}

		_prevBody = body;
		_hasPrev = true;

		// Buy reversal after 3 bearish candles when price above EMA
		if (_bearCount >= 3 && candle.ClosePrice > emaVal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Sell reversal after 3 bullish candles when price below EMA
		else if (_bullCount >= 3 && candle.ClosePrice < emaVal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
