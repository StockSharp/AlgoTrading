using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatile Action strategy.
/// Combines volatility breakout (ATR) with Alligator trend filter.
/// </summary>
public class VolatileActionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volatilityCoef;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr1;
	private AverageTrueRange _atrBase;
	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	public decimal VolatilityCoef { get => _volatilityCoef.Value; set => _volatilityCoef.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolatileActionStrategy()
	{
		_volatilityCoef = Param(nameof(VolatilityCoef), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Coef", "ATR1 multiplier against base ATR", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Base ATR period", "General");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for main calculation", "General");
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
		_atr1 = default;
		_atrBase = default;
		_jaw = default;
		_teeth = default;
		_lips = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr1 = new AverageTrueRange { Length = 1 };
		_atrBase = new AverageTrueRange { Length = AtrPeriod };
		_jaw = new SmoothedMovingAverage { Length = 13 };
		_teeth = new SmoothedMovingAverage { Length = 8 };
		_lips = new SmoothedMovingAverage { Length = 5 };

		Indicators.Add(_atr1);
		Indicators.Add(_atrBase);
		Indicators.Add(_jaw);
		Indicators.Add(_teeth);
		Indicators.Add(_lips);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atr1Val = _atr1.Process(candle);
		var atrBaseVal = _atrBase.Process(candle);

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var jawVal = _jaw.Process(median, candle.OpenTime, true);
		var teethVal = _teeth.Process(median, candle.OpenTime, true);
		var lipsVal = _lips.Process(median, candle.OpenTime, true);

		if (!atr1Val.IsFormed || !atrBaseVal.IsFormed || !jawVal.IsFormed || !teethVal.IsFormed || !lipsVal.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr1 = atr1Val.ToDecimal();
		var atrBase = atrBaseVal.ToDecimal();
		var jaw = jawVal.ToDecimal();
		var teeth = teethVal.ToDecimal();
		var lips = lipsVal.ToDecimal();

		var hl = candle.HighPrice - candle.LowPrice;
		if (hl == 0) return;

		var hc = Math.Abs(candle.HighPrice - candle.ClosePrice);
		var lc = Math.Abs(candle.LowPrice - candle.ClosePrice);

		// Alligator bullish: lips > teeth > jaw
		var bullGator = lips > teeth && teeth > jaw;
		// Alligator bearish: lips < teeth < jaw
		var bearGator = lips < teeth && teeth < jaw;

		// Volatility breakout
		var volBreakout = atrBase > 0 && atr1 > VolatilityCoef * atrBase;

		if (Position == 0)
		{
			if (volBreakout && bullGator && candle.ClosePrice > candle.OpenPrice)
			{
				BuyMarket();
			}
			else if (volBreakout && bearGator && candle.ClosePrice < candle.OpenPrice)
			{
				SellMarket();
			}
		}
	}
}
