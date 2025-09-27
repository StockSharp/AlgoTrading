using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual Vortex indicator strategy converted from the MetaTrader expert Exp_VortexIndicator_Duplex.
/// Maintains independent long and short signal streams with configurable timeframes and risk parameters.
/// </summary>
public class VortexIndicatorDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowLongExits;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _allowShortExits;
	private readonly StrategyParam<decimal> _longStopLossSteps;
	private readonly StrategyParam<decimal> _longTakeProfitSteps;
	private readonly StrategyParam<decimal> _shortStopLossSteps;
	private readonly StrategyParam<decimal> _shortTakeProfitSteps;
	private readonly StrategyParam<decimal> _tradeVolume;

	private VortexIndicator _longVortex = null!;
	private VortexIndicator _shortVortex = null!;

	private readonly List<(decimal plus, decimal minus)> _longHistory = new();
	private readonly List<(decimal plus, decimal minus)> _shortHistory = new();

	private decimal _priceStep;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;

	private readonly StrategyParam<int> _maxHistoryLength;

	/// <summary>
	/// Initializes parameters for the duplex Vortex strategy.
	/// </summary>
	public VortexIndicatorDuplexStrategy()
	{
		_maxHistoryLength = Param(nameof(MaxHistoryLength), 512)
			.SetGreaterThanZero()
			.SetDisplay("Max History Length", "Maximum stored Vortex samples per direction.", "General");

		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Timeframe used for long-side Vortex calculations.", "General");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Timeframe used for short-side Vortex calculations.", "General");

		_longLength = Param(nameof(LongLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Long Vortex Length", "VI period applied to the long signal stream.", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(7, 42, 7);

		_shortLength = Param(nameof(ShortLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Short Vortex Length", "VI period applied to the short signal stream.", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(7, 42, 7);

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Long Signal Bar", "Closed bar shift used for long evaluations.", "Signals");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Short Signal Bar", "Closed bar shift used for short evaluations.", "Signals");
		_allowLongEntries = Param(nameof(AllowLongEntries), true)
			.SetDisplay("Allow Long Entries", "Enable opening long positions when VI+ crosses above VI-.", "Trading");

		_allowLongExits = Param(nameof(AllowLongExits), true)
			.SetDisplay("Allow Long Exits", "Enable closing long positions when VI- dominates VI+.", "Trading");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
			.SetDisplay("Allow Short Entries", "Enable opening short positions when VI+ crosses below VI-.", "Trading");

		_allowShortExits = Param(nameof(AllowShortExits), true)
			.SetDisplay("Allow Short Exits", "Enable closing short positions when VI+ recovers above VI-.", "Trading");

		_longStopLossSteps = Param(nameof(LongStopLossSteps), 1000m)
			.SetNotNegative()
			.SetDisplay("Long Stop Loss Steps", "Protective distance below the long entry price in price steps (0 disables).", "Risk");

		_longTakeProfitSteps = Param(nameof(LongTakeProfitSteps), 2000m)
			.SetNotNegative()
			.SetDisplay("Long Take Profit Steps", "Target distance above the long entry price in price steps (0 disables).", "Risk");

		_shortStopLossSteps = Param(nameof(ShortStopLossSteps), 1000m)
			.SetNotNegative()
			.SetDisplay("Short Stop Loss Steps", "Protective distance above the short entry price in price steps (0 disables).", "Risk");

		_shortTakeProfitSteps = Param(nameof(ShortTakeProfitSteps), 2000m)
			.SetNotNegative()
			.SetDisplay("Short Take Profit Steps", "Target distance below the short entry price in price steps (0 disables).", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base order volume used for entries.", "Risk");
	}
	/// <summary>
	/// Candle type for the long-side signal calculations.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Candle type for the short-side signal calculations.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Vortex period applied to the long signal stream.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Vortex period applied to the short signal stream.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Shift in closed candles for long evaluations.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Shift in closed candles for short evaluations.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}
	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool AllowLongEntries
	{
		get => _allowLongEntries.Value;
		set => _allowLongEntries.Value = value;
	}

	/// <summary>
	/// Enables long exits.
	/// </summary>
	public bool AllowLongExits
	{
		get => _allowLongExits.Value;
		set => _allowLongExits.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool AllowShortEntries
	{
		get => _allowShortEntries.Value;
		set => _allowShortEntries.Value = value;
	}

	/// <summary>
	/// Enables short exits.
	/// </summary>
	public bool AllowShortExits
	{
		get => _allowShortExits.Value;
		set => _allowShortExits.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades in price steps.
	/// </summary>
	public decimal LongStopLossSteps
	{
		get => _longStopLossSteps.Value;
		set => _longStopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long trades in price steps.
	/// </summary>
	public decimal LongTakeProfitSteps
	{
		get => _longTakeProfitSteps.Value;
		set => _longTakeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades in price steps.
	/// </summary>
	public decimal ShortStopLossSteps
	{
		get => _shortStopLossSteps.Value;
		set => _shortStopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades in price steps.
	/// </summary>
	public decimal ShortTakeProfitSteps
	{
		get => _shortTakeProfitSteps.Value;
		set => _shortTakeProfitSteps.Value = value;
	}

	/// <summary>
	/// Volume used when sending market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of stored Vortex samples per signal stream.
	/// </summary>
	public int MaxHistoryLength
	{
		get => _maxHistoryLength.Value;
		set => _maxHistoryLength.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var result = new List<(Security, DataType)> { (Security, LongCandleType) };

		if (!ShortCandleType.Equals(LongCandleType))
		{
			result.Add((Security, ShortCandleType));
		}

		return result;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longHistory.Clear();
		_shortHistory.Clear();

		ResetLongState();
		ResetShortState();

		_longVortex = null!;
		_shortVortex = null!;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_priceStep = Security.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 1m;
		}

		Volume = TradeVolume;

		_longVortex = new VortexIndicator { Length = LongLength };
		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription
			.Bind(_longVortex, ProcessLongCandle)
			.Start();

		_shortVortex = new VortexIndicator { Length = ShortLength };
		var shortSubscription = SubscribeCandles(ShortCandleType);
		shortSubscription
			.Bind(_shortVortex, ProcessShortCandle)
			.Start();

		StartProtection();

		base.OnStarted(time);
	}
	private void ProcessLongCandle(ICandleMessage candle, decimal viPlus, decimal viMinus)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (CheckRiskManagement(candle.ClosePrice))
		{
			return;
		}

		AppendHistory(_longHistory, (viPlus, viMinus));

		if (!_longVortex.IsFormed)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (!TryGetHistoryPair(_longHistory, LongSignalBar, out var previous, out var current))
		{
			return;
		}

		var crossUp = previous.plus <= previous.minus && current.plus > current.minus;
		var longExit = current.minus > current.plus;

		if (longExit && AllowLongExits && Position > 0m)
		{
			SellMarket(Position);
			ResetLongState();
		}

		if (crossUp && AllowLongEntries)
		{
			TryOpenLong(candle.ClosePrice);
		}
	}
	private void ProcessShortCandle(ICandleMessage candle, decimal viPlus, decimal viMinus)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (CheckRiskManagement(candle.ClosePrice))
		{
			return;
		}

		AppendHistory(_shortHistory, (viPlus, viMinus));

		if (!_shortVortex.IsFormed)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (!TryGetHistoryPair(_shortHistory, ShortSignalBar, out var previous, out var current))
		{
			return;
		}

		var crossDown = previous.plus >= previous.minus && current.plus < current.minus;
		var shortExit = current.plus > current.minus;

		if (shortExit && AllowShortExits && Position < 0m)
		{
			BuyMarket(-Position);
			ResetShortState();
		}

		if (crossDown && AllowShortEntries)
		{
			TryOpenShort(candle.ClosePrice);
		}
	}
	private void TryOpenLong(decimal price)
	{
		if (Position > 0m)
		{
			return;
		}

		var volume = Volume;
		if (volume <= 0m)
		{
			return;
		}

		var buyVolume = volume;
		if (Position < 0m)
		{
			buyVolume += Math.Abs(Position);
		}

		if (buyVolume <= 0m)
		{
			return;
		}

		BuyMarket(buyVolume);

		_longEntryPrice = price;
		_longStopPrice = LongStopLossSteps > 0m ? price - GetStepValue(LongStopLossSteps) : null;
		_longTakeProfitPrice = LongTakeProfitSteps > 0m ? price + GetStepValue(LongTakeProfitSteps) : null;

		ResetShortState();
	}
	private void TryOpenShort(decimal price)
	{
		if (Position < 0m)
		{
			return;
		}

		var volume = Volume;
		if (volume <= 0m)
		{
			return;
		}

		var sellVolume = volume;
		if (Position > 0m)
		{
			sellVolume += Position;
		}

		if (sellVolume <= 0m)
		{
			return;
		}

		SellMarket(sellVolume);

		_shortEntryPrice = price;
		_shortStopPrice = ShortStopLossSteps > 0m ? price + GetStepValue(ShortStopLossSteps) : null;
		_shortTakeProfitPrice = ShortTakeProfitSteps > 0m ? price - GetStepValue(ShortTakeProfitSteps) : null;

		ResetLongState();
	}
	private bool CheckRiskManagement(decimal price)
	{
		if (Position > 0m)
		{
			if (_longStopPrice is decimal stop && price <= stop)
			{
				SellMarket(Position);
				ResetLongState();
				return true;
			}

			if (_longTakeProfitPrice is decimal take && price >= take)
			{
				SellMarket(Position);
				ResetLongState();
				return true;
			}
		}
		else if (Position < 0m)
		{
			if (_shortStopPrice is decimal stop && price >= stop)
			{
				BuyMarket(-Position);
				ResetShortState();
				return true;
			}

			if (_shortTakeProfitPrice is decimal take && price <= take)
			{
				BuyMarket(-Position);
				ResetShortState();
				return true;
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}

		return false;
	}
	private void AppendHistory(List<(decimal plus, decimal minus)> history, (decimal plus, decimal minus) value)
	{
		history.Add(value);
		if (history.Count > MaxHistoryLength)
		{
			history.RemoveAt(0);
		}
	}

	private bool TryGetHistoryPair(List<(decimal plus, decimal minus)> history, int signalBar, out (decimal plus, decimal minus) previous, out (decimal plus, decimal minus) current)
	{
		previous = default;
		current = default;

		var currentIndex = history.Count - 1 - signalBar;
		var previousIndex = currentIndex - 1;

		if (currentIndex < 0 || previousIndex < 0)
		{
			return false;
		}

		current = history[currentIndex];
		previous = history[previousIndex];
		return true;
	}

	private decimal GetStepValue(decimal steps)
	{
		return steps * _priceStep;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}
}
