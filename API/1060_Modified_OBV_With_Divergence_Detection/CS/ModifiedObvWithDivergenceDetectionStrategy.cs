using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Modified On-Balance Volume strategy with divergence detection.
/// Enters long when OBV-M crosses above its signal line,
/// enters short when OBV-M crosses below its signal line.
/// </summary>
public class ModifiedObvWithDivergenceDetectionStrategy : Strategy
{
	private readonly StrategyParam<int> _obvMaLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private OnBalanceVolume _obv;
	private EMA _obvMa;
	private EMA _signalMa;
	private bool _wasBelowSignal;
	private bool _isInitialized;

	public int ObvMaLength { get => _obvMaLength.Value; set => _obvMaLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ModifiedObvWithDivergenceDetectionStrategy()
	{
		_obvMaLength = Param(nameof(ObvMaLength), 7).SetGreaterThanZero();
		_signalLength = Param(nameof(SignalLength), 10).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_isInitialized = false;

		_obv = new OnBalanceVolume();
		_obvMa = new EMA { Length = ObvMaLength };
		_signalMa = new EMA { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_obv, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_obv.IsFormed)
			return;

		var obvmResult = _obvMa.Process(obvValue);
		var obvm = obvmResult.ToDecimal();
		var signal = _signalMa.Process(obvmResult).ToDecimal();

		if (!_obvMa.IsFormed || !_signalMa.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (!_isInitialized)
		{
			_wasBelowSignal = obvm < signal;
			_isInitialized = true;
			return;
		}

		var isBelow = obvm < signal;

		if (_wasBelowSignal && !isBelow && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket();
		}
		else if (!_wasBelowSignal && isBelow && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket();
		}

		_wasBelowSignal = isBelow;
	}
}
