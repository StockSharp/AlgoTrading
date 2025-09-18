using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Explosion pattern breakout strategy converted from the MetaTrader expert.
/// Opens a position when the current candle range more than doubles the previous one.
/// Applies configurable stop-loss, take-profit, and trailing stop expressed in pips.
/// </summary>
public class ExplosionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _useAutoPipMultiplier;
	private readonly StrategyParam<bool> _onlyOnePositionPerBar;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;

	private decimal? _previousRange;
	private decimal? _entryPrice;
	private decimal? _activeStopPrice;
	private decimal? _activeTakePrice;

	private DateTimeOffset? _lastBuyBarTime;
	private DateTimeOffset? _lastSellBarTime;

	/// <summary>
	/// Trading volume used for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop activation distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum price improvement before the trailing stop advances.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Automatically expand pip size on five or three decimal instruments.
	/// </summary>
	public bool UseAutoPipMultiplier
	{
		get => _useAutoPipMultiplier.Value;
		set => _useAutoPipMultiplier.Value = value;
	}

	/// <summary>
	/// Allow only one trade per candle, replicating the MQL input.
	/// </summary>
	public bool OnlyOnePositionPerBar
	{
		get => _onlyOnePositionPerBar.Value;
		set => _onlyOnePositionPerBar.Value = value;
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
	public ExplosionStrategy()
	{
		_volume = Param(nameof(TradeVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume used for entries", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 0.1m, 0.01m);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 60m, 5m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 80m, 5m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 25m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop activation distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 60m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Step (pips)", "Trailing stop step in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 30m, 5m);

		_useAutoPipMultiplier = Param(nameof(UseAutoPipMultiplier), true)
		.SetDisplay("Auto Pip Multiplier", "Multiply pip size on 3/5 digit instruments", "General");

		_onlyOnePositionPerBar = Param(nameof(OnlyOnePositionPerBar), true)
		.SetDisplay("One Trade Per Bar", "Allow only one entry per candle", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used for signals", "General");
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

		_previousRange = null;
		_entryPrice = null;
		_activeStopPrice = null;
		_activeTakePrice = null;
		_lastBuyBarTime = null;
		_lastSellBarTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		{
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");
		}

		Volume = TradeVolume;

		var priceStep = Security?.PriceStep ?? 0m;
		var decimals = Security?.Decimals;
		var pipMultiplier = UseAutoPipMultiplier && decimals is int dec && (dec == 3 || dec == 5) ? 10m : 1m;

		_pipSize = priceStep > 0m ? priceStep * pipMultiplier : 0m;
		_stopLossOffset = StopLossPips * _pipSize;
		_takeProfitOffset = TakeProfitPips * _pipSize;
		_trailingStopOffset = TrailingStopPips * _pipSize;
		_trailingStepOffset = TrailingStepPips * _pipSize;

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		ManageActivePosition(candle);

		var currentRange = candle.HighPrice - candle.LowPrice;

		if (_previousRange is not decimal previousRange)
		{
			_previousRange = currentRange;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousRange = currentRange;
			return;
		}

		var buySignal = currentRange > previousRange * 2m && candle.ClosePrice > candle.OpenPrice;
		var sellSignal = currentRange > previousRange * 2m && candle.ClosePrice < candle.OpenPrice;

		if (buySignal)
		{
			TryEnterLong(candle);
		}
		else if (sellSignal)
		{
			TryEnterShort(candle);
		}

		_previousRange = currentRange;
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			return;
		}

		if (OnlyOnePositionPerBar && _lastBuyBarTime == candle.OpenTime)
		{
			return;
		}

		var volume = Volume;
		if (volume <= 0m)
		{
			return;
		}

		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		BuyMarket(volume);

		_lastBuyBarTime = candle.OpenTime;
		SetProtectionLevels(candle.ClosePrice, true);
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (Position < 0m)
		{
			return;
		}

		if (OnlyOnePositionPerBar && _lastSellBarTime == candle.OpenTime)
		{
			return;
		}

		var volume = Volume;
		if (volume <= 0m)
		{
			return;
		}

		if (Position > 0m)
		{
			SellMarket(Position);
		}

		SellMarket(volume);

		_lastSellBarTime = candle.OpenTime;
		SetProtectionLevels(candle.ClosePrice, false);
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var volume = Position;

			if (_activeStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(volume);
				ResetProtection();
				return;
			}

			if (_activeTakePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(volume);
				ResetProtection();
				return;
			}

			UpdateTrailingForLong(candle.ClosePrice);
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);

			if (_activeStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				ResetProtection();
				return;
			}

			if (_activeTakePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(volume);
				ResetProtection();
				return;
			}

			UpdateTrailingForShort(candle.ClosePrice);
		}
		else
		{
			ResetProtection();
		}
	}

	private void UpdateTrailingForLong(decimal price)
	{
		if (_entryPrice is not decimal entry || _trailingStopOffset <= 0m || _trailingStepOffset <= 0m)
		{
			return;
		}

		var distance = price - entry;
		if (distance <= _trailingStopOffset)
		{
			return;
		}

		var newStop = price - _trailingStopOffset;
		if (_activeStopPrice is decimal currentStop && newStop - currentStop < _trailingStepOffset)
		{
			return;
		}

		_activeStopPrice = newStop;
	}

	private void UpdateTrailingForShort(decimal price)
	{
		if (_entryPrice is not decimal entry || _trailingStopOffset <= 0m || _trailingStepOffset <= 0m)
		{
			return;
		}

		var distance = entry - price;
		if (distance <= _trailingStopOffset)
		{
			return;
		}

		var newStop = price + _trailingStopOffset;
		if (_activeStopPrice is decimal currentStop && currentStop - newStop < _trailingStepOffset)
		{
			return;
		}

		_activeStopPrice = newStop;
	}

	private void SetProtectionLevels(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		if (isLong)
		{
			_activeStopPrice = _stopLossOffset > 0m ? entryPrice - _stopLossOffset : null;
			_activeTakePrice = _takeProfitOffset > 0m ? entryPrice + _takeProfitOffset : null;
		}
		else
		{
			_activeStopPrice = _stopLossOffset > 0m ? entryPrice + _stopLossOffset : null;
			_activeTakePrice = _takeProfitOffset > 0m ? entryPrice - _takeProfitOffset : null;
		}
	}

	private void ResetProtection()
	{
		_entryPrice = null;
		_activeStopPrice = null;
		_activeTakePrice = null;
	}
}
