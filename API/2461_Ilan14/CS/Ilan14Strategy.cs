using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class Ilan14Strategy : Strategy {
	private readonly StrategyParam<decimal> _pipStep;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastBuyPrice;
	private decimal _lastSellPrice;
	private decimal _lastBuyVolume;
	private decimal _lastSellVolume;
	private decimal _buyVolume;
	private decimal _sellVolume;
	private decimal _avgBuyPrice;
	private decimal _avgSellPrice;
	private int _buyCount;
	private int _sellCount;

	public decimal PipStep {
	get => _pipStep.Value;
	set => _pipStep.Value = value;
	}
	public decimal LotExponent {
	get => _lotExponent.Value;
	set => _lotExponent.Value = value;
	}
	public int MaxTrades {
	get => _maxTrades.Value;
	set => _maxTrades.Value = value;
	}
	public decimal TakeProfit {
	get => _takeProfit.Value;
	set => _takeProfit.Value = value;
	}
	public decimal InitialVolume {
	get => _initialVolume.Value;
	set => _initialVolume.Value = value;
	}
	public DataType CandleType {
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	public Ilan14Strategy() {
	_pipStep =
		Param(nameof(PipStep), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Pip Step", "Distance in pips to add position",
				"General")
		.SetCanOptimize();
	_lotExponent =
		Param(nameof(LotExponent), 1.667m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Exponent",
				"Volume multiplier for each additional order",
				"General")
		.SetCanOptimize();
	_maxTrades = Param(nameof(MaxTrades), 10)
			 .SetGreaterThanZero()
			 .SetDisplay("Max Trades",
					 "Maximum number of trades per direction",
					 "General");
	_takeProfit =
		Param(nameof(TakeProfit), 96m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit",
				"Target profit in pips from average price",
				"General")
		.SetCanOptimize();
	_initialVolume = Param(nameof(InitialVolume), 0.1m)
				 .SetGreaterThanZero()
				 .SetDisplay("Initial Volume",
					 "Volume of first order", "General")
				 .SetCanOptimize();
	_candleType =
		Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted() {
	base.OnReseted();
	_lastBuyPrice = 0m;
	_lastSellPrice = 0m;
	_lastBuyVolume = 0m;
	_lastSellVolume = 0m;
	_buyVolume = 0m;
	_sellVolume = 0m;
	_avgBuyPrice = 0m;
	_avgSellPrice = 0m;
	_buyCount = 0;
	_sellCount = 0;
	}

	protected override void OnStarted(DateTimeOffset time) {
	base.OnStarted(time);
	StartProtection();

	var sub = SubscribeCandles(CandleType);
	sub.Bind(Process).Start();
	}

	private void Process(ICandleMessage candle) {
	if (candle.State != CandleStates.Finished)
		return;

	if (!IsFormedAndOnlineAndAllowTrading())
		return;

	var step = Security.PriceStep ?? 1m;
	var price = candle.ClosePrice;

	if (_buyCount == 0 && _sellCount == 0) {
		// Open initial hedge positions
		BuyMarket(InitialVolume);
		SellMarket(InitialVolume);
		_lastBuyPrice = price;
		_lastSellPrice = price;
		_lastBuyVolume = InitialVolume;
		_lastSellVolume = InitialVolume;
		_buyVolume = InitialVolume;
		_sellVolume = InitialVolume;
		_avgBuyPrice = price;
		_avgSellPrice = price;
		_buyCount = 1;
		_sellCount = 1;
		return;
	}

	// --- Buy side management ---
	if (_buyCount > 0) {
		if (_buyCount < MaxTrades &&
		price <= _lastBuyPrice - PipStep * step) {
		var vol = _lastBuyVolume * LotExponent;
		BuyMarket(vol);
		_lastBuyVolume = vol;
		_lastBuyPrice = price;
		_avgBuyPrice = (_avgBuyPrice * _buyVolume + price * vol) /
				   (_buyVolume + vol);
		_buyVolume += vol;
		_buyCount++;
		}

		if (_buyVolume > 0 && price >= _avgBuyPrice + TakeProfit * step) {
		// Close all buy positions when profit target is reached
		SellMarket(_buyVolume);
		_buyVolume = 0m;
		_lastBuyPrice = 0m;
		_lastBuyVolume = 0m;
		_avgBuyPrice = 0m;
		_buyCount = 0;
		}
	}

	// --- Sell side management ---
	if (_sellCount > 0) {
		if (_sellCount < MaxTrades &&
		price >= _lastSellPrice + PipStep * step) {
		var vol = _lastSellVolume * LotExponent;
		SellMarket(vol);
		_lastSellVolume = vol;
		_lastSellPrice = price;
		_avgSellPrice = (_avgSellPrice * _sellVolume + price * vol) /
				(_sellVolume + vol);
		_sellVolume += vol;
		_sellCount++;
		}

		if (_sellVolume > 0 && price <= _avgSellPrice - TakeProfit * step) {
		// Close all sell positions when profit target is reached
		BuyMarket(_sellVolume);
		_sellVolume = 0m;
		_lastSellPrice = 0m;
		_lastSellVolume = 0m;
		_avgSellPrice = 0m;
		_sellCount = 0;
		}
	}
	}
}
