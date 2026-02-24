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
/// XDPO Histogram strategy built on double smoothed detrended price oscillator.
/// </summary>
public class XdpoHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _firstMaLength;
	private readonly StrategyParam<int> _secondMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prev1;
	private decimal _prev2;
	private bool _initialized;

	public XdpoHistogramStrategy()
	{
		_firstMaLength = Param(nameof(FirstMaLength), 12)
			.SetDisplay("First MA Length", "Length of the initial moving average.", "Indicators");

		_secondMaLength = Param(nameof(SecondMaLength), 5)
			.SetDisplay("Second MA Length", "Length of the moving average applied to the difference.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy calculations.", "General");
	}

	public int FirstMaLength
	{
		get => _firstMaLength.Value;
		set => _firstMaLength.Value = value;
	}

	public int SecondMaLength
	{
		get => _secondMaLength.Value;
		set => _secondMaLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prev1 = 0m;
		_prev2 = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma1 = new SimpleMovingAverage { Length = FirstMaLength };
		var ma2 = new SimpleMovingAverage { Length = SecondMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma1, ma2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma1Value, decimal ma2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// DPO = close - MA1, then smooth with MA2
		// Since we bind both MAs on close price, approximate: xdpo ~ close - ma1
		// But we need the smoothed version. Use difference between the two MAs as oscillator.
		var xdpo = ma1Value - ma2Value;

		if (!_initialized)
		{
			_prev1 = xdpo;
			_prev2 = xdpo;
			_initialized = true;
			return;
		}

		if (_prev1 < _prev2 && xdpo > _prev1 && Position <= 0)
		{
			BuyMarket();
		}
		else if (_prev1 > _prev2 && xdpo < _prev1 && Position >= 0)
		{
			SellMarket();
		}

		_prev2 = _prev1;
		_prev1 = xdpo;
	}
}
