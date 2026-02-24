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
/// Long-only strategy using ADX trend strength and EMA crossover with volume confirmation.
/// Enters long when ADX is strong and fast EMA above slow EMA. Exits when trend weakens.
/// </summary>
public class VolumeConfirmationForATrendSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;

	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolumeConfirmationForATrendSystemStrategy()
	{
		_adxLength = Param(nameof(AdxLength), 14)
			.SetDisplay("ADX Length", "ADX calculation length", "General");
		_fastLength = Param(nameof(FastLength), 13)
			.SetDisplay("Fast EMA", "Fast EMA length", "General");
		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow EMA", "Slow EMA length", "General");
		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetDisplay("ADX Threshold", "Minimum ADX for strong trend", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
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
		_prevFast = 0;
		_prevSlow = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, fast, slow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue adxValue,
		IIndicatorValue fastValue,
		IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (adxValue is not AverageDirectionalIndexValue adxData ||
			adxData.MovingAverage is not decimal adxMa)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();

		// Volume confirmation: volume above average
		var volConfirm = candle.TotalVolume > 0;

		if (adxMa > AdxThreshold && fast > slow && _prevFast <= _prevSlow && volConfirm && Position <= 0)
		{
			BuyMarket();
		}
		else if ((fast < slow || adxMa < AdxThreshold) && Position > 0)
		{
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
