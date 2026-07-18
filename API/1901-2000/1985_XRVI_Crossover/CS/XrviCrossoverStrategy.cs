using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XRVI crossover strategy.
/// Buys when RVI Average crosses above Signal, sells when it crosses below.
/// </summary>
public class XrviCrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevAvg;
	private decimal? _prevSig;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XrviCrossoverStrategy()
	{
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
		_prevAvg = null;
		_prevSig = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rvi = new RelativeVigorIndex();

		SubscribeCandles(CandleType)
			.BindEx(rvi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rviValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = (IRelativeVigorIndexValue)rviValue;
		if (value.Average is not decimal avg || value.Signal is not decimal sig)
			return;

		if (_prevAvg is not null && _prevSig is not null)
		{
			var crossUp = _prevAvg <= _prevSig && avg > sig;
			var crossDown = _prevAvg >= _prevSig && avg < sig;

			if (crossUp && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (crossDown && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevAvg = avg;
		_prevSig = sig;
	}
}
