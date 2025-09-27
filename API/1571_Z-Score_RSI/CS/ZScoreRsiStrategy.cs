using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI of price Z-Score with EMA smoothing.
/// Buys when RSI crosses above its EMA and sells on opposite cross.
/// </summary>
public class ZScoreRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _zScoreLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly RelativeStrengthIndex _rsi = new();
	private readonly ExponentialMovingAverage _rsiMa = new();

	private decimal? _prevRsiZ;
	private decimal? _prevRsiMa;

	public int ZScoreLength { get => _zScoreLength.Value; set => _zScoreLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZScoreRsiStrategy()
	{
		_zScoreLength = Param(nameof(ZScoreLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Length", "Length for mean and deviation", "Indicators")
			.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Length for RSI", "Indicators")
			.SetCanOptimize(true);

		_smoothingLength = Param(nameof(SmoothingLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("RSI EMA Length", "EMA length over RSI", "Indicators")
			.SetCanOptimize(true);

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
		_prevRsiZ = null;
		_prevRsiMa = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi.Length = RsiLength;
		_rsiMa.Length = SmoothingLength;

		var mean = new SMA { Length = ZScoreLength };
		var std = new StandardDeviation { Length = ZScoreLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mean, std, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal meanValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stdValue == 0)
			return;

		var z = (candle.ClosePrice - meanValue) / stdValue;

		var rsiValue = _rsi.Process(candle.OpenTime, z);
		if (!rsiValue.IsFinal)
			return;
		var rsiZ = rsiValue.GetValue<decimal>();

		var maValue = _rsiMa.Process(candle.OpenTime, rsiZ);
		if (!maValue.IsFinal)
			return;
		var rsiMa = maValue.GetValue<decimal>();

		if (_prevRsiZ is null)
		{
			_prevRsiZ = rsiZ;
			_prevRsiMa = rsiMa;
			return;
		}

		var crossUp = _prevRsiZ <= _prevRsiMa && rsiZ > rsiMa;
		var crossDown = _prevRsiZ >= _prevRsiMa && rsiZ < rsiMa;

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevRsiZ = rsiZ;
		_prevRsiMa = rsiMa;
	}
}
