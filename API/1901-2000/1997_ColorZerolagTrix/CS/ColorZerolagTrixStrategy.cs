using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color Zerolag TRIX strategy.
/// Uses TRIX direction changes for trend reversal signals.
/// </summary>
public class ColorZerolagTrixStrategy : Strategy
{
	private readonly StrategyParam<int> _trixPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevTrix;
	private decimal _prevPrevTrix;
	private int _count;

	public int TrixPeriod { get => _trixPeriod.Value; set => _trixPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorZerolagTrixStrategy()
	{
		_trixPeriod = Param(nameof(TrixPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("TRIX Period", "TRIX calculation period", "Indicator");

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
		_prevTrix = 0;
		_prevPrevTrix = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var trix = new Trix { Length = TrixPeriod };

		SubscribeCandles(CandleType)
			.Bind(trix, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal trixValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		if (_count < 3)
		{
			_prevPrevTrix = _prevTrix;
			_prevTrix = trixValue;
			return;
		}

		// Buy when TRIX turns up
		var turnUp = _prevTrix < _prevPrevTrix && trixValue > _prevTrix;
		// Sell when TRIX turns down
		var turnDown = _prevTrix > _prevPrevTrix && trixValue < _prevTrix;

		if (turnUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (turnDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevPrevTrix = _prevTrix;
		_prevTrix = trixValue;
	}
}
