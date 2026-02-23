using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XRVI crossover strategy.
/// Uses Relative Vigor Index with its built-in signal line.
/// Buys when RVI Average crosses above Signal and sells when it crosses below.
/// </summary>
public class XrviCrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevAvg;
	private decimal? _prevSig;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public XrviCrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevAvg = default;
		_prevSig = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rvi = new RelativeVigorIndex();

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(rvi, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(2000, UnitTypes.Absolute),
			stopLoss: new Unit(1000, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rvi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rviValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var value = (RelativeVigorIndexValue)rviValue;

		if (value.Average is not decimal avg || value.Signal is not decimal sig)
			return;

		if (_prevAvg is not null && _prevSig is not null)
		{
			var longSignal = _prevAvg <= _prevSig && avg > sig;
			var shortSignal = _prevAvg >= _prevSig && avg < sig;

			if (longSignal && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
			}
			else if (shortSignal && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
			}
		}

		_prevAvg = avg;
		_prevSig = sig;
	}
}
