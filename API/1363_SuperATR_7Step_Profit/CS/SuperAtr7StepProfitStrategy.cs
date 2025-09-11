using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy implementing SuperATR 7-Step Profit logic.
/// Combines adaptive ATR trend detection with multi-step take profit.
/// </summary>
public class SuperAtr7StepProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _atrSmaPeriod;
	private readonly StrategyParam<decimal> _trendStrengthThreshold;
	private readonly StrategyParam<bool> _useMultiStepTp;
	private readonly StrategyParam<int> _atrLengthTp;
	private readonly StrategyParam<decimal> _atrMultiplierTp1;
	private readonly StrategyParam<decimal> _atrMultiplierTp2;
	private readonly StrategyParam<decimal> _atrMultiplierTp3;
	private readonly StrategyParam<decimal> _atrMultiplierTp4;
	private readonly StrategyParam<decimal> _tpLevelPercent1;
	private readonly StrategyParam<decimal> _tpLevelPercent2;
	private readonly StrategyParam<decimal> _tpLevelPercent3;
	private readonly StrategyParam<decimal> _tpPercentAtr;
	private readonly StrategyParam<decimal> _tpPercentFixed;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _trendStrength;
	private SimpleMovingAverage _adaptiveAtrSma;
	private bool _tpOrdersSet;
	private decimal _entryPrice;

	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public int AtrSmaPeriod { get => _atrSmaPeriod.Value; set => _atrSmaPeriod.Value = value; }
	public decimal TrendStrengthThreshold { get => _trendStrengthThreshold.Value; set => _trendStrengthThreshold.Value = value; }
	public bool UseMultiStepTp { get => _useMultiStepTp.Value; set => _useMultiStepTp.Value = value; }
	public int AtrLengthTp { get => _atrLengthTp.Value; set => _atrLengthTp.Value = value; }
	public decimal AtrMultiplierTp1 { get => _atrMultiplierTp1.Value; set => _atrMultiplierTp1.Value = value; }
	public decimal AtrMultiplierTp2 { get => _atrMultiplierTp2.Value; set => _atrMultiplierTp2.Value = value; }
	public decimal AtrMultiplierTp3 { get => _atrMultiplierTp3.Value; set => _atrMultiplierTp3.Value = value; }
	public decimal AtrMultiplierTp4 { get => _atrMultiplierTp4.Value; set => _atrMultiplierTp4.Value = value; }
	public decimal TpLevelPercent1 { get => _tpLevelPercent1.Value; set => _tpLevelPercent1.Value = value; }
	public decimal TpLevelPercent2 { get => _tpLevelPercent2.Value; set => _tpLevelPercent2.Value = value; }
	public decimal TpLevelPercent3 { get => _tpLevelPercent3.Value; set => _tpLevelPercent3.Value = value; }
	public decimal TpPercentAtr { get => _tpPercentAtr.Value; set => _tpPercentAtr.Value = value; }
	public decimal TpPercentFixed { get => _tpPercentFixed.Value; set => _tpPercentFixed.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SuperAtr7StepProfitStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 3).SetDisplay("Short Period", "Short ATR period", "General");
		_longPeriod = Param(nameof(LongPeriod), 7).SetDisplay("Long Period", "Long ATR period", "General");
		_momentumPeriod = Param(nameof(MomentumPeriod), 7).SetDisplay("Momentum Period", "Period for momentum calculation", "General");
		_atrSmaPeriod = Param(nameof(AtrSmaPeriod), 7).SetDisplay("ATR SMA Period", "ATR SMA confirmation period", "General");
		_trendStrengthThreshold = Param(nameof(TrendStrengthThreshold), 1.618m).SetDisplay("Trend Strength Threshold", "Minimum trend strength", "General");
		_useMultiStepTp = Param(nameof(UseMultiStepTp), true).SetDisplay("Use Multi-Step TP", "Enable multi-step take profit", "Risk");
		_atrLengthTp = Param(nameof(AtrLengthTp), 14).SetDisplay("ATR Length TP", "ATR length for TP levels", "Risk");
		_atrMultiplierTp1 = Param(nameof(AtrMultiplierTp1), 2.618m).SetDisplay("ATR Multiplier TP1", "Multiplier for ATR TP1", "Risk");
		_atrMultiplierTp2 = Param(nameof(AtrMultiplierTp2), 5m).SetDisplay("ATR Multiplier TP2", "Multiplier for ATR TP2", "Risk");
		_atrMultiplierTp3 = Param(nameof(AtrMultiplierTp3), 10m).SetDisplay("ATR Multiplier TP3", "Multiplier for ATR TP3", "Risk");
		_atrMultiplierTp4 = Param(nameof(AtrMultiplierTp4), 13.82m).SetDisplay("ATR Multiplier TP4", "Multiplier for ATR TP4", "Risk");
		_tpLevelPercent1 = Param(nameof(TpLevelPercent1), 3m).SetDisplay("Fixed TP1 (%)", "Fixed TP level 1 percent", "Risk");
		_tpLevelPercent2 = Param(nameof(TpLevelPercent2), 8m).SetDisplay("Fixed TP2 (%)", "Fixed TP level 2 percent", "Risk");
		_tpLevelPercent3 = Param(nameof(TpLevelPercent3), 17m).SetDisplay("Fixed TP3 (%)", "Fixed TP level 3 percent", "Risk");
		_tpPercentAtr = Param(nameof(TpPercentAtr), 10m).SetDisplay("Percent per ATR TP", "Percent to exit at each ATR TP", "Risk");
		_tpPercentFixed = Param(nameof(TpPercentFixed), 10m).SetDisplay("Percent per Fixed TP", "Percent to exit at each fixed TP", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_tpOrdersSet = false;
		_entryPrice = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_trendStrength = new SimpleMovingAverage { Length = MomentumPeriod };
		_adaptiveAtrSma = new SimpleMovingAverage { Length = AtrSmaPeriod };

		var momentum = new Momentum { Length = MomentumPeriod };
		var stdev = new StandardDeviation { Length = MomentumPeriod };
		var shortMa = new SimpleMovingAverage { Length = ShortPeriod };
		var longMa = new SimpleMovingAverage { Length = LongPeriod };
		var shortAtr = new AverageTrueRange { Length = ShortPeriod };
		var longAtr = new AverageTrueRange { Length = LongPeriod };
		var atrTp = new AverageTrueRange { Length = AtrLengthTp };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(momentum, stdev, shortMa, longMa, shortAtr, longAtr, atrTp, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue, decimal stdevValue, decimal shortMa, decimal longMa, decimal shortAtr, decimal longAtr, decimal atrTp)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
			_tpOrdersSet = false;

		var momentumFactor = stdevValue != 0m ? Math.Abs(momentumValue / stdevValue) : 0m;
		var adaptiveAtr = (shortAtr * momentumFactor + longAtr) / (1 + momentumFactor);
		var atrMultiple = adaptiveAtr != 0m ? momentumValue / adaptiveAtr : 0m;

		var tsValue = _trendStrength.Process(atrMultiple, candle.CloseTime, true);
		var trendStrength = tsValue.ToDecimal();

		var atrSmaValue = _adaptiveAtrSma.Process(adaptiveAtr, candle.CloseTime, true);
		var adaptiveAtrSma = atrSmaValue.ToDecimal();

		var trendSignal = shortMa > longMa && trendStrength > TrendStrengthThreshold
			? 1
			: shortMa < longMa && trendStrength < -TrendStrengthThreshold
				? -1
				: 0;

		var trendConfirmed = (trendSignal == 1 && candle.ClosePrice > shortMa && adaptiveAtr > adaptiveAtrSma)
			|| (trendSignal == -1 && candle.ClosePrice < shortMa && adaptiveAtr > adaptiveAtrSma);

		var longEntry = trendConfirmed && trendSignal == 1;
		var shortEntry = trendConfirmed && trendSignal == -1;
		var longExit = Position > 0 && shortEntry;
		var shortExit = Position < 0 && longEntry;

		if (longEntry && Position <= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
		}
		else if (shortEntry && Position >= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
		}

		if (longExit && Position > 0)
		{
			CancelActiveOrders();
			SellMarket(Position);
		}
		else if (shortExit && Position < 0)
		{
			CancelActiveOrders();
			BuyMarket(Math.Abs(Position));
		}

		if (UseMultiStepTp && !_tpOrdersSet && Position != 0)
		{
			SetupTakeProfits(atrTp);
			_tpOrdersSet = true;
		}
	}

	private void SetupTakeProfits(decimal atrValue)
	{
		var pos = Position;
		var atrQty = Math.Abs(pos) * TpPercentAtr / 100m;
		var fixedQty = Math.Abs(pos) * TpPercentFixed / 100m;

		if (pos > 0)
		{
			SellLimit(_entryPrice + AtrMultiplierTp1 * atrValue, atrQty);
			SellLimit(_entryPrice + AtrMultiplierTp2 * atrValue, atrQty);
			SellLimit(_entryPrice + AtrMultiplierTp3 * atrValue, atrQty);
			SellLimit(_entryPrice + AtrMultiplierTp4 * atrValue, atrQty);

			SellLimit(_entryPrice * (1 + TpLevelPercent1 / 100m), fixedQty);
			SellLimit(_entryPrice * (1 + TpLevelPercent2 / 100m), fixedQty);
			SellLimit(_entryPrice * (1 + TpLevelPercent3 / 100m), fixedQty);
		}
		else if (pos < 0)
		{
			BuyLimit(_entryPrice - AtrMultiplierTp1 * atrValue, atrQty);
			BuyLimit(_entryPrice - AtrMultiplierTp2 * atrValue, atrQty);
			BuyLimit(_entryPrice - AtrMultiplierTp3 * atrValue, atrQty);
			BuyLimit(_entryPrice - AtrMultiplierTp4 * atrValue, atrQty);

			BuyLimit(_entryPrice * (1 - TpLevelPercent1 / 100m), fixedQty);
			BuyLimit(_entryPrice * (1 - TpLevelPercent2 / 100m), fixedQty);
			BuyLimit(_entryPrice * (1 - TpLevelPercent3 / 100m), fixedQty);
		}
	}
}
