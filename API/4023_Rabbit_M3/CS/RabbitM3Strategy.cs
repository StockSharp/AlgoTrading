using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor RabbitM3 (aka "Petes Party Trick").
/// Switches between long-only and short-only modes using hourly exponential moving averages.
/// Uses Williams %R momentum crosses and CCI thresholds for entries, Donchian channel for emergency exits,
/// and optional position sizing that grows after large winning trades.
/// </summary>
public class RabbitM3Strategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _williamsSellLevel;
	private readonly StrategyParam<decimal> _williamsBuyLevel;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciSellLevel;
	private readonly StrategyParam<decimal> _cciBuyLevel;
	private readonly StrategyParam<int> _donchianLength;
	private readonly StrategyParam<int> _maxOpenPositions;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _entryVolume;
	private readonly StrategyParam<decimal> _bigWinThreshold;
	private readonly StrategyParam<decimal> _volumeIncrement;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;
	private CommodityChannelIndex _cci = null!;
	private WilliamsPercentRange _williams = null!;
	private DonchianChannels _donchian = null!;

	private decimal _pipSize;
	private decimal _currentVolume;
	private decimal _currentBigWinTarget;

	private decimal? _previousWilliams;
	private decimal? _currentDonchianUpper;
	private decimal? _currentDonchianLower;
	private decimal? _previousDonchianUpper;
	private decimal? _previousDonchianLower;

	private TrendDirection _trendDirection;
	private bool _allowBuy;
	private bool _allowSell;

	private bool _longActive;
	private bool _shortActive;
	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal _longStopPrice;
	private decimal _longTakeProfitPrice;
	private decimal _shortStopPrice;
	private decimal _shortTakeProfitPrice;

	private enum TrendDirection
	{
		Neutral,
		Bullish,
		Bearish,
	}

	/// <summary>
	/// Initializes strategy parameters with RabbitM3 defaults.
	/// </summary>
	public RabbitM3Strategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 33)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(10, 80, 5);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 70)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA Period", "Length of the slow trend filter (H1 EMA)", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(20, 120, 5);

		_williamsPeriod = Param(nameof(WilliamsPeriod), 62)
		.SetGreaterThanZero()
		.SetDisplay("Williams %R Period", "Lookback for Williams %R momentum", "Entry Filter")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 5);

		_williamsSellLevel = Param(nameof(WilliamsSellLevel), -20m)
		.SetDisplay("Williams Sell Level", "Upper threshold crossed downward to trigger shorts", "Entry Filter");

		_williamsBuyLevel = Param(nameof(WilliamsBuyLevel), -80m)
		.SetDisplay("Williams Buy Level", "Lower threshold crossed upward to trigger longs", "Entry Filter");

		_cciPeriod = Param(nameof(CciPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Commodity Channel Index period", "Entry Filter")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_cciSellLevel = Param(nameof(CciSellLevel), 101m)
		.SetDisplay("CCI Sell Level", "Minimum CCI value required for short entries", "Entry Filter");

		_cciBuyLevel = Param(nameof(CciBuyLevel), 99m)
		.SetDisplay("CCI Buy Level", "Maximum CCI value allowed for long entries", "Entry Filter");

		_donchianLength = Param(nameof(DonchianLength), 410)
		.SetGreaterThanZero()
		.SetDisplay("Donchian Length", "History depth used for stop-and-reverse exits", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100, 600, 50);

		_maxOpenPositions = Param(nameof(MaxOpenPositions), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Open Positions", "Maximum simultaneous trades (net position based)", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 360m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Fixed profit target distance from entry", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance from entry", "Risk");

		_entryVolume = Param(nameof(EntryVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Entry Volume", "Initial position size for each trade", "Money Management");

		_bigWinThreshold = Param(nameof(BigWinThreshold), 4m)
		.SetNotNegative()
		.SetDisplay("Big Win Threshold", "Profit required to increase volume; doubles after each trigger", "Money Management");

		_volumeIncrement = Param(nameof(VolumeIncrement), 0.01m)
		.SetNotNegative()
		.SetDisplay("Volume Increment", "Increment added to volume after beating Big Win Threshold", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for all indicators", "General");
	}

	/// <summary>
	/// Fast EMA period (hourly in the original EA).
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period (hourly in the original EA).
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R lookback length.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Level (in %R units) that must be crossed downward to arm short entries.
	/// </summary>
	public decimal WilliamsSellLevel
	{
		get => _williamsSellLevel.Value;
		set => _williamsSellLevel.Value = value;
	}

	/// <summary>
	/// Level (in %R units) that must be crossed upward to arm long entries.
	/// </summary>
	public decimal WilliamsBuyLevel
	{
		get => _williamsBuyLevel.Value;
		set => _williamsBuyLevel.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Minimum CCI value required before a short setup is valid.
	/// </summary>
	public decimal CciSellLevel
	{
		get => _cciSellLevel.Value;
		set => _cciSellLevel.Value = value;
	}

	/// <summary>
	/// Maximum CCI value allowed before a long setup is valid.
	/// </summary>
	public decimal CciBuyLevel
	{
		get => _cciBuyLevel.Value;
		set => _cciBuyLevel.Value = value;
	}

	/// <summary>
	/// Number of candles considered for Donchian exit levels.
	/// </summary>
	public int DonchianLength
	{
		get => _donchianLength.Value;
		set => _donchianLength.Value = value;
	}

	/// <summary>
	/// Maximum net open positions. RabbitM3 defaults to a single trade.
	/// </summary>
	public int MaxOpenPositions
	{
		get => _maxOpenPositions.Value;
		set => _maxOpenPositions.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips (chart points).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips (chart points).
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Initial order volume.
	/// </summary>
	public decimal EntryVolume
	{
		get => _entryVolume.Value;
		set => _entryVolume.Value = value;
	}

	/// <summary>
	/// Profit threshold that increases the trading volume after a winning trade.
	/// </summary>
	public decimal BigWinThreshold
	{
		get => _bigWinThreshold.Value;
		set => _bigWinThreshold.Value = value;
	}

	/// <summary>
	/// Volume increment applied when the big win logic is triggered.
	/// </summary>
	public decimal VolumeIncrement
	{
		get => _volumeIncrement.Value;
		set => _volumeIncrement.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_pipSize = 0m;
		_currentVolume = 0m;
		_currentBigWinTarget = 0m;
		_previousWilliams = null;
		_currentDonchianUpper = null;
		_currentDonchianLower = null;
		_previousDonchianUpper = null;
		_previousDonchianLower = null;
		_trendDirection = TrendDirection.Neutral;
		_allowBuy = false;
		_allowSell = false;
		_longActive = false;
		_shortActive = false;
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStopPrice = 0m;
		_longTakeProfitPrice = 0m;
		_shortStopPrice = 0m;
		_shortTakeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new ExponentialMovingAverage
		{
			Length = FastEmaPeriod,
			CandlePrice = CandlePrice.Close,
		};

		_slowEma = new ExponentialMovingAverage
		{
			Length = SlowEmaPeriod,
			CandlePrice = CandlePrice.Close,
		};

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod,
		};

		_williams = new WilliamsPercentRange
		{
			Length = WilliamsPeriod,
		};

		_donchian = new DonchianChannels
		{
			Length = DonchianLength,
		};

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
		_pipSize = 1m;

		_currentVolume = EntryVolume;
		Volume = _currentVolume;

		_currentBigWinTarget = BigWinThreshold > 0m && VolumeIncrement > 0m
			? BigWinThreshold
			: decimal.MaxValue;

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(_fastEma, _slowEma, _cci, _williams, ProcessCandle);
		subscription.BindEx(_donchian, UpdateDonchian);
		subscription.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal cciValue, decimal williamsValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		UpdateTrendState(fastValue, slowValue);

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_cci.IsFormed || !_williams.IsFormed)
		{
			_previousWilliams = williamsValue;
			return;
		}

		if (_previousDonchianUpper is not decimal exitUpper || _previousDonchianLower is not decimal exitLower)
		{
			_previousWilliams = williamsValue;
			return;
		}

		ManageExits(candle, exitUpper, exitLower);

		TryEnterPosition(candle, cciValue, williamsValue);

		_previousWilliams = williamsValue;
	}

	private void UpdateDonchian(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (!donchianValue.IsFinal)
		return;

		var channels = (DonchianChannelsValue)donchianValue;

		if (channels.UpperBand is not decimal upper || channels.LowerBand is not decimal lower)
		return;

		if (_currentDonchianUpper.HasValue && _currentDonchianLower.HasValue)
		{
			_previousDonchianUpper = _currentDonchianUpper;
			_previousDonchianLower = _currentDonchianLower;
		}

		_currentDonchianUpper = upper;
		_currentDonchianLower = lower;
	}

	private void UpdateTrendState(decimal fastValue, decimal slowValue)
	{
		if (fastValue < slowValue)
		{
			if (_trendDirection == TrendDirection.Bearish)
			return;

			if (Position > 0m)
			CloseLongPosition("EMA trend flipped bearish");

			_allowSell = true;
			_allowBuy = false;
			_trendDirection = TrendDirection.Bearish;
		}
		else if (fastValue > slowValue)
		{
			if (_trendDirection == TrendDirection.Bullish)
			return;

			if (Position < 0m)
			CloseShortPosition("EMA trend flipped bullish");

			_allowSell = false;
			_allowBuy = true;
			_trendDirection = TrendDirection.Bullish;
		}
	}

	private void ManageExits(ICandleMessage candle, decimal exitUpper, decimal exitLower)
	{
		if (Position < 0m)
		{
			if (_shortActive)
			{
				if (TakeProfitPips > 0m && candle.LowPrice <= _shortTakeProfitPrice)
				{
					CloseShortPosition("Take profit reached");
					return;
				}

				if (StopLossPips > 0m && candle.HighPrice >= _shortStopPrice)
				{
					CloseShortPosition("Stop loss hit");
					return;
				}
			}

			if (candle.ClosePrice >= exitUpper)
			{
				CloseShortPosition("Donchian breakout above upper band");
			}
		}
		else if (Position > 0m)
		{
			if (_longActive)
			{
				if (TakeProfitPips > 0m && candle.HighPrice >= _longTakeProfitPrice)
				{
					CloseLongPosition("Take profit reached");
					return;
				}

				if (StopLossPips > 0m && candle.LowPrice <= _longStopPrice)
				{
					CloseLongPosition("Stop loss hit");
					return;
				}
			}

			if (candle.ClosePrice <= exitLower)
			{
				CloseLongPosition("Donchian breakout below lower band");
			}
		}
	}

	private void TryEnterPosition(ICandleMessage candle, decimal cciValue, decimal williamsValue)
	{
		if (Position != 0m)
		return;

		if (_previousWilliams is not decimal previousWilliams)
		return;

		if (MaxOpenPositions <= 0)
		return;

		var canShort = _allowSell && cciValue > CciSellLevel && previousWilliams > WilliamsSellLevel && previousWilliams < 0m && williamsValue < WilliamsSellLevel;
		if (canShort)
		{
			_shortEntryPrice = candle.ClosePrice;
			_shortStopPrice = StopLossPips > 0m ? _shortEntryPrice + StopLossPips * _pipSize : 0m;
			_shortTakeProfitPrice = TakeProfitPips > 0m ? _shortEntryPrice - TakeProfitPips * _pipSize : 0m;
			_shortActive = true;
			_longActive = false;
			SellMarket(_currentVolume);
			return;
		}

		var canLong = _allowBuy && cciValue < CciBuyLevel && previousWilliams < WilliamsBuyLevel && previousWilliams < 0m && williamsValue > WilliamsBuyLevel;
		if (canLong)
		{
			_longEntryPrice = candle.ClosePrice;
			_longStopPrice = StopLossPips > 0m ? _longEntryPrice - StopLossPips * _pipSize : 0m;
			_longTakeProfitPrice = TakeProfitPips > 0m ? _longEntryPrice + TakeProfitPips * _pipSize : 0m;
			_longActive = true;
			_shortActive = false;
			BuyMarket(_currentVolume);
		}
	}

	private void CloseLongPosition(string reason)
	{
		if (Position <= 0m)
		return;

		LogInfo($"Closing long position: {reason}");
		SellMarket(Position);
		_longActive = false;
		_longEntryPrice = 0m;
		_longStopPrice = 0m;
		_longTakeProfitPrice = 0m;
	}

	private void CloseShortPosition(string reason)
	{
		if (Position >= 0m)
		return;

		LogInfo($"Closing short position: {reason}");
		BuyMarket(-Position);
		_shortActive = false;
		_shortEntryPrice = 0m;
		_shortStopPrice = 0m;
		_shortTakeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnPnLChanged(decimal diff)
	{
		base.OnPnLChanged(diff);

		if (diff <= 0m)
		return;

		if (VolumeIncrement <= 0m)
		return;

		if (_currentBigWinTarget == decimal.MaxValue)
		return;

		if (Position != 0m)
		return;

		if (diff > _currentBigWinTarget)
		{
			var previousTarget = _currentBigWinTarget;
			_currentVolume += VolumeIncrement;
			Volume = _currentVolume;
			_currentBigWinTarget *= 2m;
			LogInfo($"Increasing volume to {_currentVolume} after profit {diff:F5} exceeded threshold {previousTarget:F5}.");
		}
	}
}
