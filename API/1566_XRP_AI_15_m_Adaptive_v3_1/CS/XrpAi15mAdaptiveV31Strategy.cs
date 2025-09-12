using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XRP AI 15-m Adaptive v3.1 strategy.
/// Implements long-only entries with ATR-based management.
/// </summary>
public class XrpAi15mAdaptiveV31Strategy : Strategy
{
	private readonly StrategyParam<decimal> _riskMult;
	private readonly StrategyParam<decimal> _tpSmall;
	private readonly StrategyParam<decimal> _tpMed;
	private readonly StrategyParam<decimal> _tpLarge;
	private readonly StrategyParam<decimal> _volMult;
	private readonly StrategyParam<decimal> _trailPct;
	private readonly StrategyParam<decimal> _trailArm;
	private readonly StrategyParam<int> _maxBars;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;

	private bool _trendUp;
	private int _medWindow;
	private int _barIndex;
	private int _entryBar;
	private decimal _entryPrice;
	private decimal _highWater;
	private bool _trailLive;
	private decimal _atrEntry;
	private decimal _tpMultEntry;

	/// <summary>
	/// ATR multiplier for initial stop.
	/// </summary>
	public decimal RiskMult { get => _riskMult.Value; set => _riskMult.Value = value; }

	/// <summary>
	/// ATR multiplier for small path target.
	/// </summary>
	public decimal TpSmall { get => _tpSmall.Value; set => _tpSmall.Value = value; }

	/// <summary>
	/// ATR multiplier for medium path target.
	/// </summary>
	public decimal TpMed { get => _tpMed.Value; set => _tpMed.Value = value; }

	/// <summary>
	/// ATR multiplier for large path target.
	/// </summary>
	public decimal TpLarge { get => _tpLarge.Value; set => _tpLarge.Value = value; }

	/// <summary>
	/// Volume multiplier to detect spikes.
	/// </summary>
	public decimal VolMult { get => _volMult.Value; set => _volMult.Value = value; }

	/// <summary>
	/// Trailing stop percent of ATR.
	/// </summary>
	public decimal TrailPct { get => _trailPct.Value; set => _trailPct.Value = value; }

	/// <summary>
	/// ATR gain before trailing activates.
	/// </summary>
	public decimal TrailArm { get => _trailArm.Value; set => _trailArm.Value = value; }

	/// <summary>
	/// Maximum number of bars to hold.
	/// </summary>
	public int MaxBars { get => _maxBars.Value; set => _maxBars.Value = value; }

	/// <summary>
	/// Candle type for 15-minute calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Candle type for trend filter.
	/// </summary>
	public DataType TrendCandleType { get => _trendCandleType.Value; set => _trendCandleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="XrpAi15mAdaptiveV31Strategy"/> class.
	/// </summary>
	public XrpAi15mAdaptiveV31Strategy()
	{
		_riskMult = Param(nameof(RiskMult), 1.1m)
			.SetDisplay("Risk Mult", "ATR multiplier for stop", "Parameters")
			.SetCanOptimize();

		_tpSmall = Param(nameof(TpSmall), 2.5m)
			.SetDisplay("Small TP", "ATR multiplier for small target", "Parameters")
			.SetCanOptimize();

		_tpMed = Param(nameof(TpMed), 3.5m)
			.SetDisplay("Med TP", "ATR multiplier for medium target", "Parameters")
			.SetCanOptimize();

		_tpLarge = Param(nameof(TpLarge), 5m)
			.SetDisplay("Large TP", "ATR multiplier for large target", "Parameters")
			.SetCanOptimize();

		_volMult = Param(nameof(VolMult), 5m)
			.SetDisplay("Volume Mult", "Volume spike multiplier", "Parameters")
			.SetCanOptimize();

		_trailPct = Param(nameof(TrailPct), 0.6m)
			.SetDisplay("Trail Percent", "Trailing stop percent of ATR", "Parameters")
			.SetCanOptimize();

		_trailArm = Param(nameof(TrailArm), 1m)
			.SetDisplay("Trail Arm", "ATR gain before trailing", "Parameters")
			.SetCanOptimize();

		_maxBars = Param(nameof(MaxBars), 48)
			.SetGreaterThanZero()
			.SetDisplay("Max Bars", "Maximum bars to hold", "Parameters")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Main candle type", "Parameters");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Trend Candle Type", "Higher timeframe for trend", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TrendCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trendUp = false;
		_medWindow = 0;
		_barIndex = 0;
		_entryBar = -1;
		_entryPrice = 0m;
		_highWater = 0m;
		_trailLive = false;
		_atrEntry = 0m;
		_tpMultEntry = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = 14 };
		var ema13 = new ExponentialMovingAverage { Length = 13 };
		var ema34 = new ExponentialMovingAverage { Length = 34 };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var roc = new RateOfChange { Length = 5 };
		var volSma = new SimpleMovingAverage { Length = 20 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ema13, ema34, rsi, roc, volSma, ProcessCandle).Start();

		var ema13Trend = new ExponentialMovingAverage { Length = 13 };
		var ema34Trend = new ExponentialMovingAverage { Length = 34 };
		var trendSub = SubscribeCandles(TrendCandleType);
		trendSub.Bind(ema13Trend, ema34Trend, ProcessTrend).Start();

		StartProtection();
	}

