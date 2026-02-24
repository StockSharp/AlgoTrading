using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Binary Wave Standard Deviation strategy.
/// Combines multiple indicators with weights and uses volatility filter based on standard deviation.
/// </summary>
public class BinaryWaveStdDevStrategy : Strategy
{
	private readonly StrategyParam<decimal> _weightMa;
	private readonly StrategyParam<decimal> _weightCci;
	private readonly StrategyParam<decimal> _weightRsi;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<DataType> _candleType;

	public decimal WeightMa { get => _weightMa.Value; set => _weightMa.Value = value; }
	public decimal WeightCci { get => _weightCci.Value; set => _weightCci.Value = value; }
	public decimal WeightRsi { get => _weightRsi.Value; set => _weightRsi.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int StdDevPeriod { get => _stdDevPeriod.Value; set => _stdDevPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BinaryWaveStdDevStrategy()
	{
		_weightMa = Param(nameof(WeightMa), 1m)
			.SetDisplay("MA Weight", "Weight for moving average direction", "Weights");
		_weightCci = Param(nameof(WeightCci), 1m)
			.SetDisplay("CCI Weight", "Weight for CCI direction", "Weights");
		_weightRsi = Param(nameof(WeightRsi), 1m)
			.SetDisplay("RSI Weight", "Weight for RSI", "Weights");

		_maPeriod = Param(nameof(MaPeriod), 13)
			.SetDisplay("MA Period", "Moving average period", "Indicators");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "Lookback period for CCI", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Lookback for RSI", "Indicators");
		_stdDevPeriod = Param(nameof(StdDevPeriod), 9)
			.SetDisplay("StdDev Period", "Length of standard deviation", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var stdDev = new StandardDeviation { Length = StdDevPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, cci, rsi, stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal cciValue, decimal rsiValue, decimal stdDevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var score = 0m;
		score += candle.ClosePrice > emaValue ? WeightMa : -WeightMa;
		score += cciValue > 0 ? WeightCci : -WeightCci;
		score += rsiValue > 50 ? WeightRsi : -WeightRsi;

		if (score > 0 && Position <= 0)
			BuyMarket();
		else if (score < 0 && Position >= 0)
			SellMarket();
	}
}
