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
/// Trend strategy based on multiple EMA alignment.
/// Enters long when price is above all 5 EMAs and short when below all 5 EMAs.
/// </summary>
public class TrendAlexcudStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _ma1;
	private readonly StrategyParam<int> _ma2;
	private readonly StrategyParam<int> _ma3;
	private readonly StrategyParam<int> _ma4;
	private readonly StrategyParam<int> _ma5;
	private int _previousBias;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Period of the shortest moving average.</summary>
	public int MaPeriod1 { get => _ma1.Value; set => _ma1.Value = value; }

	/// <summary>Period of the second moving average.</summary>
	public int MaPeriod2 { get => _ma2.Value; set => _ma2.Value = value; }

	/// <summary>Period of the third moving average.</summary>
	public int MaPeriod3 { get => _ma3.Value; set => _ma3.Value = value; }

	/// <summary>Period of the fourth moving average.</summary>
	public int MaPeriod4 { get => _ma4.Value; set => _ma4.Value = value; }

	/// <summary>Period of the longest moving average.</summary>
	public int MaPeriod5 { get => _ma5.Value; set => _ma5.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public TrendAlexcudStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_ma1 = Param(nameof(MaPeriod1), 5)
			.SetGreaterThanZero()
			.SetDisplay("MA 1", "Shortest MA period", "Indicators");
		_ma2 = Param(nameof(MaPeriod2), 8)
			.SetGreaterThanZero()
			.SetDisplay("MA 2", "Second MA period", "Indicators");
		_ma3 = Param(nameof(MaPeriod3), 13)
			.SetGreaterThanZero()
			.SetDisplay("MA 3", "Third MA period", "Indicators");
		_ma4 = Param(nameof(MaPeriod4), 21)
			.SetGreaterThanZero()
			.SetDisplay("MA 4", "Fourth MA period", "Indicators");
		_ma5 = Param(nameof(MaPeriod5), 34)
			.SetGreaterThanZero()
			.SetDisplay("MA 5", "Longest MA period", "Indicators");
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
		_previousBias = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema1 = new ExponentialMovingAverage { Length = MaPeriod1 };
		var ema2 = new ExponentialMovingAverage { Length = MaPeriod2 };
		var ema3 = new ExponentialMovingAverage { Length = MaPeriod3 };
		var ema4 = new ExponentialMovingAverage { Length = MaPeriod4 };
		var ema5 = new ExponentialMovingAverage { Length = MaPeriod5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema1, ema2, ema3, ema4, ema5, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema1);
			DrawIndicator(area, ema2);
			DrawIndicator(area, ema3);
			DrawIndicator(area, ema4);
			DrawIndicator(area, ema5);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal v1, decimal v2, decimal v3, decimal v4, decimal v5)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		var isBull = price > v1 && price > v2 && price > v3 && price > v4 && price > v5;
		var isBear = price < v1 && price < v2 && price < v3 && price < v4 && price < v5;
		var bias = isBull ? 1 : isBear ? -1 : 0;

		if (isBull && _previousBias != 1 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (isBear && _previousBias != -1 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_previousBias = bias;
	}
}
