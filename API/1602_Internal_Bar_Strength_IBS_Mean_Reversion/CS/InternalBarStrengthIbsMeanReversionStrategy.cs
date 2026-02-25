using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short strategy using internal bar strength for mean reversion.
/// </summary>
public class InternalBarStrengthIbsMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private bool _isReady;

	public decimal UpperThreshold { get => _upperThreshold.Value; set => _upperThreshold.Value = value; }
	public decimal LowerThreshold { get => _lowerThreshold.Value; set => _lowerThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public InternalBarStrengthIbsMeanReversionStrategy()
	{
		_upperThreshold = Param(nameof(UpperThreshold), 0.9m)
			.SetDisplay("Upper Threshold", "IBS value to trigger entry", "Parameters");

		_lowerThreshold = Param(nameof(LowerThreshold), 0.3m)
			.SetDisplay("Lower Threshold", "IBS value to exit", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = 0;
		_isReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _dummy)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range == 0)
		{
			_prevHigh = candle.HighPrice;
			_isReady = true;
			return;
		}

		if (!_isReady)
		{
			_prevHigh = candle.HighPrice;
			_isReady = true;
			return;
		}

		var ibs = (candle.ClosePrice - candle.LowPrice) / range;

		// Short when close above previous high and IBS is high (near candle top)
		if (candle.ClosePrice > _prevHigh && ibs >= UpperThreshold && Position >= 0)
			SellMarket();
		else if (Position < 0 && ibs <= LowerThreshold)
			BuyMarket();

		_prevHigh = candle.HighPrice;
	}
}
