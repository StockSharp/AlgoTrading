using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTC Difficulty Adjustments Strategy - uses rate of change indicator
/// to detect momentum shifts and trade accordingly.
/// </summary>
public class BtcDifficultyAdjustmentsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rocPeriod;
	private readonly StrategyParam<int> _smaPeriod;

	private decimal _prevRoc;
	private decimal _prevSma;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RocPeriod { get => _rocPeriod.Value; set => _rocPeriod.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

	public BtcDifficultyAdjustmentsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rocPeriod = Param(nameof(RocPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("ROC Period", "Rate of change period", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Trend filter SMA period", "Indicators");
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
		_prevRoc = 0m;
		_prevSma = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var roc = new RateOfChange { Length = RocPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}

		var rocArea = CreateChartArea();
		if (rocArea != null)
		{
			DrawIndicator(rocArea, roc);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevRoc == 0m || _prevSma == 0m)
		{
			_prevRoc = rocValue;
			_prevSma = smaValue;
			return;
		}

		// Buy when ROC crosses above zero and price above SMA
		if (_prevRoc <= 0m && rocValue > 0m && candle.ClosePrice > smaValue && Position <= 0)
		{
			BuyMarket();
		}
		// Sell when ROC crosses below zero and price below SMA
		else if (_prevRoc >= 0m && rocValue < 0m && candle.ClosePrice < smaValue && Position >= 0)
		{
			SellMarket();
		}

		_prevRoc = rocValue;
		_prevSma = smaValue;
	}
}
