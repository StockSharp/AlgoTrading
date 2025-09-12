using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining zero lag MACD, Kijun-sen baseline and Ease of Movement filter.
/// </summary>
public class ZeroLagMacdKijunSenEomStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _macdEmaLength;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _eomLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private ZeroLagExponentialMovingAverage _fastZlema = null!;
	private ZeroLagExponentialMovingAverage _slowZlema = null!;
	private ExponentialMovingAverage _signalEma1 = null!;
	private ExponentialMovingAverage _signalEma2 = null!;
	private DonchianChannels _donchian = null!;
	private SimpleMovingAverage _eomSma = null!;
	private AverageTrueRange _atr = null!;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _prevMid;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private bool _entryPlaced;

	/// <summary>
	/// Fast period for zero lag MACD.
	/// </summary>
	public int FastLength
	{
	    get => _fastLength.Value;
	    set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow period for zero lag MACD.
	/// </summary>
	public int SlowLength
	{
	    get => _slowLength.Value;
	    set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal period for MACD smoothing.
	/// </summary>
	public int SignalLength
	{
	    get => _signalLength.Value;
	    set => _signalLength.Value = value;
	}

	/// <summary>
	/// EMA period applied to MACD line.
	/// </summary>
	public int MacdEmaLength
	{
	    get => _macdEmaLength.Value;
	    set => _macdEmaLength.Value = value;
	}

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
	    get => _kijunPeriod.Value;
	    set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Length for Ease of Movement.
	/// </summary>
	public int EomLength
	{
	    get => _eomLength.Value;
	    set => _eomLength.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
	    get => _atrPeriod.Value;
	    set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrMultiplier
	{
	    get => _atrMultiplier.Value;
	    set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public decimal RiskReward
	{
	    get => _riskReward.Value;
	    set => _riskReward.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZeroLagMacdKijunSenEomStrategy"/>.
	/// </summary>
	public ZeroLagMacdKijunSenEomStrategy()
	{
	    _fastLength = Param(nameof(FastLength), 12).SetDisplay("Fast Length", "Fast period for MACD", "Indicators");
	    _slowLength = Param(nameof(SlowLength), 26).SetDisplay("Slow Length", "Slow period for MACD", "Indicators");
	    _signalLength = Param(nameof(SignalLength), 9).SetDisplay("Signal Length", "Signal period", "Indicators");
	    _macdEmaLength = Param(nameof(MacdEmaLength), 9).SetDisplay("MACD EMA", "EMA length on MACD", "Indicators");
	    _kijunPeriod = Param(nameof(KijunPeriod), 26).SetDisplay("Kijun Period", "Baseline length", "Indicators");
	    _eomLength = Param(nameof(EomLength), 14).SetDisplay("EOM Length", "Ease of Movement length", "Indicators");
	    _atrPeriod = Param(nameof(AtrPeriod), 14).SetDisplay("ATR Period", "ATR calculation period", "Risk");
	    _atrMultiplier = Param(nameof(AtrMultiplier), 2.5m).SetDisplay("ATR Mult", "ATR stop multiplier", "Risk");
	    _riskReward = Param(nameof(RiskReward), 1.2m).SetDisplay("Risk/Reward", "Take profit ratio", "Risk");
	    _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    _fastZlema = new ZeroLagExponentialMovingAverage { Length = FastLength };
	    _slowZlema = new ZeroLagExponentialMovingAverage { Length = SlowLength };
	    _signalEma1 = new ExponentialMovingAverage { Length = SignalLength };
	    _signalEma2 = new ExponentialMovingAverage { Length = SignalLength };
	    _donchian = new DonchianChannels { Length = KijunPeriod };
	    _eomSma = new SimpleMovingAverage { Length = EomLength };
	    _atr = new AverageTrueRange { Length = AtrPeriod };

	    var subscription = SubscribeCandles(CandleType);
	    subscription
	        .Bind(_fastZlema, _slowZlema, _atr, _donchian, ProcessCandle)
	        .Start();

	    base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atrValue, IIndicatorValue donchianValue)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    if (!IsFormedAndOnlineAndAllowTrading())
	        return;

	    var macd = fast - slow;
	    var ema1 = _signalEma1.Process(macd, candle.CloseTime, true).ToDecimal();
	    var ema2 = _signalEma2.Process(ema1, candle.CloseTime, true).ToDecimal();
	    var signal = 2m * ema1 - ema2;

	    var dc = (DonchianChannelsValue)donchianValue;
	    if (dc.Upper is not decimal upper || dc.Lower is not decimal lower)
	        return;
	    var kijun = (upper + lower) / 2m;

	    var mid = (candle.HighPrice + candle.LowPrice) / 2m;
	    var eomRaw = candle.Volume == 0 ? 0 : (mid - _prevMid) * (candle.HighPrice - candle.LowPrice) / candle.Volume;
	    _prevMid = mid;
	    var eom = _eomSma.Process(eomRaw, candle.CloseTime, true).ToDecimal();

	    var macdCrossUp = _prevMacd <= _prevSignal && macd > signal;
	    var macdCrossDown = _prevMacd >= _prevSignal && macd < signal;
	    _prevMacd = macd;
	    _prevSignal = signal;

	    var price = candle.ClosePrice;

	    if (!_entryPlaced)
	    {
	        if (macdCrossUp && signal < 0 && price > kijun && eom > 0 && Position <= 0)
	        {
	            CancelActiveOrders();
	            var volume = Volume + Math.Abs(Position);
	            BuyMarket(volume);
	            _entryPlaced = true;
	            _stopPrice = price - atrValue * AtrMultiplier;
	            _takeProfitPrice = price + (price - _stopPrice) * RiskReward;
	        }
	        else if (macdCrossDown && signal > 0 && price < kijun && eom < 0 && Position >= 0)
	        {
	            CancelActiveOrders();
	            var volume = Volume + Math.Abs(Position);
	            SellMarket(volume);
	            _entryPlaced = true;
	            _stopPrice = price + atrValue * AtrMultiplier;
	            _takeProfitPrice = price - (_stopPrice - price) * RiskReward;
	        }
	    }
	    else
	    {
	        if (Position > 0)
	        {
	            if (candle.LowPrice <= _stopPrice)
	            {
	                SellMarket(Position);
	                _entryPlaced = false;
	            }
	            else if (candle.HighPrice >= _takeProfitPrice)
	            {
	                SellMarket(Position);
	                _entryPlaced = false;
	            }
	        }
	        else if (Position < 0)
	        {
	            if (candle.HighPrice >= _stopPrice)
	            {
	                BuyMarket(Math.Abs(Position));
	                _entryPlaced = false;
	            }
	            else if (candle.LowPrice <= _takeProfitPrice)
	            {
	                BuyMarket(Math.Abs(Position));
	                _entryPlaced = false;
	            }
	        }
	    }
	}
}
