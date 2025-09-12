namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class HarmonySignalFlowByArunStrategy : Strategy {
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _lThr, _uThr, _bSl, _bTg, _sSl,
		_sTg;
	private readonly StrategyParam<DataType> _candleType;
	private readonly RelativeStrengthIndex _rsi = new();
	private decimal? _prev;
	private bool _free = true;
	private decimal _price;

	public HarmonySignalFlowByArunStrategy() {
		_rsiPeriod =
			Param(nameof(RsiPeriod), 5)
				.SetDisplay("RSI Period", "RSI period length", "Parameters")
				.SetCanOptimize();
		_lThr = Param(nameof(LowerThreshold), 30m)
					.SetDisplay("Lower Threshold", "RSI lower threshold",
								"Parameters")
					.SetCanOptimize();
		_uThr = Param(nameof(UpperThreshold), 70m)
					.SetDisplay("Upper Threshold", "RSI upper threshold",
								"Parameters")
					.SetCanOptimize();
		_bSl = Param(nameof(BuyStopLoss), 100m)
				   .SetDisplay("Buy Stop-Loss", "Stop-loss for long positions",
							   "Risk")
				   .SetCanOptimize();
		_bTg =
			Param(nameof(BuyTarget), 150m)
				.SetDisplay("Buy Target", "Target for long positions", "Risk")
				.SetCanOptimize();
		_sSl = Param(nameof(SellStopLoss), 100m)
				   .SetDisplay("Sell Stop-Loss",
							   "Stop-loss for short positions", "Risk")
				   .SetCanOptimize();
		_sTg =
			Param(nameof(SellTarget), 150m)
				.SetDisplay("Sell Target", "Target for short positions", "Risk")
				.SetCanOptimize();
		_candleType =
			Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type",
							"Candle type for strategy calculation", "General");
	}

	public int RsiPeriod {
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}
	public decimal LowerThreshold {
		get => _lThr.Value;
		set => _lThr.Value = value;
	}
	public decimal UpperThreshold {
		get => _uThr.Value;
		set => _uThr.Value = value;
	}
	public decimal BuyStopLoss {
		get => _bSl.Value;
		set => _bSl.Value = value;
	}
	public decimal BuyTarget {
		get => _bTg.Value;
		set => _bTg.Value = value;
	}
	public decimal SellStopLoss {
		get => _sSl.Value;
		set => _sSl.Value = value;
	}
	public decimal SellTarget {
		get => _sTg.Value;
		set => _sTg.Value = value;
	}
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted() {
		base.OnReseted();
		_rsi.Reset();
		_prev = null;
		_free = true;
		_price = 0m;
	}

	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);
		_rsi.Length = RsiPeriod;
		var sub = SubscribeCandles(CandleType);
		sub.Bind(_rsi, Process).Start();
		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, sub);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage c, decimal r) {
		if (c.State != CandleStates.Finished)
			return;
		var t = c.CloseTime.LocalDateTime;
		if (t.Hour == 15 && t.Minute == 25) {
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));
			_free = true;
			_price = 0m;
			_prev = r;
			return;
		}
		if (_prev is null) {
			_prev = r;
			return;
		}
		var up = _prev <= LowerThreshold && r > LowerThreshold;
		var down = _prev >= UpperThreshold && r < UpperThreshold;
		if (Position == 0 && _free) {
			if (up) {
				BuyMarket();
				_free = false;
				_price = c.ClosePrice;
			} else if (down) {
				SellMarket();
				_free = false;
				_price = c.ClosePrice;
			}
		} else if (Position > 0) {
			var sl = _price - BuyStopLoss;
			var tg = _price + BuyTarget;
			if (c.ClosePrice <= sl || c.ClosePrice >= tg) {
				SellMarket(Position);
				_free = true;
				_price = 0m;
			}
		} else if (Position < 0) {
			var sl = _price + SellStopLoss;
			var tg = _price - SellTarget;
			if (c.ClosePrice >= sl || c.ClosePrice <= tg) {
				BuyMarket(Math.Abs(Position));
				_free = true;
				_price = 0m;
			}
		}
		_prev = r;
	}
}
