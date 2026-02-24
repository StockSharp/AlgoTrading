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
/// Strategy combining MACD and CCI indicators.
/// Opens long when both indicators drop below negative threshold.
/// Opens short when both indicators rise above positive threshold.
/// </summary>
public class MacdCciLotfyStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _macdCoefficient;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public decimal MacdCoefficient { get => _macdCoefficient.Value; set => _macdCoefficient.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdCciLotfyStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period of CCI indicator", "General");

		_fastPeriod = Param(nameof(FastPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast EMA", "Fast EMA length for MACD", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 33)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow EMA", "Slow EMA length for MACD", "MACD");

		_macdCoefficient = Param(nameof(MacdCoefficient), 86000m)
			.SetGreaterThanZero()
			.SetDisplay("MACD Coefficient", "Multiplier for MACD value", "MACD");

		_threshold = Param(nameof(Threshold), 85m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold", "Absolute level for signals", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = FastPeriod },
			LongMa = { Length = SlowPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var scaledMacd = macdValue * MacdCoefficient;

		if (cciValue < -Threshold && scaledMacd < -Threshold)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (cciValue > Threshold && scaledMacd > Threshold)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
	}
}
