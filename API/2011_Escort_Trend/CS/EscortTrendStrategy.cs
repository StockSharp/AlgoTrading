using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Escort Trend strategy combining WMA crossover with MACD and CCI confirmation.
/// Buys when fast WMA above slow WMA, MACD bullish, CCI above threshold.
/// Sells when opposite conditions met.
/// </summary>
public class EscortTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastWmaPeriod;
	private readonly StrategyParam<int> _slowWmaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciThreshold;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _slowWma;
	private CommodityChannelIndex _cci;
	private MovingAverageConvergenceDivergenceSignal _macd;

	public int FastWmaPeriod { get => _fastWmaPeriod.Value; set => _fastWmaPeriod.Value = value; }
	public int SlowWmaPeriod { get => _slowWmaPeriod.Value; set => _slowWmaPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal CciThreshold { get => _cciThreshold.Value; set => _cciThreshold.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EscortTrendStrategy()
	{
		_fastWmaPeriod = Param(nameof(FastWmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Length of fast weighted MA", "General");

		_slowWmaPeriod = Param(nameof(SlowWmaPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Length of slow weighted MA", "General");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI calculation period", "General");

		_cciThreshold = Param(nameof(CciThreshold), 100m)
			.SetDisplay("CCI Threshold", "Threshold for CCI signal", "General");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var fastWma = new WeightedMovingAverage { Length = FastWmaPeriod };
		_slowWma = new WeightedMovingAverage { Length = SlowWmaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastWma, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastWma);
			DrawIndicator(area, _slowWma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var slowResult = _slowWma.Process(candle.ClosePrice, candle.OpenTime, true);
		if (!slowResult.IsFormed)
			return;

		var slow = slowResult.ToDecimal();

		var cciResult = _cci.Process(candle);
		if (!cciResult.IsFormed)
			return;

		var cciValue = cciResult.ToDecimal();

		var macdResult = _macd.Process(candle.ClosePrice, candle.OpenTime, true);
		if (!macdResult.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdResult;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;

		// Buy: fast WMA above slow WMA, MACD bullish, CCI above threshold
		var buy = fast > slow && macdLine > signalLine && cciValue > CciThreshold;
		// Sell: fast WMA below slow WMA, MACD bearish, CCI below negative threshold
		var sell = fast < slow && macdLine < signalLine && cciValue < -CciThreshold;

		if (buy && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (sell && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
