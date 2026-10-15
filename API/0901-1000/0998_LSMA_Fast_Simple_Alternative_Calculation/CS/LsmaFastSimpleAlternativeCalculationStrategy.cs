using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LSMA Fast And Simple Alternative Calculation Strategy.
/// Buys when close price crosses above LSMA and sells when it crosses below.
/// </summary>
public class LsmaFastSimpleAlternativeCalculationStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _wma;
	private SimpleMovingAverage _sma;
	private decimal _prevDiff;
	private int _cooldown;

	public LsmaFastSimpleAlternativeCalculationStrategy()
	{
		_length = Param(nameof(Length), 50)
			.SetDisplay("LSMA Length", "Length for LSMA calculation", "LSMA");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDiff = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_wma = new WeightedMovingAverage { Length = Length };
		_sma = new SimpleMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_wma, _sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wmaValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_wma.IsFormed || !_sma.IsFormed)
			return;

		var lsma = 3m * wmaValue - 2m * smaValue;
		var diff = candle.ClosePrice - lsma;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevDiff = diff;
			return;
		}

		if (_prevDiff <= 0m && diff > 0m && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldown = 10;
		}
		else if (_prevDiff >= 0m && diff < 0m && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldown = 10;
		}

		_prevDiff = diff;
	}
}
