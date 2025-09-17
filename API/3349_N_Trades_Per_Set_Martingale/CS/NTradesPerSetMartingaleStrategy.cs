using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sequential long-only martingale strategy converted from the MetaTrader "N trades per set martingale + Close and reset on equity increase" expert advisor.
/// The strategy opens a new long trade whenever the previous position is closed and applies a configurable martingale multiplier after a fully losing set.
/// </summary>
public class NTradesPerSetMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _tradesPerSet;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _scaleFactor;
	private readonly StrategyParam<decimal> _equityDivisor;
	private readonly StrategyParam<decimal> _equityIncreaseTarget;

	private int _winsInSet;
	private int _lossesInSet;
	private int _tradesInSet;
	private decimal _currentVolume;
	private decimal _equityTarget;
	private decimal _openVolume;
	private decimal _averageEntryPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="NTradesPerSetMartingaleStrategy"/>.
	/// </summary>
	public NTradesPerSetMartingaleStrategy()
	{
		_tradesPerSet = Param(nameof(TradesPerSet), 5)
			.SetRange(1, 100)
			.SetDisplay("Trades Per Set", "Number of sequential trades that form one martingale cycle.", "Martingale")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetRange(0m, 10000m)
			.SetDisplay("Stop Loss (pips)", "Protective stop in price steps applied to each position.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
			.SetRange(0m, 10000m)
			.SetDisplay("Take Profit (pips)", "Profit target in price steps applied to each position.", "Risk")
			.SetCanOptimize(true);

		_scaleFactor = Param(nameof(ScaleFactor), 2m)
			.SetRange(1m, 10m)
			.SetDisplay("Scale Factor", "Multiplier applied to the next trade volume after a losing cycle.", "Martingale")
			.SetCanOptimize(true);

		_equityDivisor = Param(nameof(EquityDivisor), 100000m)
			.SetRange(1m, 10000000m)
			.SetDisplay("Equity Divisor", "Divides account equity to derive the base lot size after a winning cycle.", "Money Management")
			.SetCanOptimize(true);

		_equityIncreaseTarget = Param(nameof(EquityIncreaseTarget), 10m)
			.SetRange(0m, 1000000m)
			.SetDisplay("Equity Increase", "Amount of equity growth that triggers a global reset with volume recalculation.", "Money Management")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Number of trades that form a martingale set.
	/// </summary>
	public int TradesPerSet
	{
		get => _tradesPerSet.Value;
		set => _tradesPerSet.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the next trade volume after a fully losing set.
	/// </summary>
	public decimal ScaleFactor
	{
		get => _scaleFactor.Value;
		set => _scaleFactor.Value = value;
	}

	/// <summary>
	/// Divisor used to derive the base lot size from account equity after a winning cycle.
	/// </summary>
	public decimal EquityDivisor
	{
		get => _equityDivisor.Value;
		set => _equityDivisor.Value = value;
	}

	/// <summary>
	/// Equity growth threshold that forces all positions to close and the cycle to reset.
	/// </summary>
	public decimal EquityIncreaseTarget
	{
		get => _equityIncreaseTarget.Value;
		set => _equityIncreaseTarget.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		ResetCycle();
		_currentVolume = AlignVolume(CalculateBaseVolume());
		_equityTarget = CalculateEquityTarget();

		TryOpenPosition();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order?.Direction == null)
			return;

		if (trade.Order.Direction == Sides.Buy)
		{
			RegisterEntry(trade);
			AttachProtection(trade);
		}
		else if (trade.Order.Direction == Sides.Sell)
		{
			RegisterExit(trade);
		}
	}

	private void RegisterEntry(MyTrade trade)
	{
		var volume = trade.Trade?.Volume ?? 0m;
		var price = trade.Trade?.Price ?? 0m;
		if (volume <= 0m)
			return;

		var totalVolume = _openVolume + volume;
		if (totalVolume <= 0m)
			return;

		var accumulatedCost = (_averageEntryPrice * _openVolume) + (price * volume);
		_openVolume = totalVolume;
		_averageEntryPrice = accumulatedCost / totalVolume;
	}

	private void AttachProtection(MyTrade trade)
	{
		if (StopLossPips <= 0m && TakeProfitPips <= 0m)
			return;

		var price = trade.Trade?.Price ?? 0m;
		if (price <= 0m)
			return;

		var position = Position;
		if (position <= 0m)
			return;

		var stopSteps = ToSteps(StopLossPips);
		var takeSteps = ToSteps(TakeProfitPips);

		if (takeSteps > 0)
			SetTakeProfit(takeSteps, price, position);

		if (stopSteps > 0)
			SetStopLoss(stopSteps, price, position);
	}

	private void RegisterExit(MyTrade trade)
	{
		var volume = trade.Trade?.Volume ?? 0m;
		var price = trade.Trade?.Price ?? 0m;
		if (volume <= 0m || price <= 0m)
			return;

		_openVolume -= volume;
		if (_openVolume < 0m)
			_openVolume = 0m;

		if (_openVolume > 0m)
			return;

		var isWin = price > _averageEntryPrice;
		_averageEntryPrice = 0m;

		ProcessClosedTrade(isWin);
	}

	private void ProcessClosedTrade(bool isWin)
	{
		_tradesInSet++;

		if (isWin)
			_winsInSet++;
		else
			_lossesInSet++;

		if (_tradesInSet >= TradesPerSet)
		{
			HandleCompletedSet();
		}

		if (CheckEquityReset())
			return;

		TryOpenPosition();
	}

	private void HandleCompletedSet()
	{
		if (_winsInSet >= TradesPerSet)
		{
			_currentVolume = AlignVolume(CalculateBaseVolume());
		}
		else if (_lossesInSet >= TradesPerSet)
		{
			var scaled = _currentVolume * Math.Max(1m, ScaleFactor);
			_currentVolume = AlignVolume(scaled);
		}

		ResetCycle();
	}

	private bool CheckEquityReset()
	{
		if (EquityIncreaseTarget <= 0m)
			return false;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return false;

		if (equity < _equityTarget)
			return false;

		ResetCycle();
		_currentVolume = AlignVolume(CalculateBaseVolume());
		_equityTarget = equity + EquityIncreaseTarget;

		TryOpenPosition();
		return true;
	}

	private void TryOpenPosition()
	{
		if (ProcessState != ProcessStates.Started)
			return;

		if (Position != 0m)
			return;

		var volume = AlignVolume(_currentVolume);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
	}

	private void ResetCycle()
	{
		_winsInSet = 0;
		_lossesInSet = 0;
		_tradesInSet = 0;
		_openVolume = 0m;
		_averageEntryPrice = 0m;
	}

	private decimal CalculateBaseVolume()
	{
		var divisor = Math.Max(1m, EquityDivisor);
		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return _currentVolume > 0m ? _currentVolume : (Security?.VolumeStep ?? 1m);

		return equity / divisor;
	}

	private decimal AlignVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var minVolume = Security?.MinVolume ?? step;
		if (minVolume <= 0m)
			minVolume = step;

		var maxVolume = Security?.MaxVolume ?? decimal.MaxValue;

		if (volume <= 0m)
			volume = minVolume;

		var steps = Math.Round(volume / step, 0, MidpointRounding.AwayFromZero);
		var result = steps * step;

		if (result < minVolume)
			result = minVolume;
		if (result > maxVolume)
			result = maxVolume;

		return result;
	}

	private decimal CalculateEquityTarget()
	{
		if (EquityIncreaseTarget <= 0m)
			return decimal.MaxValue;

		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return EquityIncreaseTarget;

		return equity + EquityIncreaseTarget;
	}

	private int ToSteps(decimal priceOffset)
	{
		if (priceOffset <= 0m)
			return 0;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return (int)Math.Round(priceOffset, MidpointRounding.AwayFromZero);

		var steps = priceOffset / priceStep;
		return (int)Math.Round(steps, MidpointRounding.AwayFromZero);
	}
}
