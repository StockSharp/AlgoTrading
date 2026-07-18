using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy with EMA direction entries and trailing stop management.
/// Enters on EMA direction change, exits via trailing stop.
/// </summary>
public class TrailingStopActivationStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevPrevEma;
	private int _count;
	private decimal _entryPrice;
	private decimal _stopPrice;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrailingStopActivationStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for entries", "Indicator");

		_trailingStop = Param(nameof(TrailingStop), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = 0;
		_prevPrevEma = 0;
		_count = 0;
		_entryPrice = 0;
		_stopPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Trailing stop check
		if (Position > 0)
		{
			var trail = close - TrailingStop;
			if (trail > _stopPrice)
				_stopPrice = trail;

			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
			}
		}
		else if (Position < 0)
		{
			var trail = close + TrailingStop;
			if (_stopPrice == 0 || trail < _stopPrice)
				_stopPrice = trail;

			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopPrice = 0;
			}
		}

		_count++;

		if (_count < 3)
		{
			_prevPrevEma = _prevEma;
			_prevEma = emaValue;
			return;
		}

		// Entry on EMA direction change
		var turnUp = _prevEma < _prevPrevEma && emaValue > _prevEma;
		var turnDown = _prevEma > _prevPrevEma && emaValue < _prevEma;

		if (turnUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_entryPrice = close;
			_stopPrice = close - TrailingStop;
		}
		else if (turnDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_entryPrice = close;
			_stopPrice = close + TrailingStop;
		}

		_prevPrevEma = _prevEma;
		_prevEma = emaValue;
	}
}
