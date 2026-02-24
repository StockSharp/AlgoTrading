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
/// Uhl adaptive moving average crossover system.
/// Buys when the CTS line crosses above CMA and sells when it crosses below.
/// </summary>
public class UhlMaCrossoverSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _cma;
	private decimal _cts;
	private bool _wasCtsAbove;
	private bool _initialized;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UhlMaCrossoverSystemStrategy()
	{
		_length = Param(nameof(Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback length", "General");

		_multiplier = Param(nameof(Multiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Variance multiplier", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_cma = 0m;
		_cts = 0m;
		_wasCtsAbove = false;
		_initialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = Length };
		var stdDev = new StandardDeviation { Length = Length };

		_cma = 0m;
		_cts = 0m;
		_wasCtsAbove = false;
		_initialized = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Use stdDev^2 as variance
		var varValue = stdValue * stdValue * Multiplier;

		var prevCma = _cma == 0m ? candle.ClosePrice : _cma;
		var prevCts = _cts == 0m ? candle.ClosePrice : _cts;

		var secma = (smaValue - prevCma) * (smaValue - prevCma);
		var sects = (candle.ClosePrice - prevCts) * (candle.ClosePrice - prevCts);

		var ka = secma > 0 && varValue < secma ? 1m - varValue / secma : 0m;
		var kb = sects > 0 && varValue < sects ? 1m - varValue / sects : 0m;

		_cma = ka * smaValue + (1m - ka) * prevCma;
		_cts = kb * candle.ClosePrice + (1m - kb) * prevCts;

		var isCtsAbove = _cts > _cma;

		if (!_initialized)
		{
			_wasCtsAbove = isCtsAbove;
			_initialized = true;
			return;
		}

		if (_wasCtsAbove != isCtsAbove)
		{
			if (isCtsAbove && Position <= 0)
				BuyMarket();
			else if (!isCtsAbove && Position >= 0)
				SellMarket();
		}

		_wasCtsAbove = isCtsAbove;
	}
}
