using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SJ NIFTY trend following strategy based on SuperTrend, VWAP, RSI and EMA200 with Keltner Channel filter.
/// Calculates position size from risk percentage of portfolio and applies stop-loss and take-profit.
/// </summary>
public class SjNiftyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<decimal> _rrTarget;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<int> _kcLength;
	private readonly StrategyParam<decimal> _kcMultiplier;
	private readonly StrategyParam<bool> _useKcFilter;
	private readonly StrategyParam<decimal> _riskPctCap;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _prevLongCond;
	private bool _prevShortCond;
	private int _barIndex;
	private int _lastSignalIndex = int.MinValue;
	
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;
	
	/// <summary>
	/// Technical stop loss percent from entry.
	/// </summary>
	public decimal SlPercent { get => _slPercent.Value; set => _slPercent.Value = value; }
	
	/// <summary>
	/// Take profit risk ratio.
	/// </summary>
	public decimal RrTarget { get => _rrTarget.Value; set => _rrTarget.Value = value; }
	
	/// <summary>
	/// ATR length for SuperTrend.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	
	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	
	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	
	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	
	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	
	/// <summary>
	/// Trend EMA length.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	
	/// <summary>
	/// Cooldown bars after a signal.
	/// </summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	
	/// <summary>
	/// Keltner Channel length.
	/// </summary>
	public int KcLength { get => _kcLength.Value; set => _kcLength.Value = value; }
	
	/// <summary>
	/// Keltner Channel multiplier.
	/// </summary>
	public decimal KcMultiplier { get => _kcMultiplier.Value; set => _kcMultiplier.Value = value; }
	
	/// <summary>
	/// Use Keltner Channel basis as trend filter.
	/// </summary>
	public bool UseKcFilter { get => _useKcFilter.Value; set => _useKcFilter.Value = value; }
	
	/// <summary>
	/// Max risk percentage of capital.
	/// </summary>
	public decimal RiskPctCap { get => _riskPctCap.Value; set => _riskPctCap.Value = value; }
	
	/// <summary>
	/// Lot size.
	/// </summary>
	public decimal LotSize { get => _lotSize.Value; set => _lotSize.Value = value; }
	
	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initialize <see cref="SjNiftyStrategy"/>.
	/// </summary>
	public SjNiftyStrategy()
	{
		_slPercent = Param(nameof(SlPercent), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Technical stop loss percent from entry", "Risk Management");
		
		_rrTarget = Param(nameof(RrTarget), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("RR Target", "Take profit risk ratio", "Risk Management");
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR length for SuperTrend", "SuperTrend");
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "ATR multiplier for SuperTrend", "SuperTrend");
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI length", "RSI");
		
		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
		.SetGreaterThanZero()
		.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");
		
		_rsiOversold = Param(nameof(RsiOversold), 30m)
		.SetGreaterThanZero()
		.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");
		
		_emaLength = Param(nameof(EmaLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "Trend EMA length", "Trend");
		
		_cooldownBars = Param(nameof(CooldownBars), 8)
		.SetDisplay("Cooldown Bars", "Cooldown bars after signal", "General");
		
		_kcLength = Param(nameof(KcLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("KC Length", "Keltner Channel length", "Keltner");
		
		_kcMultiplier = Param(nameof(KcMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("KC Multiplier", "Keltner Channel multiplier", "Keltner");
		
		_useKcFilter = Param(nameof(UseKcFilter), true)
		.SetDisplay("Use KC Filter", "Use Keltner Channel basis as filter", "Keltner");
		
		_riskPctCap = Param(nameof(RiskPctCap), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Risk % of Capital", "Max risk percent of capital", "Risk Management");
		
		_lotSize = Param(nameof(LotSize), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Size", "Quantity step", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevHigh = 0m;
		_prevLow = 0m;
		_prevLongCond = false;
		_prevShortCond = false;
		_barIndex = 0;
		_lastSignalIndex = int.MinValue;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var supertrend = new SuperTrend { Length = AtrLength, Multiplier = AtrMultiplier };
		var vwap = new VolumeWeightedMovingAverage();
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var kc = new KeltnerChannels { Length = KcLength, Multiplier = KcMultiplier };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx([supertrend, vwap, rsi, ema, kc], ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawIndicator(area, vwap);
			DrawIndicator(area, ema);
			DrawIndicator(area, kc);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (values[0].ToNullableDecimal() is not decimal supertrendValue)
		return;
		if (values[1].ToNullableDecimal() is not decimal vwapValue)
		return;
		if (values[2].ToNullableDecimal() is not decimal rsiValue)
		return;
		if (values[3].ToNullableDecimal() is not decimal emaValue)
		return;
		var kcValue = (KeltnerChannelsValue)values[4];
		if (kcValue.Middle is not decimal kcBasis)
		return;
		
		_barIndex++;
		
		if (_prevHigh == 0m)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}
		
		var upTrend = candle.ClosePrice > emaValue;
		var downTrend = candle.ClosePrice < emaValue;
		var kcLongOk = !UseKcFilter || candle.ClosePrice >= kcBasis;
		var kcShortOk = !UseKcFilter || candle.ClosePrice <= kcBasis;
		
		var longCondRaw = candle.ClosePrice > supertrendValue && candle.ClosePrice > vwapValue && rsiValue > RsiOverbought && upTrend && kcLongOk && candle.ClosePrice > _prevHigh;
		var shortCondRaw = candle.ClosePrice < supertrendValue && candle.ClosePrice < vwapValue && rsiValue < RsiOversold && downTrend && kcShortOk && candle.ClosePrice < _prevLow;
		
		var longTrig = longCondRaw && !_prevLongCond;
		var shortTrig = shortCondRaw && !_prevShortCond;
		
		var canSignal = (_barIndex - _lastSignalIndex) > CooldownBars;
		var longSignal = longTrig && canSignal;
		var shortSignal = shortTrig && canSignal;
		
		if (longSignal || shortSignal)
		_lastSignalIndex = _barIndex;
		
		if (longSignal)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice * (1m - SlPercent / 100m);
			var riskPerUnit = _entryPrice - _stopPrice;
			var cap = Portfolio?.CurrentValue ?? 0m;
			var riskBudget = cap * (RiskPctCap / 100m);
			var qty = riskPerUnit > 0m ? Math.Floor((riskBudget / riskPerUnit) / LotSize) * LotSize : 0m;
			if (qty <= 0m)
			qty = LotSize;
			_targetPrice = _entryPrice + riskPerUnit * RrTarget;
			BuyMarket(qty + Math.Abs(Position));
		}
		else if (shortSignal)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice * (1m + SlPercent / 100m);
			var riskPerUnit = _stopPrice - _entryPrice;
			var cap = Portfolio?.CurrentValue ?? 0m;
			var riskBudget = cap * (RiskPctCap / 100m);
			var qty = riskPerUnit > 0m ? Math.Floor((riskBudget / riskPerUnit) / LotSize) * LotSize : 0m;
			if (qty <= 0m)
			qty = LotSize;
			_targetPrice = _entryPrice - riskPerUnit * RrTarget;
			SellMarket(qty + Math.Abs(Position));
		}
		
		if (Position > 0m && _entryPrice != 0m)
		{
			if (candle.ClosePrice <= _stopPrice || candle.ClosePrice >= _targetPrice)
			{
				SellMarket(Position);
				_entryPrice = 0m;
			}
		}
		else if (Position < 0m && _entryPrice != 0m)
		{
			if (candle.ClosePrice >= _stopPrice || candle.ClosePrice <= _targetPrice)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0m;
			}
		}
		
		_prevLongCond = longCondRaw;
		_prevShortCond = shortCondRaw;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
