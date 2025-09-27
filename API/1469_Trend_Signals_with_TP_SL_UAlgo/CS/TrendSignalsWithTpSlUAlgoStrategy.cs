using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class TrendSignalsWithTpSlUAlgoStrategy : Strategy {
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _up;
	private decimal _dn;
	private int _trend;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	public decimal Multiplier {
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}
	public int AtrPeriod {
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}
	public decimal TpMultiplier {
		get => _tpMultiplier.Value;
		set => _tpMultiplier.Value = value;
	}
	public decimal SlMultiplier {
		get => _slMultiplier.Value;
		set => _slMultiplier.Value = value;
	}
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TrendSignalsWithTpSlUAlgoStrategy() {
		_multiplier =
			Param(nameof(Multiplier), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Sensitivity", "ATR sensitivity", "Parameters");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
						 .SetGreaterThanZero()
						 .SetDisplay("ATR Length", "ATR period", "Parameters");

		_tpMultiplier = Param(nameof(TpMultiplier), 2m)
							.SetGreaterThanZero()
							.SetDisplay("ATR TP Multiplier",
										"Take profit multiplier", "Parameters");

		_slMultiplier = Param(nameof(SlMultiplier), 1m)
							.SetGreaterThanZero()
							.SetDisplay("ATR SL Multiplier",
										"Stop loss multiplier", "Parameters");

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
		_up = 0m;
		_dn = 0m;
		_trend = 1;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
	}

	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr) {
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var src = (candle.HighPrice + candle.LowPrice) / 2m;
		var prevClose = candle.ClosePrice; // using current close for simplicity

		var up = src - Multiplier * atr;
		if (prevClose > _up)
			up = Math.Max(up, _up);

		var dn = src + Multiplier * atr;
		if (prevClose < _dn)
			dn = Math.Min(dn, _dn);

		var newTrend = _trend;
		if (_trend == -1 && candle.ClosePrice > _dn)
			newTrend = 1;
		else if (_trend == 1 && candle.ClosePrice < _up)
			newTrend = -1;

		var buySignal = newTrend == 1 && _trend == -1;
		var sellSignal = newTrend == -1 && _trend == 1;

		_up = up;
		_dn = dn;
		_trend = newTrend;

		if (buySignal && Position <= 0) {
			BuyMarket(Volume + Math.Abs(Position));
			_longStop = candle.ClosePrice - atr * SlMultiplier;
			_longTake = candle.ClosePrice + atr * TpMultiplier;
		} else if (sellSignal && Position >= 0) {
			SellMarket(Volume + Math.Abs(Position));
			_shortStop = candle.ClosePrice + atr * SlMultiplier;
			_shortTake = candle.ClosePrice - atr * TpMultiplier;
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
