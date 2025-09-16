using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Nova strategy converted from MQL that compares the current price with the price N seconds ago.
/// Opens a long position when the previous candle is bullish and the ask price has moved up by a threshold.
/// Opens a short position when the previous candle is bearish and the bid price has dropped below the stored ask price.
/// After a stop-loss the position size is multiplied by a coefficient, after a take-profit it resets to the base volume.
/// </summary>
public class NovaStrategy : Strategy
{
	private readonly StrategyParam<int> _secondsAgo;
	private readonly StrategyParam<int> _stepPips;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _lossCoefficient;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _referenceAsk;
	private decimal? _referenceBid;
	private DateTimeOffset? _lastCheckTime;
	private decimal _stepOffset;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _currentVolume;
	private decimal? _lastTradeVolume;
	private decimal _previousPnL;
	private decimal? _currentAsk;
	private decimal? _currentBid;
	private ICandleMessage _previousCandle;

	/// <summary>
	/// Seconds to look back for the price comparison.
	/// </summary>
	public int SecondsAgo
	{
		get => _secondsAgo.Value;
		set => _secondsAgo.Value = value;
	}

	/// <summary>
	/// Step in pips that is required for the breakout condition.
	/// </summary>
	public int StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Base trading volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Coefficient used to increase the volume after a stop-loss.
	/// </summary>
	public decimal LossCoefficient
	{
		get => _lossCoefficient.Value;
		set => _lossCoefficient.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public NovaStrategy()
	{
		_secondsAgo = Param(nameof(SecondsAgo), 10)
		.SetGreaterThanZero()
		.SetDisplay("Seconds window", "Seconds to look back for price comparison", "General")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);

		_stepPips = Param(nameof(StepPips), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Step (pips)", "Price offset in pips for breakout check", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(0, 5, 1);

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Base volume", "Initial order volume", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 0.5m, 0.05m);

		_stopLossPips = Param(nameof(StopLossPips), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop-loss (pips)", "Stop-loss distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0, 5, 1);

		_takeProfitPips = Param(nameof(TakeProfitPips), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take-profit (pips)", "Take-profit distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0, 5, 1);

		_lossCoefficient = Param(nameof(LossCoefficient), 1.6m)
		.SetGreaterThanZero()
		.SetDisplay("Loss coefficient", "Multiplier for the next trade after a stop-loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 2.5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle type", "Candles used for signal calculations", "General");
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

		_referenceAsk = null;
		_referenceBid = null;
		_lastCheckTime = null;
		_stepOffset = 0m;
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		_currentVolume = 0m;
		_lastTradeVolume = null;
		_previousPnL = 0m;
		_currentAsk = null;
		_currentBid = null;
		_previousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_previousPnL = PnL;

		var pipSize = GetPipSize();
		_stepOffset = StepPips * pipSize;
		_stopLossOffset = StopLossPips * pipSize;
		_takeProfitOffset = TakeProfitPips * pipSize;

		_currentVolume = NormalizeVolume(BaseVolume);
		Volume = _currentVolume;

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(ProcessCandle)
		.Start();

		if (StopLossPips > 0 || TakeProfitPips > 0)
		{
			StartProtection(
			stopLoss: StopLossPips > 0 ? new Unit(_stopLossOffset, UnitTypes.Absolute) : default,
			takeProfit: TakeProfitPips > 0 ? new Unit(_takeProfitOffset, UnitTypes.Absolute) : default);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		_currentAsk = (decimal)ask;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		_currentBid = (decimal)bid;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateVolumeFromPnL();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var previous = _previousCandle;
		_previousCandle = candle;

		if (previous is null)
		return;

		if (Position != 0)
		return;

		var now = candle.CloseTime;
		var interval = TimeSpan.FromSeconds(SecondsAgo);

		if (_lastCheckTime != null && now - _lastCheckTime < interval)
		return;

		var currentAsk = _currentAsk ?? Security?.BestAsk?.Price ?? candle.ClosePrice;
		var currentBid = _currentBid ?? Security?.BestBid?.Price ?? candle.ClosePrice;

		if (currentAsk == 0m || currentBid == 0m)
		{
			_referenceAsk = null;
			_referenceBid = null;
			_lastCheckTime = now;
			return;
		}

		if (_referenceAsk is null || _referenceBid is null)
		{
			_referenceAsk = currentAsk;
			_referenceBid = currentBid;
			_lastCheckTime = now;
			return;
		}

		var bullishPrevious = previous.ClosePrice > previous.OpenPrice;
		var bearishPrevious = previous.ClosePrice < previous.OpenPrice;
		var referenceAsk = _referenceAsk.Value;

		if (bullishPrevious && currentAsk - _stepOffset > referenceAsk)
		{
			TryEnterLong(currentAsk);
		}
		else if (bearishPrevious && currentBid + _stepOffset < referenceAsk)
		{
			TryEnterShort(currentBid);
		}

		_referenceAsk = currentAsk;
		_referenceBid = currentBid;
		_lastCheckTime = now;
	}

	private void TryEnterLong(decimal price)
	{
		if (_currentVolume <= 0m)
		return;

		BuyMarket(_currentVolume);
		_lastTradeVolume = _currentVolume;
		LogInfo($"Open long at {price:F5} with volume {_currentVolume:F2}");
	}

	private void TryEnterShort(decimal price)
	{
		if (_currentVolume <= 0m)
		return;

		SellMarket(_currentVolume);
		_lastTradeVolume = _currentVolume;
		LogInfo($"Open short at {price:F5} with volume {_currentVolume:F2}");
	}

	private void UpdateVolumeFromPnL()
	{
		var realizedPnL = PnL;
		if (realizedPnL == _previousPnL)
		return;

		var delta = realizedPnL - _previousPnL;
		_previousPnL = realizedPnL;

		if (delta > 0m)
		{
			_currentVolume = NormalizeVolume(BaseVolume);
			Volume = _currentVolume;
			LogInfo("Reset volume after profitable trade");
		}
		else if (delta < 0m)
		{
			var referenceVolume = _lastTradeVolume ?? _currentVolume;
			_currentVolume = NormalizeVolume(referenceVolume * LossCoefficient);
			Volume = _currentVolume;
			LogInfo($"Increase volume after loss to {_currentVolume:F2}");
		}
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			volume = Math.Floor(volume / step) * step;
		}

		var min = Security?.MinVolume ?? 0m;
		if (min > 0m && volume < min)
		{
			volume = min;
		}

		var max = Security?.MaxVolume ?? 0m;
		if (max > 0m && volume > max)
		{
			volume = max;
		}

		return volume;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		var factor = decimals is 3 or 5 ? 10m : 1m;
		return step * factor;
	}
}
