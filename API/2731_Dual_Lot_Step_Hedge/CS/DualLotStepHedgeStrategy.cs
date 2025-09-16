using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the "x1 lot from high to low" and "x1 lot from low to high" MetaTrader robots.
/// Opens hedged long/short positions with adjustable lot cycling and closes the basket once
/// a profit target is achieved.
/// </summary>
public class DualLotStepHedgeStrategy : Strategy
{
	private readonly StrategyParam<int> _lotMultiplier;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _minProfit;
	private readonly StrategyParam<LotScalingMode> _scalingMode;

	private decimal _volumeStep;
	private decimal _maxVolume;
	private decimal _currentVolume;
	private decimal _pipValue;
	private decimal _initialEquity;

	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;

	private bool _longEntryInProgress;
	private bool _shortEntryInProgress;
	private bool _longExitInProgress;
	private bool _shortExitInProgress;

	private decimal _pendingLongEntryVolume;
	private decimal _pendingShortEntryVolume;
	private decimal _pendingLongExitVolume;
	private decimal _pendingShortExitVolume;

	private bool _resetRequested;

	/// <summary>
	/// Defines the lot stepping mode that matches the original MetaTrader experts.
	/// </summary>
	public enum LotScalingMode
	{
		/// <summary>
		/// Start with the maximum lot multiplier and drop to the next step after the first cycle.
		/// </summary>
		HighToLow,

		/// <summary>
		/// Start with the minimum lot step and grow until the configured multiplier is reached.
		/// </summary>
		LowToHigh,
	}

