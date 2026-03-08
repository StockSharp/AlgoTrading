using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Anubis CCI + MACD strategy.
/// Buys when CCI crosses above 0 and MACD histogram is positive.
/// Sells when CCI crosses below 0 and MACD histogram is negative.
/// </summary>
public class AnubisCciMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCci;
	private bool _hasPrev;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AnubisCciMacdStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "CCI lookback period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_prevCci = 0m;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(cci, macd, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cciValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!cciValue.IsFinal || !macdValue.IsFinal)
			return;

		var cci = cciValue.ToDecimal();
		var macdVal = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdVal.Macd is not decimal macd || macdVal.Signal is not decimal signal)
			return;

		var histogram = macd - signal;

		if (!_hasPrev)
		{
			_prevCci = cci;
			_hasPrev = true;
			return;
		}

		// CCI crosses above 0 with bullish MACD
		if (_prevCci <= 0 && cci > 0 && histogram > 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// CCI crosses below 0 with bearish MACD
		else if (_prevCci >= 0 && cci < 0 && histogram < 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevCci = cci;
	}
}
