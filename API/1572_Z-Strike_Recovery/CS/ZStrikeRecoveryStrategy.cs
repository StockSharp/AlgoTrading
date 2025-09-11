using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long when price change Z-Score exceeds threshold and exit after fixed periods.
/// </summary>
public class ZStrikeRecoveryStrategy : Strategy
{
	private readonly StrategyParam<int> _zLength;
	private readonly StrategyParam<decimal> _zThreshold;
	private readonly StrategyParam<int> _exitPeriods;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SMA _mean = new();
	private readonly StandardDeviation _std = new();

	private decimal? _prevClose;
	private int _barsInPosition;

	public int ZLength { get => _zLength.Value; set => _zLength.Value = value; }
	public decimal ZThreshold { get => _zThreshold.Value; set => _zThreshold.Value = value; }
	public int ExitPeriods { get => _exitPeriods.Value; set => _exitPeriods.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZStrikeRecoveryStrategy()
	{
		_zLength = Param(nameof(ZLength), 16)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Length", "Lookback length for z-score", "Indicators")
			.SetCanOptimize(true);

		_zThreshold = Param(nameof(ZThreshold), 1.3m)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Threshold", "Entry threshold", "Trading")
			.SetCanOptimize(true);

		_exitPeriods = Param(nameof(ExitPeriods), 10)
			.SetGreaterThanZero()
			.SetDisplay("Exit Periods", "Bars to hold position", "Trading")
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
		_prevClose = null;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_mean.Length = ZLength;
		_std.Length = ZLength;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevClose is null)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var change = candle.ClosePrice - _prevClose.Value;
		_prevClose = candle.ClosePrice;

		var meanVal = _mean.Process(candle.OpenTime, change);
		var stdVal = _std.Process(candle.OpenTime, change);

		if (!stdVal.IsFinal || stdVal.GetValue<decimal>() == 0)
			return;

		var mean = meanVal.GetValue<decimal>();
		var std = stdVal.GetValue<decimal>();
		var z = (change - mean) / std;

		if (z > ZThreshold && Position == 0)
		{
			BuyMarket();
			_barsInPosition = 0;
		}

		if (Position != 0)
		{
			_barsInPosition++;

			if (_barsInPosition >= ExitPeriods)
				ClosePosition();
		}
	}
}