	/// <summary>
	/// Maximum lot multiplier expressed in minimal volume steps.
	/// </summary>
	public int LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips from the average entry price of the leg.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips from the average entry price of the leg.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Basket profit target in account currency.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Selected lot stepping mode.
	/// </summary>
	public LotScalingMode ScalingMode
	{
		get => _scalingMode.Value;
		set => _scalingMode.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DualLotStepHedgeStrategy"/>.
	/// </summary>
	public DualLotStepHedgeStrategy()
	{
		_lotMultiplier = Param(nameof(LotMultiplier), 10)
		.SetGreaterThanZero()
		.SetDisplay("Lot Multiplier", "Maximum lot multiplier over the minimal step", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1, 20, 1);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance for each leg", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
		.SetDisplay("Take Profit (pips)", "Take profit distance for each leg", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 400m, 20m);

		_minProfit = Param(nameof(MinProfit), 27m)
		.SetDisplay("Basket Profit", "Target profit in account currency", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(5m, 200m, 5m);

		_scalingMode = Param(nameof(ScalingMode), LotScalingMode.HighToLow)
		.SetDisplay("Scaling Mode", "How the lot size evolves after entries", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_volumeStep = 0m;
		_maxVolume = 0m;
		_currentVolume = 0m;
		_pipValue = 0m;
		_initialEquity = 0m;

		_longVolume = 0m;
		_shortVolume = 0m;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;

		_longEntryInProgress = false;
		_shortEntryInProgress = false;
		_longExitInProgress = false;
		_shortExitInProgress = false;

		_pendingLongEntryVolume = 0m;
		_pendingShortEntryVolume = 0m;
		_pendingLongExitVolume = 0m;
		_pendingShortExitVolume = 0m;

		_resetRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_volumeStep = Security.VolumeStep ?? 0m;
		if (_volumeStep <= 0m)
		_volumeStep = 1m;

		_maxVolume = LotCheck(_volumeStep * LotMultiplier);
		if (_maxVolume <= 0m)
		_maxVolume = _volumeStep;

		_currentVolume = ScalingMode == LotScalingMode.HighToLow ? _maxVolume : _volumeStep;
		_pipValue = CalculatePipValue();

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		if (trade.TradePrice is not decimal price || price <= 0m)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_volumeStep <= 0m)
		return;

		if (_initialEquity <= 0m)
		_initialEquity = Portfolio.CurrentValue ?? 0m;

		CheckProtectiveLevels(price);

		if (_longExitInProgress || _shortExitInProgress)
		return;

		if (CheckProfitTarget())
		return;

		ResetCurrentVolumeIfNeeded();

		var buyCount = _longVolume > 0m ? 1 : 0;
		var sellCount = _shortVolume > 0m ? 1 : 0;

		if (buyCount > 1 || sellCount > 1)
		{
			CloseAllPositions();
			return;
		}

		if (_longEntryInProgress || _shortEntryInProgress)
		return;

		if (buyCount == 0 && sellCount == 0)
		{
			TryOpenHedge();
		}
		else if (buyCount == 1 && sellCount == 0)
		{
			OpenShortIfNeeded();
		}
		else if (buyCount == 0 && sellCount == 1)
		{
			OpenLongIfNeeded();
		}
	}

	private bool CheckProfitTarget()
	{
		if (_initialEquity <= 0m || MinProfit <= 0m)
		return false;

		var currentEquity = Portfolio.CurrentValue ?? 0m;
		if (currentEquity - _initialEquity >= MinProfit)
		{
			CloseAllPositions();
			return true;
		}

		return false;
	}

	private void TryOpenHedge()
	{
		if (_longEntryInProgress || _shortEntryInProgress)
		return;

		var volume = LotCheck(_currentVolume);
		if (volume <= 0m)
		return;

		var buyOk = ExecuteBuy(volume, true);
		var sellOk = ExecuteSell(volume, true);

		if (buyOk && sellOk)
		AdjustVolumeAfterEntry();
	}

	private void OpenLongIfNeeded()
	{
		if (_longEntryInProgress)
		return;

		var volume = LotCheck(_currentVolume);
		if (volume <= 0m)
		return;

		if (ExecuteBuy(volume, true))
		AdjustVolumeAfterEntry();
	}

	private void OpenShortIfNeeded()
	{
		if (_shortEntryInProgress)
		return;

		var volume = LotCheck(_currentVolume);
		if (volume <= 0m)
		return;

		if (ExecuteSell(volume, true))
		AdjustVolumeAfterEntry();
	}

	private void AdjustVolumeAfterEntry()
	{
		if (ScalingMode == LotScalingMode.HighToLow)
		{
			_currentVolume = LotCheck(_currentVolume - _volumeStep);
		}
		else
		{
			_currentVolume = LotCheck(_currentVolume + _volumeStep);
		}
	}

	private void CloseAllPositions()
	{
		if (_longVolume <= 0m && _shortVolume <= 0m && !_longExitInProgress && !_shortExitInProgress)
		{
			_resetRequested = true;
			ApplyResetIfFlat();
			return;
		}

		if (_longVolume > 0m && !_longExitInProgress)
		{
			if (ExecuteSell(_longVolume, false))
			_resetRequested = true;
		}

		if (_shortVolume > 0m && !_shortExitInProgress)
		{
			if (ExecuteBuy(_shortVolume, false))
			_resetRequested = true;
		}
	}

	private void CloseLong()
	{
		if (_longVolume <= 0m || _longExitInProgress)
		return;

		ExecuteSell(_longVolume, false);
	}

	private void CloseShort()
	{
		if (_shortVolume <= 0m || _shortExitInProgress)
		return;

		ExecuteBuy(_shortVolume, false);
	}

	private bool ExecuteBuy(decimal volume, bool openingLong)
	{
		if (volume <= 0m)
		return false;

		var order = BuyMarket(volume);
		if (order == null)
		return false;

		if (openingLong)
		{
			_longEntryInProgress = true;
			_pendingLongEntryVolume += volume;
		}
		else
		{
			_shortExitInProgress = true;
			_pendingShortExitVolume += volume;
		}

		return true;
	}

	private bool ExecuteSell(decimal volume, bool openingShort)
	{
		if (volume <= 0m)
		return false;

		var order = SellMarket(volume);
		if (order == null)
		return false;

		if (openingShort)
		{
			_shortEntryInProgress = true;
			_pendingShortEntryVolume += volume;
		}
		else
		{
			_longExitInProgress = true;
			_pendingLongExitVolume += volume;
		}

		return true;
	}

	private void CheckProtectiveLevels(decimal price)
	{
		if (_pipValue <= 0m)
		return;

		if (_longVolume > 0m && !_longExitInProgress)
		{
			var stop = StopLossPips > 0m ? _longAveragePrice - StopLossPips * _pipValue : decimal.MinValue;
			var take = TakeProfitPips > 0m ? _longAveragePrice + TakeProfitPips * _pipValue : decimal.MaxValue;

			if (StopLossPips > 0m && price <= stop)
			{
				CloseLong();
				return;
			}

			if (TakeProfitPips > 0m && price >= take)
			{
				CloseLong();
				return;
			}
		}

		if (_shortVolume > 0m && !_shortExitInProgress)
		{
			var stop = StopLossPips > 0m ? _shortAveragePrice + StopLossPips * _pipValue : decimal.MaxValue;
			var take = TakeProfitPips > 0m ? _shortAveragePrice - TakeProfitPips * _pipValue : decimal.MinValue;

			if (StopLossPips > 0m && price >= stop)
			{
				CloseShort();
				return;
			}

			if (TakeProfitPips > 0m && price <= take)
			{
				CloseShort();
			}
		}
	}

	private void ResetCurrentVolumeIfNeeded()
	{
		if (ScalingMode == LotScalingMode.HighToLow)
		{
			if (_currentVolume < _volumeStep)
			_currentVolume = _maxVolume;
		}
		else
		{
			if (_currentVolume < _volumeStep)
			_currentVolume = _volumeStep;
			else if (_currentVolume > _maxVolume)
			_currentVolume = _volumeStep;
		}
	}

	private decimal LotCheck(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var step = _volumeStep;
		if (step <= 0m)
		return 0m;

		var ratio = Math.Floor(volume / step);
		var normalized = ratio * step;

		if (normalized < step)
		normalized = 0m;

		if (normalized > _maxVolume)
		normalized = _maxVolume;

		return normalized;
	}

	private decimal CalculatePipValue()
	{
		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		double stepDouble;
		try
		{
			stepDouble = Convert.ToDouble(step);
		}
		catch
		{
			return step;
		}

		if (stepDouble <= 0d)
		return step;

		var decimals = (int)Math.Round(-Math.Log10(stepDouble));
		if (decimals == 3 || decimals == 5)
		return step * 10m;

		return step;
	}

	private void ApplyResetIfFlat()
	{
		if (!_resetRequested)
		return;

		if (_longVolume > 0m || _shortVolume > 0m)
		return;

		if (_longExitInProgress || _shortExitInProgress)
		return;

		if (_pendingLongEntryVolume > 0m || _pendingShortEntryVolume > 0m)
		return;

		_resetRequested = false;
		_initialEquity = 0m;

		if (ScalingMode == LotScalingMode.HighToLow)
		{
			_currentVolume = 0m;
		}
		else
		{
			_currentVolume = _volumeStep;
		}
	}

	private void ApplyLongOpen(decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		var total = _longVolume + volume;
		_longAveragePrice = _longVolume <= 0m
		? price
		: (_longAveragePrice * _longVolume + price * volume) / total;
		_longVolume = total;
	}

	private void ApplyShortOpen(decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		var total = _shortVolume + volume;
		_shortAveragePrice = _shortVolume <= 0m
		? price
		: (_shortAveragePrice * _shortVolume + price * volume) / total;
		_shortVolume = total;
	}

	private void ApplyLongClose(decimal volume)
	{
		if (volume <= 0m || _longVolume <= 0m)
		return;

		var closed = Math.Min(_longVolume, volume);
		_longVolume -= closed;
		if (_longVolume <= 0m)
		{
			_longVolume = 0m;
			_longAveragePrice = 0m;
		}
	}

	private void ApplyShortClose(decimal volume)
	{
		if (volume <= 0m || _shortVolume <= 0m)
		return;

		var closed = Math.Min(_shortVolume, volume);
		_shortVolume -= closed;
		if (_shortVolume <= 0m)
		{
			_shortVolume = 0m;
			_shortAveragePrice = 0m;
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order?.Security != Security)
		return;

		var volume = trade.Trade.Volume ?? 0m;
		if (volume <= 0m)
		return;

		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			ProcessBuyTrade(volume, price);
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			ProcessSellTrade(volume, price);
		}

		ApplyResetIfFlat();
	}

	private void ProcessBuyTrade(decimal volume, decimal price)
	{
		var remaining = volume;

		if (_pendingShortExitVolume > 0m)
		{
			var closing = Math.Min(_pendingShortExitVolume, remaining);
			ApplyShortClose(closing);
			_pendingShortExitVolume -= closing;
			remaining -= closing;

			if (_pendingShortExitVolume <= 0m)
			_shortExitInProgress = false;
		}

		if (remaining <= 0m)
		return;

		if (_pendingLongEntryVolume > 0m)
		{
			var opening = Math.Min(_pendingLongEntryVolume, remaining);
			ApplyLongOpen(opening, price);
			_pendingLongEntryVolume -= opening;
			remaining -= opening;

			if (_pendingLongEntryVolume <= 0m)
			_longEntryInProgress = false;
		}

		if (remaining > 0m)
		ApplyLongOpen(remaining, price);
	}

	private void ProcessSellTrade(decimal volume, decimal price)
	{
		var remaining = volume;

		if (_pendingLongExitVolume > 0m)
		{
			var closing = Math.Min(_pendingLongExitVolume, remaining);
			ApplyLongClose(closing);
			_pendingLongExitVolume -= closing;
			remaining -= closing;

			if (_pendingLongExitVolume <= 0m)
			_longExitInProgress = false;
		}

		if (remaining <= 0m)
		return;

		if (_pendingShortEntryVolume > 0m)
		{
			var opening = Math.Min(_pendingShortEntryVolume, remaining);
			ApplyShortOpen(opening, price);
			_pendingShortEntryVolume -= opening;
			remaining -= opening;

			if (_pendingShortEntryVolume <= 0m)
			_shortEntryInProgress = false;
		}

		if (remaining > 0m)
		ApplyShortOpen(remaining, price);
	}
}
