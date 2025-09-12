using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend strategy with volume, body and RSI filters plus ATR based TP/SL.
/// </summary>
public class SupertrendHombrokBotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _bodyPctOfAtr;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<decimal> _capitalPerTrade;

	private SimpleMovingAverage _volumeSma;
	private AverageTrueRange _atr;
	private RelativeStrengthIndex _rsi;
	private SuperTrend _supertrend;
	private decimal _stopLevel;
	private decimal _tpLevel;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }
	public decimal BodyPctOfAtr { get => _bodyPctOfAtr.Value; set => _bodyPctOfAtr.Value = value; }
	public decimal RiskRewardRatio { get => _riskRewardRatio.Value; set => _riskRewardRatio.Value = value; }
	public decimal CapitalPerTrade { get => _capitalPerTrade.Value; set => _capitalPerTrade.Value = value; }

	public SupertrendHombrokBotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Period", "ATR period", "Supertrend");
		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetDisplay("ATR Multiplier", "ATR factor", "Supertrend");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI period", "RSI");
		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought", "RSI");
		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold", "RSI");
		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.2m)
			.SetDisplay("Volume Multiplier", "Volume filter", "Filters");
		_bodyPctOfAtr = Param(nameof(BodyPctOfAtr), 0.3m)
			.SetDisplay("Body % of ATR", "Minimum body size", "Filters");
		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
			.SetDisplay("Risk Reward", "TP/SL ratio", "Risk");
		_capitalPerTrade = Param(nameof(CapitalPerTrade), 10m)
			.SetDisplay("Capital", "Capital per trade", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_stopLevel = 0m;
		_tpLevel = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = AtrMultiplier };
		_volumeSma = new SimpleMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_atr, _rsi, _supertrend, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrVal, IIndicatorValue rsiVal, IIndicatorValue stVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atr = atrVal.ToDecimal();
		var rsi = rsiVal.ToDecimal();
		var volAvg = _volumeSma.Process(candle.TotalVolume ?? 0m, candle.OpenTime, true).ToDecimal();
		var volOk = candle.TotalVolume > volAvg * VolumeMultiplier;
		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var bodyOk = bodySize > atr * BodyPctOfAtr;
		var st = (SuperTrendIndicatorValue)stVal;
		var isUp = st.IsUpTrend;

		var buyCond = isUp && volOk && bodyOk && rsi < RsiOverbought;
		var sellCond = !isUp && volOk && bodyOk && rsi > RsiOversold;

		if (buyCond)
		{
			var qty = CapitalPerTrade / candle.ClosePrice;
			BuyMarket(qty);
			_stopLevel = candle.ClosePrice - atr;
			_tpLevel = candle.ClosePrice + atr * RiskRewardRatio;
		}
		else if (sellCond)
		{
			var qty = CapitalPerTrade / candle.ClosePrice;
			SellMarket(qty);
			_stopLevel = candle.ClosePrice + atr;
			_tpLevel = candle.ClosePrice - atr * RiskRewardRatio;
		}

		if (Position > 0 && (_stopLevel > 0m || _tpLevel > 0m))
		{
			if (candle.LowPrice <= _stopLevel || candle.HighPrice >= _tpLevel)
			{
				SellMarket(Position);
				_stopLevel = 0m;
				_tpLevel = 0m;
			}
		}
		else if (Position < 0 && (_stopLevel > 0m || _tpLevel > 0m))
		{
			if (candle.HighPrice >= _stopLevel || candle.LowPrice <= _tpLevel)
			{
				BuyMarket(Math.Abs(Position));
				_stopLevel = 0m;
				_tpLevel = 0m;
			}
		}
	}
}
