using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exp XPVT strategy: uses Price Volume Trend concept with EMA smoothing.
/// Trades when PVT-based momentum (approximated by volume-weighted ROC + EMA) crosses signal.
/// </summary>
public class ExpXpvtStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _rocLength;

	private decimal _prevPvt;
	private decimal _prevSignal;
	private bool _hasPrev;

	/// <summary>
	/// Constructor.
	/// </summary>
	public ExpXpvtStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "EMA period for PVT signal", "Indicators");

		_rocLength = Param(nameof(RocLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ROC Length", "Rate of change period", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var roc = new RateOfChange { Length = RocLength };
		var ema = new EMA { Length = SmoothingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_hasPrev)
		{
			// ROC crosses above 0 while price above EMA -> buy
			if (_prevPvt <= 0 && rocValue > 0 && candle.ClosePrice > emaValue && Position <= 0)
			{
				BuyMarket();
			}
			// ROC crosses below 0 while price below EMA -> sell
			else if (_prevPvt >= 0 && rocValue < 0 && candle.ClosePrice < emaValue && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevPvt = rocValue;
		_prevSignal = emaValue;
		_hasPrev = true;
	}
}
