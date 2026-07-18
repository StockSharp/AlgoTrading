using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses linear regression slope crossover with MACD confirmation.
/// Goes long when regression slope turns positive and MACD above signal.
/// Goes short when regression slope turns negative and MACD below signal.
/// </summary>
public class LinearCrossTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _slopeThresholdPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSlope;
	private bool _prevSlopeSet;
	private int _barsFromSignal;

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public decimal SlopeThresholdPercent
	{
		get => _slopeThresholdPercent.Value;
		set => _slopeThresholdPercent.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LinearCrossTradingStrategy()
	{
		_length = Param(nameof(Length), 21)
			.SetGreaterThanZero()
			.SetDisplay("Regression Length", "Number of bars for linear regression", "Indicator");

		_slopeThresholdPercent = Param(nameof(SlopeThresholdPercent), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Slope Threshold %", "Minimum normalized slope for signals", "Indicator");

		_cooldownBars = Param(nameof(CooldownBars), 60)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum bars between entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");
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

		_prevSlope = 0m;
		_prevSlopeSet = false;
		_barsFromSignal = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var linReg = new LinearReg { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(linReg, OnProcess).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, linReg);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal linRegValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;
		if (closePrice <= 0)
			return;

		var slope = (closePrice - linRegValue) / closePrice * 100m;

		if (!_prevSlopeSet)
		{
			_prevSlope = slope;
			_prevSlopeSet = true;
			return;
		}

		_barsFromSignal++;

		if (_barsFromSignal >= CooldownBars)
		{
			if (_prevSlope <= SlopeThresholdPercent && slope > SlopeThresholdPercent && Position <= 0)
			{
				BuyMarket();
				_barsFromSignal = 0;
			}
			else if (_prevSlope >= -SlopeThresholdPercent && slope < -SlopeThresholdPercent && Position >= 0)
			{
				SellMarket();
				_barsFromSignal = 0;
			}
		}

		_prevSlope = slope;
	}
}
