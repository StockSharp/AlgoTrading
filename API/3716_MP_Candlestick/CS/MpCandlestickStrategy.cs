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

/// <summary>
/// Candlestick-based risk managed strategy converted from the MetaTrader "mp candlestick" expert.
/// Uses candle direction to decide trade side, applies ATR-based or fixed stop-loss distance,
/// and enforces a configurable risk-to-reward profile with margin awareness.
/// </summary>
public class MpCandlestickStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<decimal> _maxMarginUsage;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<bool> _useAutoSl;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private bool _isLongPosition;

	/// <summary>
	/// Percentage of portfolio equity risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Desired risk to reward ratio.
	/// </summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
	}

	/// <summary>
	/// Maximum allowed margin usage percentage.
	/// </summary>
	public decimal MaxMarginUsage
	{
		get => _maxMarginUsage.Value;
		set => _maxMarginUsage.Value = value;
	}

	/// <summary>
	/// Fixed stop-loss distance in MetaTrader pips when dynamic stop is disabled.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enables ATR based stop-loss sizing.
	/// </summary>
	public bool UseAutoSl
	{
		get => _useAutoSl.Value;
		set => _useAutoSl.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation and ATR calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public MpCandlestickStrategy()
	{
		_riskPercent = Param(nameof(RiskPercent), 1m)
		.SetNotNegative()
		.SetDisplay("Risk Percent", "Percentage of portfolio equity risked per trade", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 10m, 0.5m);

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Risk/Reward Ratio", "Target reward multiple relative to the initial risk", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 4m, 0.25m);

		_maxMarginUsage = Param(nameof(MaxMarginUsage), 30m)
		.SetNotNegative()
		.SetDisplay("Max Margin Usage", "Upper bound for margin consumption as percent of equity", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetGreaterThanZero()
		.SetDisplay("Stop-Loss Pips", "Fixed stop-loss size in MetaTrader pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 5);

		_useAutoSl = Param(nameof(UseAutoSl), true)
		.SetDisplay("Use ATR Stop", "If enabled the stop-loss uses ATR * 1.5 distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series for signals", "Data");
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

		_atr = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_isLongPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();
		ResetRiskLevels();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
			ResetRiskLevels();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		CheckRiskLevels(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
		{
			UpdateTrailingStop(candle.ClosePrice);
			return;
		}

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		if (!isBullish && !isBearish)
			return;

		if (!TryCreateRiskTargets(isBullish, candle.ClosePrice, atrValue,
		out var stopPrice, out var takeProfit, out var stopDistance))
		{
			return;
		}

		var volume = CalculateTradeVolume(stopDistance);
		if (volume <= 0m)
			return;

		if (!ValidateMargin(candle.ClosePrice, volume, isBullish))
			return;

		if (isBullish)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_entryPrice = candle.ClosePrice;
		_stopPrice = stopPrice;
		_takeProfitPrice = takeProfit;
		_isLongPosition = isBullish;
		UpdateTrailingStop(candle.ClosePrice);
	}

	private bool TryCreateRiskTargets(bool isLong, decimal entryPrice, decimal atrValue,
	out decimal stopPrice, out decimal takeProfitPrice, out decimal stopDistance)
	{
		stopPrice = 0m;
		takeProfitPrice = 0m;
		stopDistance = 0m;

		var security = Security;
		if (security == null)
			return false;

		if (RiskRewardRatio <= 0m)
			return false;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		decimal distance;
		if (UseAutoSl)
		{
			if (_atr is null || !_atr.IsFormed)
				return false;

			distance = atrValue * 1.5m;
		}
		else
		{
			distance = StopLossPips * priceStep;
		}

		if (distance <= 0m)
			return false;

		stopDistance = distance;
		stopPrice = isLong ? entryPrice - distance : entryPrice + distance;
		takeProfitPrice = isLong ? entryPrice + distance * RiskRewardRatio : entryPrice - distance * RiskRewardRatio;

		return stopPrice > 0m && takeProfitPrice > 0m;
	}

	private decimal CalculateTradeVolume(decimal stopDistance)
	{
		var security = Security;
		var portfolio = Portfolio;

		if (security == null || portfolio == null)
			return 0m;

		var volumeStep = security.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
			volumeStep = 1m;

		if (RiskPercent <= 0m)
			return AlignVolume(volumeStep);

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
			return 0m;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var stepPrice = security.StepPrice ?? priceStep;
		if (stepPrice <= 0m)
			stepPrice = priceStep;

		if (stopDistance <= 0m)
			return 0m;

		var steps = stopDistance / priceStep;
		if (steps <= 0m)
			return 0m;

		var lossPerVolumeStep = steps * stepPrice;
		if (lossPerVolumeStep <= 0m)
			return 0m;

		var riskAmount = equity * (RiskPercent / 100m);
		if (riskAmount <= 0m)
			return 0m;

		var rawVolume = riskAmount / lossPerVolumeStep * volumeStep;
		return AlignVolume(rawVolume);
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return 0m;

		var volumeStep = security.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
			volumeStep = 1m;

		if (volume <= 0m)
			volume = volumeStep;

		var steps = Math.Floor(volume / volumeStep);
		if (steps <= 0m)
			steps = 1m;

		var normalized = steps * volumeStep;

		var minVolume = security.MinVolume ?? volumeStep;
		if (normalized < minVolume)
			normalized = minVolume;

		var maxVolume = security.MaxVolume;
		if (maxVolume.HasValue && normalized > maxVolume.Value)
			normalized = maxVolume.Value;

		return normalized;
	}

	private bool ValidateMargin(decimal price, decimal volume, bool isLong)
	{
		if (MaxMarginUsage <= 0m)
			return true;

		var security = Security;
		var portfolio = Portfolio;
		if (security == null || portfolio == null)
			return false;

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
			return false;

		var volumeStep = security.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
			volumeStep = 1m;

		var marginPerVolume = isLong ? security.MarginBuy : security.MarginSell;

		decimal margin;
		if (marginPerVolume is decimal direct && direct > 0m)
		{
			margin = direct * (volume / volumeStep);
		}
		else
		{
			margin = price * volume;
		}

		var maxMargin = equity * (MaxMarginUsage / 100m);
		return margin <= maxMargin;
	}

	private void CheckRiskLevels(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetRiskLevels();
				return;
			}

			if (_takeProfitPrice is decimal target && candle.HighPrice >= target)
			{
				SellMarket(Position);
				ResetRiskLevels();
			}
		}
		else if (Position < 0m)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetRiskLevels();
				return;
			}

			if (_takeProfitPrice is decimal target && candle.LowPrice <= target)
			{
				BuyMarket(Math.Abs(Position));
				ResetRiskLevels();
			}
		}
	}

	private void UpdateTrailingStop(decimal currentPrice)
	{
		if (!UseAutoSl)
			return;

		if (_entryPrice is not decimal entry || _takeProfitPrice is not decimal take || _stopPrice is not decimal currentStop)
			return;

		var security = Security;
		if (security == null)
			return;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		if (_isLongPosition)
		{
			var candidate = entry + (take - entry) * 0.5m;
			var limit = currentPrice - priceStep;
			if (limit <= entry)
				limit = entry;

			if (candidate > limit)
				candidate = limit;

			if (candidate > currentStop && candidate < currentPrice)
				_stopPrice = candidate;
		}
		else
		{
			var candidate = entry - (entry - take) * 0.5m;
			var limit = currentPrice + priceStep;
			if (limit >= entry)
				limit = entry;

			if (candidate < limit)
				candidate = limit;

			if (candidate < currentStop && candidate > currentPrice)
				_stopPrice = candidate;
		}
	}

	private void ResetRiskLevels()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_isLongPosition = false;
	}
}