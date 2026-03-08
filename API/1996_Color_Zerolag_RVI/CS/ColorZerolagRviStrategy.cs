using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossing of RVI and its signal line.
/// Buys on RVI crossing above signal, sells on crossing below.
/// </summary>
public class ColorZerolagRviStrategy : Strategy
{
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevRvi;
	private decimal? _prevSignal;

	public int RviLength { get => _rviLength.Value; set => _rviLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorZerolagRviStrategy()
	{
		_rviLength = Param(nameof(RviLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "RVI calculation period", "Indicator");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "RVI signal line period", "Indicator");

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
		_prevRvi = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rvi = new RelativeVigorIndex();
		rvi.Average.Length = RviLength;
		rvi.Signal.Length = SignalLength;

		SubscribeCandles(CandleType)
			.BindEx(rvi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rviValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = (IRelativeVigorIndexValue)rviValue;
		if (value.Average is not decimal rvi || value.Signal is not decimal signal)
			return;

		if (_prevRvi is null || _prevSignal is null)
		{
			_prevRvi = rvi;
			_prevSignal = signal;
			return;
		}

		var crossUp = _prevRvi < _prevSignal && rvi > signal;
		var crossDown = _prevRvi > _prevSignal && rvi < signal;

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

		_prevRvi = rvi;
		_prevSignal = signal;
	}
}
