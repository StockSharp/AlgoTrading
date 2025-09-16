using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid style strategy that opens a position on every new bar with optional martingale sizing.
/// </summary>
public class PrecipiceMartinStrategy : Strategy
{
	private readonly StrategyParam<bool> _useBuy;
	private readonly StrategyParam<int> _buyStepPips;
	private readonly StrategyParam<bool> _useSell;
	private readonly StrategyParam<int> _sellStepPips;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<decimal> _martingaleCoefficient;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _martingaleMultiplier;
	private decimal? _longEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortEntryPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private decimal _lastLongVolume;
	private decimal _lastShortVolume;
	private bool _preferLongEntry;

	public bool UseBuy
	{
		get => _useBuy.Value;
		set => _useBuy.Value = value;
	}

	public int BuyStepPips
	{
		get => _buyStepPips.Value;
		set => _buyStepPips.Value = value;
	}

	public bool UseSell
	{
		get => _useSell.Value;
		set => _useSell.Value = value;
	}

	public int SellStepPips
	{
		get => _sellStepPips.Value;
		set => _sellStepPips.Value = value;
	}

	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	public decimal MartingaleCoefficient
	{
		get => _martingaleCoefficient.Value;
		set => _martingaleCoefficient.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PrecipiceMartinStrategy()
	{
		_useBuy = Param(nameof(UseBuy), true)
			.SetDisplay("Use Buy", "Enable opening long positions", "Trading");
		_buyStepPips = Param(nameof(BuyStepPips), 89)
			.SetDisplay("Buy SL/TP (pips)", "Stop loss and take profit distance for longs", "Trading");
		_useSell = Param(nameof(UseSell), true)
			.SetDisplay("Use Sell", "Enable opening short positions", "Trading");
		_sellStepPips = Param(nameof(SellStepPips), 89)
			.SetDisplay("Sell SL/TP (pips)", "Stop loss and take profit distance for shorts", "Trading");
		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Use Martingale", "Increase volume after losing trades", "Position sizing");
		_martingaleCoefficient = Param(nameof(MartingaleCoefficient), 1.6m)
			.SetDisplay("Martingale Coefficient", "Multiplier applied after losses", "Position sizing")
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to generate trading bars", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_martingaleMultiplier = 1m;
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_lastLongVolume = 0m;
		_lastShortVolume = 0m;
		_preferLongEntry = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Calculate the pip size based on the instrument tick size.
		_pipSize = (Security?.PriceStep ?? 1m) * 10m;
		if (_pipSize <= 0m)
			_pipSize = Security?.PriceStep ?? 1m;
		if (_pipSize <= 0m)
			_pipSize = 1m;

		_martingaleMultiplier = 1m;

		// Subscribe to candle data and process every completed bar.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Ignore unfinished candles because the original strategy trades on bar close.
		if (candle.State != CandleStates.Finished)
			return;

		// Skip processing when the strategy is not ready to trade yet.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Manage exits before looking for new entries.
		var closedLong = TryCloseLong(candle);
		var closedShort = TryCloseShort(candle);

		// Do not open new trades while any position is still active.
		if (Position != 0)
			return;

		// Avoid immediate re-entry for a direction that has just closed on this bar.
		if (closedLong)
			return;
		if (closedShort)
			return;

		if (_longEntryPrice.HasValue || _shortEntryPrice.HasValue)
			return;

		if (UseBuy && UseSell)
		{
			if (_preferLongEntry)
			{
				if (TryEnterLong(candle))
				{
					_preferLongEntry = false;
					return;
				}

				if (TryEnterShort(candle))
				{
					_preferLongEntry = false;
				}
			}
			else
			{
				if (TryEnterShort(candle))
				{
					_preferLongEntry = true;
					return;
				}

				if (TryEnterLong(candle))
				{
					_preferLongEntry = true;
				}
			}
		}
		else
		{
			if (UseBuy)
			{
				TryEnterLong(candle);
			}

			if (UseSell)
			{
				TryEnterShort(candle);
			}
		}
	}

	private bool TryEnterLong(ICandleMessage candle)
	{
		// Prevent duplicate long entries.
		if (_longEntryPrice.HasValue)
			return false;

		// Ensure no net position exists before opening a new long.
		if (Position != 0)
			return false;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return false;

		var entryPrice = candle.ClosePrice;

		BuyMarket(volume);

		_longEntryPrice = entryPrice;
		_lastLongVolume = volume;

		if (BuyStepPips > 0)
		{
			var offset = BuyStepPips * _pipSize;
			_longStopPrice = entryPrice - offset;
			_longTakePrice = entryPrice + offset;
		}
		else
		{
			_longStopPrice = null;
			_longTakePrice = null;
		}

		return true;
	}

	private bool TryEnterShort(ICandleMessage candle)
	{
		// Prevent duplicate short entries.
		if (_shortEntryPrice.HasValue)
			return false;

		// Ensure no net position exists before opening a new short.
		if (Position != 0)
			return false;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return false;

		var entryPrice = candle.ClosePrice;

		SellMarket(volume);

		_shortEntryPrice = entryPrice;
		_lastShortVolume = volume;

		if (SellStepPips > 0)
		{
			var offset = SellStepPips * _pipSize;
			_shortStopPrice = entryPrice + offset;
			_shortTakePrice = entryPrice - offset;
		}
		else
		{
			_shortStopPrice = null;
			_shortTakePrice = null;
		}

		return true;
	}

	private bool TryCloseLong(ICandleMessage candle)
	{
		if (!_longEntryPrice.HasValue)
			return false;

		var volume = Position;
		if (volume <= 0m)
			volume = _lastLongVolume;

		if (volume <= 0m)
			return false;

		var stopHit = _longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value;
		var takeHit = _longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value;

		if (!stopHit && !takeHit)
			return false;

		var exitPrice = stopHit ? _longStopPrice!.Value : _longTakePrice!.Value;

		SellMarket(volume);

		var pnl = (exitPrice - _longEntryPrice.Value) * volume;
		UpdateMartingale(pnl);

		ResetLongState();
		return true;
	}

	private bool TryCloseShort(ICandleMessage candle)
	{
		if (!_shortEntryPrice.HasValue)
			return false;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			volume = _lastShortVolume;

		if (volume <= 0m)
			return false;

		var stopHit = _shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value;
		var takeHit = _shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value;

		if (!stopHit && !takeHit)
			return false;

		var exitPrice = stopHit ? _shortStopPrice!.Value : _shortTakePrice!.Value;

		BuyMarket(volume);

		var pnl = (_shortEntryPrice.Value - exitPrice) * volume;
		UpdateMartingale(pnl);

		ResetShortState();
		return true;
	}

	private decimal CalculateOrderVolume()
	{
		var minVolume = Security?.MinVolume ?? Volume;
		if (minVolume <= 0m)
			minVolume = 1m;

		var multiplier = UseMartingale ? _martingaleMultiplier : 1m;
		var volume = minVolume * multiplier;

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		var step = Security?.VolumeStep;
		if (step.HasValue && step.Value > 0m)
		{
			var steps = Math.Truncate(volume / step.Value);
			volume = steps * step.Value;
		}

		var min = Security?.MinVolume;
		if (min.HasValue && min.Value > 0m && volume < min.Value)
			volume = 0m;

		var max = Security?.MaxVolume;
		if (max.HasValue && max.Value > 0m && volume > max.Value)
			volume = max.Value;

		return volume;
	}

	private void UpdateMartingale(decimal realizedPnl)
	{
		if (!UseMartingale)
		{
			_martingaleMultiplier = 1m;
			return;
		}

		// Reset the multiplier after profitable trades and scale up after losses.
		_martingaleMultiplier = realizedPnl > 0m
			? 1m
			: _martingaleMultiplier * MartingaleCoefficient;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_lastLongVolume = 0m;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_lastShortVolume = 0m;
	}
}