	private void ProcessTrend(ICandleMessage candle, decimal ema13, decimal ema34)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_trendUp = ema13 > ema34;
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal ema13, decimal ema34, decimal rsi, decimal roc, decimal volSma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barIndex++;

		var smallOk = candle.ClosePrice <= ema13 * 0.995m &&
			rsi < 45m &&
			candle.ClosePrice > candle.OpenPrice &&
			_trendUp;

		if (candle.TotalVolume >= volSma * VolMult &&
			candle.ClosePrice < candle.OpenPrice &&
			rsi < 55m &&
			_trendUp)
			_medWindow = 2;
		else
			_medWindow = Math.Max(_medWindow - 1, 0);

		var medOk = _medWindow > 0 && candle.ClosePrice > candle.OpenPrice;

		var largeOk = rsi < 25m &&
			roc > 4m &&
			candle.ClosePrice > ema34 &&
			_trendUp;

		string path = null;
		if (largeOk)
			path = "LARGE";
		else if (medOk)
			path = "MED";
		else if (smallOk)
			path = "SMALL";

		if (Position == 0 && path != null)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);

			_entryBar = _barIndex;
			_entryPrice = candle.ClosePrice;
			_highWater = candle.ClosePrice;
			_trailLive = false;
			_atrEntry = atr;
			_tpMultEntry = path == "SMALL" ? TpSmall : path == "MED" ? TpMed : TpLarge;
			return;
		}

		if (Position <= 0)
			return;

		var stopPrice = _entryPrice - _atrEntry * RiskMult;
		var targetPrice = _entryPrice + _atrEntry * _tpMultEntry;

		if (candle.LowPrice <= stopPrice)
		{
			SellMarket(Position);
			ResetTrade();
			return;
		}

		if (candle.HighPrice >= targetPrice)
		{
			SellMarket(Position);
			ResetTrade();
			return;
		}

		if (candle.HighPrice > _highWater)
			_highWater = candle.HighPrice;

		if (!_trailLive && (_highWater - _entryPrice) >= _atrEntry * TrailArm)
			_trailLive = true;

		if (_trailLive)
		{
			var trailStop = _highWater - _atrEntry * TrailPct;
			if (candle.LowPrice <= trailStop)
			{
				SellMarket(Position);
				ResetTrade();
				return;
			}
		}

		if (_barIndex - _entryBar >= MaxBars)
		{
			SellMarket(Position);
			ResetTrade();
		}
	}

	private void ResetTrade()
	{
		_medWindow = 0;
		_entryBar = -1;
		_entryPrice = 0m;
		_highWater = 0m;
		_trailLive = false;
		_atrEntry = 0m;
		_tpMultEntry = 0m;
	}
}
