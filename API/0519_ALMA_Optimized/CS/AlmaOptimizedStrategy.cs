using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ALMA based strategy with volatility filter and ATR exits.
/// </summary>
public class AlmaOptimizedStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _slAtrMultiplier;
	private readonly StrategyParam<decimal> _tpAtrMultiplier;
	private readonly StrategyParam<int> _timeBasedExit;
	private readonly StrategyParam<decimal> _minAtr;
	private readonly StrategyParam<DataType> _candleType;
	
	private int _barIndex;
	private int _lastBuyBar;
	private int _entryBar;
	private SignalType _lastSignal;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _prevClose;
	private decimal _prevFastEma;
	
	private enum SignalType
	{
		None,
		Buy,
		Sell
	}
	
	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	
	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}
	
	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}
	
	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}
	
	/// <summary>
	/// Cooldown bars after long entry.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}
	
	/// <summary>
	/// Bollinger Bands multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}
	
	/// <summary>
	/// Stop loss ATR multiplier.
	/// </summary>
	public decimal SlAtrMultiplier
	{
		get => _slAtrMultiplier.Value;
		set => _slAtrMultiplier.Value = value;
	}
	
	/// <summary>
	/// Take profit ATR multiplier.
	/// </summary>
	public decimal TpAtrMultiplier
	{
		get => _tpAtrMultiplier.Value;
		set => _tpAtrMultiplier.Value = value;
	}
	
	/// <summary>
	/// Exit after N bars.
	/// </summary>
	public int TimeBasedExit
	{
		get => _timeBasedExit.Value;
		set => _timeBasedExit.Value = value;
	}
	
	/// <summary>
	/// Minimum ATR for volatility filter.
	/// </summary>
	public decimal MinAtr
	{
		get => _minAtr.Value;
		set => _minAtr.Value = value;
	}
	
	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="AlmaOptimizedStrategy"/>.
	/// </summary>
	public AlmaOptimizedStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Length", "Fast EMA period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(7, 21, 7);
		
		_emaLength = Param(nameof(EmaLength), 72)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "Slow EMA period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(50, 100, 10);
		
		_adxLength = Param(nameof(AdxLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("ADX Length", "ADX period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(7, 21, 7);
		
		_cooldownBars = Param(nameof(CooldownBars), 7)
		.SetGreaterThanZero()
		.SetDisplay("Cooldown Bars", "Bars to wait after long entry", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(3, 15, 2);
		
		_bbMultiplier = Param(nameof(BbMultiplier), 3m)
		.SetGreaterThanZero()
		.SetDisplay("BB Multiplier", "Bollinger Bands deviation", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);
		
		_slAtrMultiplier = Param(nameof(SlAtrMultiplier), 5m)
		.SetGreaterThanZero()
		.SetDisplay("SL ATR Mult", "ATR multiplier for stop loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2m, 8m, 1m);
		
		_tpAtrMultiplier = Param(nameof(TpAtrMultiplier), 4m)
		.SetGreaterThanZero()
		.SetDisplay("TP ATR Mult", "ATR multiplier for take profit", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2m, 8m, 1m);
		
		_timeBasedExit = Param(nameof(TimeBasedExit), 0)
		.SetDisplay("Time Based Exit", "Exit after N bars (0 disabled)", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0, 20, 5);
		
		_minAtr = Param(nameof(MinAtr), 0.005m)
		.SetGreaterThanZero()
		.SetDisplay("Min ATR", "Minimum ATR value", "Filter")
		.SetCanOptimize(true)
		.SetOptimize(0.001m, 0.01m, 0.001m);
		
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
		
		_barIndex = 0;
		_lastBuyBar = int.MinValue;
		_entryBar = 0;
		_lastSignal = SignalType.None;
		_stopPrice = 0m;
		_takePrice = 0m;
		_prevClose = 0m;
		_prevFastEma = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = EmaLength };
		var alma = new ALMA { Length = 15, Offset = 0.65m, Sigma = 6 };
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var bollinger = new BollingerBands { Length = 20, Width = BbMultiplier };
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(fastEma, slowEma, alma, adx, rsi, atr, bollinger, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, alma);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue, decimal almaValue, decimal adxValue, decimal rsiValue, decimal atrValue, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_barIndex++;
		
		var volatility = atrValue > MinAtr;
		
		if (_prevClose != 0m || _prevFastEma != 0m)
		{
			var buyCond = volatility && candle.ClosePrice > slowEmaValue && candle.ClosePrice > almaValue && rsiValue > 30m && adxValue > 30m && candle.ClosePrice < upperBand && (_barIndex - _lastBuyBar > CooldownBars) && _lastSignal != SignalType.Buy;
			
			var crossUnder = _prevClose >= _prevFastEma && candle.ClosePrice < fastEmaValue;
			var sellCond = volatility && crossUnder && _lastSignal != SignalType.Sell;
			
			if (buyCond && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_lastBuyBar = _barIndex;
				_entryBar = _barIndex;
				_lastSignal = SignalType.Buy;
				_stopPrice = candle.ClosePrice - atrValue * SlAtrMultiplier;
				_takePrice = candle.ClosePrice + atrValue * TpAtrMultiplier;
			}
		else if (sellCond && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryBar = _barIndex;
			_lastSignal = SignalType.Sell;
			_stopPrice = candle.ClosePrice + atrValue * SlAtrMultiplier;
			_takePrice = candle.ClosePrice - atrValue * TpAtrMultiplier;
		}
	}
	
	if (Position > 0)
	{
		if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice || (TimeBasedExit > 0 && _barIndex - _entryBar >= TimeBasedExit))
		{
			SellMarket(Position);
			_lastSignal = SignalType.Sell;
		}
	else if (Position < 0)
	{
		if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice || (TimeBasedExit > 0 && _barIndex - _entryBar >= TimeBasedExit))
		{
			BuyMarket(Math.Abs(Position));
			_lastSignal = SignalType.Buy;
		}
	}

	_prevClose = candle.ClosePrice;
	_prevFastEma = fastEmaValue;
}
}
