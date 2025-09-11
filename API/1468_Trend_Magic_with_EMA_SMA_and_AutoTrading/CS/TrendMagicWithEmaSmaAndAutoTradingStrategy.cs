using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class TrendMagicWithEmaSmaAndAutoTradingStrategy : Strategy {
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _magicTrend;
	private bool _prevIsBlue;
	private decimal? _longSma90;
	private decimal? _shortSma90;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	public int CciPeriod {
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}
	public decimal AtrMultiplier {
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	public int AtrPeriod {
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}
	public decimal RiskReward {
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TrendMagicWithEmaSmaAndAutoTradingStrategy() {
		_cciPeriod =
			Param(nameof(CciPeriod), 21)
				.SetGreaterThanZero()
				.SetDisplay("CCI Period", "Commodity Channel Index period",
							"Parameters");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.0m)
							 .SetGreaterThanZero()
							 .SetDisplay("ATR Multiplier", "Multiplier for ATR",
										 "Parameters");

		_atrPeriod =
			Param(nameof(AtrPeriod), 7)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for Average True Range",
							"Parameters");

		_riskReward =
			Param(nameof(RiskReward), 1.5m)
				.SetGreaterThanZero()
				.SetDisplay("Risk Reward", "Risk reward ratio", "Parameters");

		_candleType =
			Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
		return [(Security, CandleType)];
	}

	protected override void OnReseted() {
		base.OnReseted();
		_magicTrend = 0m;
		_prevIsBlue = false;
		_longSma90 = null;
		_shortSma90 = null;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
	}

	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var ema45 = new ExponentialMovingAverage { Length = 45 };
		var sma90 = new SimpleMovingAverage { Length = 90 };
		var sma180 = new SimpleMovingAverage { Length = 180 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, atr, ema45, sma90, sma180, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawIndicator(area, ema45);
			DrawIndicator(area, sma90);
			DrawIndicator(area, sma180);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci, decimal atr,
							   decimal ema45, decimal sma90, decimal sma180) {
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var up = candle.LowPrice - atr * AtrMultiplier;
		var down = candle.HighPrice + atr * AtrMultiplier;

		_magicTrend =
			cci >= 0 ? Math.Max(up, _magicTrend)
					 : Math.Min(down, _magicTrend == 0m ? down : _magicTrend);

		var isBlue = cci >= 0;
		var turnedBlue = !_prevIsBlue && isBlue;
		var turnedRed = _prevIsBlue && !isBlue;
		_prevIsBlue = isBlue;

		var bullishOrder = ema45 > sma90 && sma90 > sma180;
		var bearishOrder = ema45 < sma90 && sma90 < sma180;

		var longCondition = bullishOrder && turnedBlue;
		var shortCondition = bearishOrder && turnedRed;

		if (longCondition && Position <= 0) {
			BuyMarket(Volume + Math.Abs(Position));
			_longSma90 = sma90;
			_longStop = _longSma90.Value;
			var risk = candle.ClosePrice - _longSma90.Value;
			_longTake = candle.ClosePrice + risk * RiskReward;
		} else if (shortCondition && Position >= 0) {
			SellMarket(Volume + Math.Abs(Position));
			_shortSma90 = sma90;
			_shortStop = _shortSma90.Value;
			var risk = _shortSma90.Value - candle.ClosePrice;
			_shortTake = candle.ClosePrice - risk * RiskReward;
		}

		if (Position > 0) {
			if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTake)
				SellMarket(Math.Abs(Position));
		} else if (Position < 0) {
			if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake)
				BuyMarket(Math.Abs(Position));
		}
	}
}
