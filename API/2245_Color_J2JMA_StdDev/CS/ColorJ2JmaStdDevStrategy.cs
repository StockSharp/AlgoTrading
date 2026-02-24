using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the slope of Jurik moving average and its standard deviation.
/// Opens a long position when the JMA slope rises above the high threshold.
/// Opens a short position when the JMA slope falls below the negative high threshold.
/// Existing positions are closed when the slope crosses the opposite low threshold.
/// </summary>
public class ColorJ2JmaStdDevStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<decimal> _k1;
	private readonly StrategyParam<decimal> _k2;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevJma;
	private StandardDeviation _stdDev;

	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	public decimal K1
	{
		get => _k1.Value;
		set => _k1.Value = value;
	}

	public decimal K2
	{
		get => _k2.Value;
		set => _k2.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ColorJ2JmaStdDevStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 5)
			.SetDisplay("JMA Length", "Period of JMA", "Parameters")
			.SetOptimize(3, 20, 1);

		_stdDevPeriod = Param(nameof(StdDevPeriod), 9)
			.SetDisplay("StdDev Period", "Period of standard deviation", "Parameters")
			.SetOptimize(5, 20, 1);

		_k1 = Param(nameof(K1), 0.5m)
			.SetDisplay("K1", "First threshold multiplier (close)", "Parameters")
			.SetOptimize(0.3m, 2m, 0.3m);

		_k2 = Param(nameof(K2), 1.0m)
			.SetDisplay("K2", "Second threshold multiplier (entry)", "Parameters")
			.SetOptimize(0.5m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevJma = null;

		var jma = new JurikMovingAverage { Length = JmaLength };
		_stdDev = new StandardDeviation { Length = StdDevPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevJma is not decimal prev)
		{
			_prevJma = jmaValue;
			return;
		}

		var diff = jmaValue - prev;
		_prevJma = jmaValue;

		// Process diff through StdDev manually with IsFinal = true
		var stdResult = _stdDev.Process(diff, candle.ServerTime, true);

		if (!_stdDev.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stDev = stdResult.GetValue<decimal>();

		if (stDev == 0)
			return;

		var lowThreshold = K1 * stDev;
		var highThreshold = K2 * stDev;

		// Close existing long when slope turns strongly down
		if (Position > 0 && diff < -lowThreshold)
		{
			SellMarket();
			return;
		}

		// Close existing short when slope turns strongly up
		if (Position < 0 && diff > lowThreshold)
		{
			BuyMarket();
			return;
		}

		// Open new long on strong positive slope
		if (Position <= 0 && diff > highThreshold)
		{
			BuyMarket();
		}
		// Open new short on strong negative slope
		else if (Position >= 0 && diff < -highThreshold)
		{
			SellMarket();
		}
	}
}
