using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale strategy for Bitcoin with optional entry signals.
/// </summary>
public class InwCoinMartingaleStrategy : Strategy {
	private enum StartLogic { MacdLine, StochasticRsi, AtrChannel }

	private readonly StrategyParam<StartLogic> _startLogic;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _martingalePercent;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _avgPrice;
	private int _martingaleCount;
	private decimal _prevMacd;
	private decimal _prevD;
	private bool _isFirst;

	/// <summary>
	/// Entry logic selector.
	/// </summary>
	public StartLogic EntryLogic {
	  get => _startLogic.Value;
	  set => _startLogic.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent {
	  get => _takeProfitPercent.Value;
	  set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Percent drop to trigger martingale.
	/// </summary>
	public decimal MartingalePercent {
	  get => _martingalePercent.Value;
	  set => _martingalePercent.Value = value;
	}

	/// <summary>
	/// Multiplier for each martingale step.
	/// </summary>
	public decimal MartingaleMultiplier {
	  get => _martingaleMultiplier.Value;
	  set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType {
	  get => _candleType.Value;
	  set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="InwCoinMartingaleStrategy"/>.
	/// </summary>
	public InwCoinMartingaleStrategy() {
	  _startLogic =
	Param(nameof(EntryLogic), StartLogic.MacdLine)
	    .SetDisplay("Start Logic", "Entry signal selector", "Parameters");

	  _takeProfitPercent =
	Param(nameof(TakeProfitPercent), 5m)
	    .SetRange(1m, 20m)
	    .SetDisplay("Take Profit %", "Profit percent to exit",
			"Parameters");

	  _martingalePercent =
	Param(nameof(MartingalePercent), 10m)
	    .SetRange(1m, 50m)
	    .SetDisplay("Martingale %", "Price drop percent for averaging",
			"Parameters");

	  _martingaleMultiplier =
	Param(nameof(MartingaleMultiplier), 2m)
	    .SetRange(1m, 5m)
	    .SetDisplay("Multiplier", "Volume multiplier for martingale",
			"Parameters");

	  _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		      .SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
	  return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted() {
	  base.OnReseted();
	  _avgPrice = 0m;
	  _martingaleCount = 0;
	  _prevMacd = 0m;
	  _prevD = 0m;
	  _isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
	  base.OnStarted(time);

	  var macd = new MovingAverageConvergenceDivergenceSignal {
	    Macd =
	  {
	    ShortMa = { Length = 12 },
	    LongMa = { Length = 26 },
	  },
	    SignalMa = { Length = 9 }
	  };

	  var rsi = new RelativeStrengthIndex { Length = 14 };
	  var stoch = new StochasticOscillator { Length = 14, K = { Length = 3 },
					   D = { Length = 3 } };

	  var ema = new ExponentialMovingAverage { Length = 10 };
	  var atr = new AverageTrueRange { Length = 14 };

	  var subscription = SubscribeCandles(CandleType);
	  subscription.BindEx(macd, stoch, rsi, ema, atr, ProcessCandle).Start();

	  StartProtection(useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdVal,
			     IIndicatorValue stochVal, IIndicatorValue rsiVal,
			     IIndicatorValue emaVal, IIndicatorValue atrVal) {
	  if (candle.State != CandleStates.Finished)
	    return;

	  if (!IsFormedAndOnlineAndAllowTrading())
	    return;

	  var price = candle.ClosePrice;

	  bool buySignal = false;

	  switch (EntryLogic) {
	  case StartLogic.MacdLine: {
	    var macd = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
	    if (macd.Macd is decimal m && macd.Signal is decimal s) {
	var hist = m - s;
	buySignal = _prevMacd <= 0 && hist > 0;
	_prevMacd = hist;
	    }
	    break;
	  }
	  case StartLogic.StochasticRsi: {
	    var stoch = (StochasticOscillatorValue)stochVal;
	    if (stoch.D is decimal d && stoch.K is decimal k) {
	if (_isFirst) {
	  _prevD = d;
	  _isFirst = false;
	}
	buySignal = _prevD <= 20m && d > 20m && k > 20m;
	_prevD = d;
	    }
	    break;
	  }
	  case StartLogic.AtrChannel: {
	    if (emaVal.GetValue<decimal>() is decimal e && atrVal.GetValue<decimal>()
							 is decimal a) {
	var upper = e + a;
	buySignal = candle.OpenPrice <= upper && price > upper;
	    }
	    break;
	  }
	  }

	  if (buySignal && Position <= 0 && _martingaleCount == 0) {
	    BuyMarket(Volume);
	    _avgPrice = price;
	    _martingaleCount = 1;
	  } else if (Position > 0) {
	    var drop = (price - _avgPrice) / _avgPrice * 100m;
	    if (drop <= -MartingalePercent) {
	var vol = Volume * (decimal)Math.Pow((double)MartingaleMultiplier,
					     _martingaleCount);
	BuyMarket(vol);
	_avgPrice = ((_avgPrice * Position) + price * vol) / (Position + vol);
	_martingaleCount++;
	    }

	    var profit = (price - _avgPrice) / _avgPrice * 100m;
	    if (profit >= TakeProfitPercent) {
	SellMarket(Position);
	_martingaleCount = 0;
	    }
	  }
	}
}
