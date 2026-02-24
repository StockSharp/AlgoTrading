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
/// Segmented linear regression strategy using standard deviation channel.
/// Buys when price crosses above the lower channel and sells when it crosses below the upper channel.
/// </summary>
public class TfSegmentedLinearRegressionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _regLength;
	private readonly StrategyParam<decimal> _multiplier;

	private decimal _prevClose;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Linear regression length.
	/// </summary>
	public int RegLength { get => _regLength.Value; set => _regLength.Value = value; }

	/// <summary>
	/// Channel width multiplier.
	/// </summary>
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	public TfSegmentedLinearRegressionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_regLength = Param(nameof(RegLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Regression Length", "Linear regression period", "Parameters");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Channel width multiplier", "Parameters");
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
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var linReg = new LinearReg { Length = RegLength };
		var stdDev = new StandardDeviation { Length = RegLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(linReg, stdDev, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal regVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdVal <= 0)
			return;

		var upper = regVal + stdVal * Multiplier;
		var lower = regVal - stdVal * Multiplier;

		if (_prevClose != 0)
		{
			if (Position <= 0 && _prevClose < lower && candle.ClosePrice > lower)
				BuyMarket();
			else if (Position >= 0 && _prevClose > upper && candle.ClosePrice < upper)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
	}
}
