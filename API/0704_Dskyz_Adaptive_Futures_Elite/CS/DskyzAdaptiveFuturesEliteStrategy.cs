using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class DskyzAdaptiveFuturesEliteStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<int> _minimumVolume;
	private readonly StrategyParam<decimal> _volatilityThreshold;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;
	private decimal _fast15;
	private decimal _slow15;
	
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public bool UseRsi { get => _useRsi.Value; set => _useRsi.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public bool UseTrendFilter { get => _useTrendFilter.Value; set => _useTrendFilter.Value = value; }
	public int MinimumVolume { get => _minimumVolume.Value; set => _minimumVolume.Value = value; }
	public decimal VolatilityThreshold { get => _volatilityThreshold.Value; set => _volatilityThreshold.Value = value; }
	public int TradingStartHour { get => _tradingStartHour.Value; set => _tradingStartHour.Value = value; }
	public int TradingEndHour { get => _tradingEndHour.Value; set => _tradingEndHour.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public DskyzAdaptiveFuturesEliteStrategy()
	{
		_fastLength = Param(nameof(FastLength), 9)
		.SetDisplay("Fast MA Length", "Period of the fast moving average", "MA Settings")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_slowLength = Param(nameof(SlowLength), 19)
		.SetDisplay("Slow MA Length", "Period of the slow moving average", "MA Settings")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 1);
		
		_useRsi = Param(nameof(UseRsi), false)
		.SetDisplay("Use RSI Filter", string.Empty, "RSI Settings");
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "RSI period", "RSI Settings")
		.SetGreaterThanZero();
		
		_rsiOverbought = Param(nameof(RsiOverbought), 60m)
		.SetDisplay("RSI Overbought", string.Empty, "RSI Settings");
		
		_rsiOversold = Param(nameof(RsiOversold), 40m)
		.SetDisplay("RSI Oversold", string.Empty, "RSI Settings");
		
		_useTrendFilter = Param(nameof(UseTrendFilter), true)
		.SetDisplay("Use 15m Trend Filter", string.Empty, "Filter Settings");
		
		_minimumVolume = Param(nameof(MinimumVolume), 10)
		.SetDisplay("Minimum Volume", string.Empty, "Filter Settings")
		.SetGreaterThanZero();
		
		_volatilityThreshold = Param(nameof(VolatilityThreshold), 0.01m)
		.SetDisplay("Volatility Threshold", "ATR/Close limit", "Filter Settings")
		.SetGreaterThanZero();
		
		_tradingStartHour = Param(nameof(TradingStartHour), 9)
		.SetDisplay("Trading Start Hour", string.Empty, "Filter Settings");
		
		_tradingEndHour = Param(nameof(TradingEndHour), 16)
		.SetDisplay("Trading End Hour", string.Empty, "Filter Settings");
		
		_atrPeriod = Param(nameof(AtrPeriod), 7)
		.SetDisplay("ATR Period", string.Empty, "General")
		.SetGreaterThanZero();
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromMinutes(15).TimeFrame())];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
		_fast15 = 0m;
		_slow15 = 0m;
		_isInitialized = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };
		var rsi = new RSI { Length = RsiLength };
		var atr = new ATR { Length = AtrPeriod };
		
		var fastMa15 = new SMA { Length = FastLength };
		var slowMa15 = new SMA { Length = SlowLength };
		
		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.Bind(fastMa, slowMa, rsi, atr, ProcessCandle)
		.Start();
		
		var trendSubscription = SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame());
		trendSubscription
		.Bind(fastMa15, slowMa15, (c, fast15, slow15) =>
		{
			if (c.State != CandleStates.Finished)
			return;
			
			_fast15 = fast15;
			_slow15 = slow15;
		})
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isInitialized = true;
			return;
		}
		
		var volumeOk = candle.TotalVolume >= MinimumVolume;
		var hour = candle.OpenTime.LocalDateTime.Hour;
		var timeOk = hour >= TradingStartHour && hour <= TradingEndHour;
		var volatility = atrValue / candle.ClosePrice;
		var volatilityOk = volatility <= VolatilityThreshold;
		
		var trend = _fast15 > _slow15 ? 1 : _fast15 < _slow15 ? -1 : 0;
		
		var longSignal = fast > slow && _prevFast <= _prevSlow;
		var shortSignal = fast < slow && _prevFast >= _prevSlow;
		
		var rsiLongOk = !UseRsi || rsiValue <= RsiOversold;
		var rsiShortOk = !UseRsi || rsiValue >= RsiOverbought;
		var trendLongOk = !UseTrendFilter || trend > 0;
		var trendShortOk = !UseTrendFilter || trend < 0;
		
		var canTrade = volumeOk && timeOk && volatilityOk;
		
		if (longSignal && canTrade && rsiLongOk && trendLongOk && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		
		if (shortSignal && canTrade && rsiShortOk && trendShortOk && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));
		
		_prevFast = fast;
		_prevSlow = slow;
	}
}
