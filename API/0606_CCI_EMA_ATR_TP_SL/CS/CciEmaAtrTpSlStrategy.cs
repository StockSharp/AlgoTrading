using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI + EMA strategy with optional RSI filter and percentage or ATR based TP/SL.
/// </summary>
public class CciEmaAtrTpSlStrategy : Strategy
{
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<decimal> _cciOverbought;
	private readonly StrategyParam<decimal> _cciOversold;

	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;

	private readonly StrategyParam<bool> _useEma;
	private readonly StrategyParam<int> _emaLength;

	private readonly StrategyParam<bool> _tpSlMethodPercentage;
	private readonly StrategyParam<bool> _tpSlMethodAtr;
	private readonly StrategyParam<decimal> _tpPercentage;
	private readonly StrategyParam<decimal> _slPercentage;

	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskRewardRatio;

	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCci;
	private bool _cciInitialized;
	private decimal? _longTp;
	private decimal? _longSl;
	private decimal? _shortTp;
	private decimal? _shortSl;

	public int CciLength { get => _cciLength.Value; set => _cciLength.Value = value; }
	public decimal CciOverbought { get => _cciOverbought.Value; set => _cciOverbought.Value = value; }
	public decimal CciOversold { get => _cciOversold.Value; set => _cciOversold.Value = value; }

	public bool UseRsi { get => _useRsi.Value; set => _useRsi.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	public bool UseEma { get => _useEma.Value; set => _useEma.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	public bool TpSlMethodPercentage { get => _tpSlMethodPercentage.Value; set => _tpSlMethodPercentage.Value = value; }
	public bool TpSlMethodAtr { get => _tpSlMethodAtr.Value; set => _tpSlMethodAtr.Value = value; }
	public decimal TpPercentage { get => _tpPercentage.Value; set => _tpPercentage.Value = value; }
	public decimal SlPercentage { get => _slPercentage.Value; set => _slPercentage.Value = value; }

	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal RiskRewardRatio { get => _riskRewardRatio.Value; set => _riskRewardRatio.Value = value; }

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CciEmaAtrTpSlStrategy()
	{
		_cciLength = Param(nameof(CciLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Length", "CCI period length", "CCI");

		_cciOverbought = Param(nameof(CciOverbought), 150m)
		.SetDisplay("Overbought", "CCI overbought level", "CCI");

		_cciOversold = Param(nameof(CciOversold), -140m)
		.SetDisplay("Oversold", "CCI oversold level", "CCI");

		_useRsi = Param(nameof(UseRsi), false)
		.SetDisplay("Use RSI", "Enable RSI filter", "RSI");

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI period length", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
		.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
		.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		_useEma = Param(nameof(UseEma), true)
		.SetDisplay("Use EMA", "Enable EMA filter", "EMA");

		_emaLength = Param(nameof(EmaLength), 55)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "EMA period length", "EMA");

		_tpSlMethodPercentage = Param(nameof(TpSlMethodPercentage), true)
		.SetDisplay("Percentage TP/SL", "Use percentage take-profit and stop-loss", "TP/SL");

		_tpSlMethodAtr = Param(nameof(TpSlMethodAtr), false)
		.SetDisplay("ATR TP/SL", "Use ATR based take-profit and stop-loss", "TP/SL");

		_tpPercentage = Param(nameof(TpPercentage), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit percent", "TP/SL");

		_slPercentage = Param(nameof(SlPercentage), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss percent", "TP/SL");

		_atrLength = Param(nameof(AtrLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR period length", "TP/SL");

		_atrMultiplier = Param(nameof(AtrMultiplier), 4m)
		.SetGreaterThanZero()
		.SetDisplay("ATR SL Multiplier", "Multiplier for ATR stop-loss", "TP/SL");

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Reward Ratio", "Risk reward ratio", "TP/SL");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCci = 0m;
		_cciInitialized = false;
		_longTp = _longSl = _shortTp = _shortSl = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var cci = new CommodityChannelIndex { Length = CciLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(cci, rsi, ema, atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			if (UseRsi)
			DrawIndicator(area, rsi);
			if (UseEma)
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}

		void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal rsiValue, decimal emaValue, decimal atrValue)
		{
			if (candle.State != CandleStates.Finished)
			return;

			if (!cci.IsFormed || (UseRsi && !rsi.IsFormed) || (UseEma && !ema.IsFormed) || (TpSlMethodAtr && !atr.IsFormed))
			return;

			if (!IsFormedAndOnlineAndAllowTrading())
			return;

			if (!_cciInitialized)
			{
				_prevCci = cciValue;
				_cciInitialized = true;
				return;
			}

			var longSignal = _prevCci <= CciOversold && cciValue > CciOversold &&
			(!UseEma || candle.ClosePrice > emaValue) &&
			(!UseRsi || rsiValue < RsiOversold);

			var shortSignal = _prevCci >= CciOverbought && cciValue < CciOverbought &&
			(!UseEma || candle.ClosePrice < emaValue) &&
			(!UseRsi || rsiValue > RsiOverbought);

			if (longSignal && Position <= 0)
			{
				var entryPrice = candle.ClosePrice;
				BuyMarket(Volume + Math.Abs(Position));

				if (TpSlMethodPercentage)
				{
					_longTp = entryPrice * (1m + TpPercentage / 100m);
					_longSl = entryPrice * (1m - SlPercentage / 100m);
				}
				else if (TpSlMethodAtr)
				{
					var sl = entryPrice - atrValue * AtrMultiplier;
					_longSl = sl;
					_longTp = (entryPrice - sl) * RiskRewardRatio + entryPrice;
				}
			}
			else if (shortSignal && Position >= 0)
			{
				var entryPrice = candle.ClosePrice;
				SellMarket(Volume + Math.Abs(Position));

				if (TpSlMethodPercentage)
				{
					_shortTp = entryPrice * (1m - TpPercentage / 100m);
					_shortSl = entryPrice * (1m + SlPercentage / 100m);
				}
				else if (TpSlMethodAtr)
				{
					var sl = entryPrice + atrValue * AtrMultiplier;
					_shortSl = sl;
					_shortTp = entryPrice - (sl - entryPrice) * RiskRewardRatio;
				}
			}

			if (Position > 0)
			{
				if (_longTp.HasValue && candle.HighPrice >= _longTp.Value)
				{
					SellMarket(Math.Abs(Position));
					_longTp = _longSl = null;
				}
				else if (_longSl.HasValue && candle.LowPrice <= _longSl.Value)
				{
					SellMarket(Math.Abs(Position));
					_longTp = _longSl = null;
				}
				else if (_prevCci < CciOverbought && cciValue >= CciOverbought)
				{
					SellMarket(Math.Abs(Position));
					_longTp = _longSl = null;
				}
			}
			else if (Position < 0)
			{
				if (_shortTp.HasValue && candle.LowPrice <= _shortTp.Value)
				{
					BuyMarket(Math.Abs(Position));
					_shortTp = _shortSl = null;
				}
				else if (_shortSl.HasValue && candle.HighPrice >= _shortSl.Value)
				{
					BuyMarket(Math.Abs(Position));
					_shortTp = _shortSl = null;
				}
				else if (_prevCci > CciOversold && cciValue <= CciOversold)
				{
					BuyMarket(Math.Abs(Position));
					_shortTp = _shortSl = null;
				}
			}

			_prevCci = cciValue;
		}
	}
}
