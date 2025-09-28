namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

public class CryptosStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _lookbackCandles;
	private readonly StrategyParam<decimal> _takeProfitRatio;
	private readonly StrategyParam<decimal> _alternativeTakeProfitRatio;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<decimal> _valueIndex;
	private readonly StrategyParam<decimal> _cryptoValueIndex;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<int> _minRangeTicks;
	private readonly StrategyParam<decimal> _spreadPoints;
	private readonly StrategyParam<int> _globalTrend;
	private readonly StrategyParam<bool> _autoHighLow;
	private readonly StrategyParam<bool> _manualBuyTrigger;
	private readonly StrategyParam<bool> _manualSellTrigger;
	private readonly StrategyParam<bool> _skipBuys;
	private readonly StrategyParam<bool> _skipSells;

	private WeightedMovingAverage _lwma = null!;
	private BollingerBands _bollinger = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private bool _isTradingAllowed;
	private decimal _manualLow;
	private decimal _manualHigh;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	public CryptosStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Length of the Bollinger Bands", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 55)
		.SetGreaterThanZero()
		.SetDisplay("WMA Period", "Length of the weighted moving average", "Indicators");

		_lookbackCandles = Param(nameof(LookbackCandles), 60)
		.SetGreaterThanZero()
		.SetDisplay("Lookback Candles", "Depth for swing high/low detection", "Risk");

		_takeProfitRatio = Param(nameof(TakeProfitRatio), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Take-Profit Ratio", "Multiplier applied to the range for crypto symbols", "Risk")
		.SetCanOptimize(true);

		_alternativeTakeProfitRatio = Param(nameof(AlternativeTakeProfitRatio), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Alt Take-Profit Ratio", "Multiplier applied when the symbol is not ETH/USD", "Risk");

		_riskPerTrade = Param(nameof(RiskPerTrade), 250m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Per Trade", "Capital risked in quote currency", "Risk")
		.SetCanOptimize(true);

		_valueIndex = Param(nameof(ValueIndex), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Value Index", "Contract size multiplier for non-crypto symbols", "Risk");

		_cryptoValueIndex = Param(nameof(CryptoValueIndex), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Crypto Value Index", "Contract size multiplier for ETH/USD", "Risk");

		_minVolume = Param(nameof(MinVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Minimum Volume", "Lower position size bound", "Risk");

		_maxVolume = Param(nameof(MaxVolume), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Maximum Volume", "Upper position size bound", "Risk");

		_minRangeTicks = Param(nameof(MinRangeTicks), 100)
		.SetGreaterThanZero()
		.SetDisplay("Minimum Range", "Lower limit for calculated ranges in ticks", "Risk");

		_spreadPoints = Param(nameof(SpreadPoints), 0m)
		.SetDisplay("Spread (ticks)", "Override for spread if Level1 data is unavailable", "General");

		_globalTrend = Param(nameof(GlobalTrend), 0)
		.SetDisplay("Global Trend", "Manual override: 1=sell bias, 2=buy bias", "Events");

		_autoHighLow = Param(nameof(AutoHighLow), true)
		.SetDisplay("Auto High/Low", "Use automatic swing detection", "Events");

		_manualBuyTrigger = Param(nameof(ManualBuyTrigger), false)
		.SetDisplay("Manual Buy", "Set true to queue a manual long entry", "Events");

		_manualSellTrigger = Param(nameof(ManualSellTrigger), false)
		.SetDisplay("Manual Sell", "Set true to queue a manual short entry", "Events");

		_skipBuys = Param(nameof(SkipBuys), false)
		.SetDisplay("Skip Buys", "Disable new long entries", "Events");

		_skipSells = Param(nameof(SkipSells), false)
		.SetDisplay("Skip Sells", "Disable new short entries", "Events");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int LookbackCandles
	{
		get => _lookbackCandles.Value;
		set => _lookbackCandles.Value = value;
	}

	public decimal TakeProfitRatio
	{
		get => _takeProfitRatio.Value;
		set => _takeProfitRatio.Value = value;
	}

	public decimal AlternativeTakeProfitRatio
	{
		get => _alternativeTakeProfitRatio.Value;
		set => _alternativeTakeProfitRatio.Value = value;
	}

	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}

	public decimal ValueIndex
	{
		get => _valueIndex.Value;
		set => _valueIndex.Value = value;
	}

	public decimal CryptoValueIndex
	{
		get => _cryptoValueIndex.Value;
		set => _cryptoValueIndex.Value = value;
	}

	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	public int MinRangeTicks
	{
		get => _minRangeTicks.Value;
		set => _minRangeTicks.Value = value;
	}

	public decimal SpreadPoints
	{
		get => _spreadPoints.Value;
		set => _spreadPoints.Value = value;
	}

	public int GlobalTrend
	{
		get => _globalTrend.Value;
		set => _globalTrend.Value = value;
	}

	public bool AutoHighLow
	{
		get => _autoHighLow.Value;
		set => _autoHighLow.Value = value;
	}

	public bool ManualBuyTrigger
	{
		get => _manualBuyTrigger.Value;
		set => _manualBuyTrigger.Value = value;
	}

	public bool ManualSellTrigger
	{
		get => _manualSellTrigger.Value;
		set => _manualSellTrigger.Value = value;
	}

	public bool SkipBuys
	{
		get => _skipBuys.Value;
		set => _skipBuys.Value = value;
	}

	public bool SkipSells
	{
		get => _skipSells.Value;
		set => _skipSells.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_isTradingAllowed = true;
		_manualLow = decimal.MaxValue;
		_manualHigh = decimal.MinValue;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare indicators for Bollinger Bands, WMA, and swing detection.
		_lwma = new WeightedMovingAverage { Length = MaPeriod };
		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerWidth
		};
		_highest = new Highest { Length = LookbackCandles + 1 };
		_lowest = new Lowest { Length = LookbackCandles + 1 };

		// Validate available capital before the first trade.
		var balance = Portfolio?.CurrentValue ?? Portfolio?.CurrentBalance ?? Portfolio?.BeginBalance ?? 0m;
		_isTradingAllowed = balance >= RiskPerTrade * 3m;

		if (!_isTradingAllowed)
		{
			LogWarning("Balance {0} is below the recommended minimum {1}.", balance, RiskPerTrade * 3m);
		}

		var security = Security;
		if (security != null)
		{
			if (security.VolumeMin.HasValue && security.VolumeMin.Value > 0m)
				MinVolume = Math.Max(MinVolume, security.VolumeMin.Value);

			if (security.VolumeMax.HasValue && security.VolumeMax.Value > 0m)
				MaxVolume = Math.Min(MaxVolume, security.VolumeMax.Value);
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_lwma, _bollinger, _highest, _lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal middleBand, decimal upperBand, decimal lowerBand, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isTradingAllowed)
			return;

		if (!_lwma.IsFormed || !_bollinger.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var closePrice = candle.ClosePrice;

		// Update global bias when the price touches Bollinger extremes.
		if (closePrice >= upperBand)
		{
			GlobalTrend = 1;
			_manualLow = decimal.MaxValue;
			AutoHighLow = false;
		}
		else if (closePrice <= lowerBand)
		{
			GlobalTrend = 2;
			_manualHigh = decimal.MinValue;
			AutoHighLow = false;
		}

		// Track manual high/low anchors when auto mode is disabled.
		if (closePrice < maValue && closePrice < _manualLow)
			_manualLow = closePrice;
		else if (closePrice > maValue && closePrice > _manualHigh)
			_manualHigh = closePrice;

		var lowestBoundary = AutoHighLow ? lowestValue : (_manualLow != decimal.MaxValue ? _manualLow : lowestValue);
		var highestBoundary = AutoHighLow ? highestValue : (_manualHigh != decimal.MinValue ? _manualHigh : highestValue);

		var isEth = Security?.Code.EqualsIgnoreCase("ETHUSD");
		var takeProfitRatio = isEth ? TakeProfitRatio : AlternativeTakeProfitRatio;
		var valueIndex = isEth ? CryptoValueIndex : ValueIndex;

		var spreadPoints = SpreadPoints;
		if (spreadPoints <= 0m)
		{
			var bestAsk = Security?.BestAskPrice;
			var bestBid = Security?.BestBidPrice;
			if (bestAsk.HasValue && bestBid.HasValue && priceStep > 0m)
				spreadPoints = (bestAsk.Value - bestBid.Value) / priceStep;
		}

		var buyRangeTicks = (maValue - lowestBoundary) / priceStep * takeProfitRatio;
		if (buyRangeTicks < MinRangeTicks)
			buyRangeTicks = MinRangeTicks;

		var sellRangeTicks = (highestBoundary - maValue) / priceStep * takeProfitRatio;
		if (sellRangeTicks < MinRangeTicks)
			sellRangeTicks = MinRangeTicks;

		if (buyRangeTicks <= 0m || sellRangeTicks <= 0m)
			return;

		// Execute manual triggers if requested by the operator.
		if (ManualBuyTrigger)
		{
			var executed = TryOpenBuy(closePrice, priceStep, spreadPoints, buyRangeTicks, lowestBoundary, valueIndex);
			if (executed)
				ManualBuyTrigger = false;
		}

		if (ManualSellTrigger)
		{
			var executed = TryOpenSell(closePrice, priceStep, spreadPoints, sellRangeTicks, highestBoundary, valueIndex);
			if (executed)
				ManualSellTrigger = false;
		}

		// Core entry logic following the original MQL conditions.
		if (closePrice < maValue && GlobalTrend == 1 && Position >= 0 && !SkipSells)
		{
			if (TryOpenSell(closePrice, priceStep, spreadPoints, sellRangeTicks, highestBoundary, valueIndex))
			{
				GlobalTrend = 0;
				_manualHigh = decimal.MinValue;
			}
		}
		else if (closePrice > maValue && GlobalTrend == 2 && Position <= 0 && !SkipBuys)
		{
			if (TryOpenBuy(closePrice, priceStep, spreadPoints, buyRangeTicks, lowestBoundary, valueIndex))
			{
				GlobalTrend = 0;
				_manualLow = decimal.MaxValue;
			}
		}

		// Trailing exits when price pierces the opposite Bollinger band.
		if (closePrice <= lowerBand && Position > 0)
		{
			CloseLongPosition();
		}
		else if (closePrice >= upperBand && Position < 0)
		{
			CloseShortPosition();
		}

		// Enforce stored stop-loss and take-profit levels.
		if (Position > 0)
		{
			if (_longStop.HasValue && closePrice <= _longStop.Value || _longTake.HasValue && closePrice >= _longTake.Value)
				CloseLongPosition();
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && closePrice >= _shortStop.Value || _shortTake.HasValue && closePrice <= _shortTake.Value)
				CloseShortPosition();
		}
	}

	private bool TryOpenBuy(decimal closePrice, decimal priceStep, decimal spreadPoints, decimal rangeTicks, decimal lowestBoundary, decimal valueIndex)
	{
		if (Position > 0)
			return false;

		var volume = CalculateVolume(rangeTicks, valueIndex);
		if (volume <= 0m)
			return false;

		var askPrice = Security?.BestAskPrice ?? closePrice;
		var takeProfit = askPrice + (rangeTicks + spreadPoints) * priceStep;
		var stopLoss = lowestBoundary - spreadPoints * priceStep;

		BuyMarket(volume);
		LogInfo("Opened long position: volume={0} price={1} tp={2} sl={3}", volume, askPrice, takeProfit, stopLoss);

		_longStop = stopLoss;
		_longTake = takeProfit;
		_shortStop = null;
		_shortTake = null;

		return true;
	}

	private bool TryOpenSell(decimal closePrice, decimal priceStep, decimal spreadPoints, decimal rangeTicks, decimal highestBoundary, decimal valueIndex)
	{
		if (Position < 0)
			return false;

		var volume = CalculateVolume(rangeTicks, valueIndex);
		if (volume <= 0m)
			return false;

		var bidPrice = Security?.BestBidPrice ?? closePrice;
		var takeProfit = bidPrice - (rangeTicks + spreadPoints) * priceStep;
		var stopLoss = highestBoundary + spreadPoints * priceStep;

		SellMarket(volume);
		LogInfo("Opened short position: volume={0} price={1} tp={2} sl={3}", volume, bidPrice, takeProfit, stopLoss);

		_shortStop = stopLoss;
		_shortTake = takeProfit;
		_longStop = null;
		_longTake = null;

		return true;
	}

	private void CloseLongPosition()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		LogInfo("Closed long position: volume={0}", volume);

		_longStop = null;
		_longTake = null;
	}

	private void CloseShortPosition()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		LogInfo("Closed short position: volume={0}", volume);

		_shortStop = null;
		_shortTake = null;
	}

	private decimal CalculateVolume(decimal rangeTicks, decimal valueIndex)
	{
		if (rangeTicks <= 0m)
			return 0m;

		var risk = RiskPerTrade;
		var raw = risk > 0m ? risk / rangeTicks * valueIndex : MinVolume;

		var volume = AlignVolume(raw);
		if (volume < MinVolume)
			volume = MinVolume;

		if (MaxVolume > 0m && volume > MaxVolume)
			volume = MaxVolume;

		return volume;
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			if (security.VolumeStep.HasValue && security.VolumeStep.Value > 0m)
			{
				var step = security.VolumeStep.Value;
				volume = Math.Round(volume / step) * step;
			}

			if (security.VolumeMin.HasValue && volume < security.VolumeMin.Value)
				volume = security.VolumeMin.Value;

			if (security.VolumeMax.HasValue && volume > security.VolumeMax.Value)
				volume = security.VolumeMax.Value;
		}

		return volume;
	}
}

