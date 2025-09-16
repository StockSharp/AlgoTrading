using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Relative Strength Index crossover strategy translated from the MetaTrader RSI EA.
/// </summary>
public class RsiEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _closeBySignal;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<bool> _useAutoVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _manualVolume;

	private RelativeStrengthIndex _rsi;
	private decimal? _previousRsi;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTakeProfit;
	private decimal? _shortTakeProfit;

	/// <summary>
	/// Candle type used to calculate RSI.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the RSI indicator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level that triggers long entries.
	/// </summary>
	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// Overbought level that triggers short entries.
	/// </summary>
	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	/// <summary>
	/// Enable or disable long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Enable or disable short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Close positions when the RSI crosses the opposite level.
	/// </summary>
	public bool CloseBySignal
	{
		get => _closeBySignal.Value;
		set => _closeBySignal.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Automatically size orders by account risk.
	/// </summary>
	public bool UseAutoVolume
	{
		get => _useAutoVolume.Value;
		set => _useAutoVolume.Value = value;
	}

	/// <summary>
	/// Risk percentage applied when auto volume is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed order volume used when auto sizing is disabled.
	/// </summary>
	public decimal ManualVolume
	{
		get => _manualVolume.Value;
		set => _manualVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RsiEaStrategy"/> class.
	/// </summary>
	public RsiEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for RSI", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Number of bars used for RSI", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(8, 28, 2);

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Buy Level", "Cross above this level opens longs", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_rsiSellLevel = Param(nameof(RsiSellLevel), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Sell Level", "Cross below this level opens shorts", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow bullish trades", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow bearish trades", "Trading");

		_closeBySignal = Param(nameof(CloseBySignal), true)
			.SetDisplay("Close By Signal", "Exit when RSI flips", "Trading");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetRange(0m, 1000m)
			.SetDisplay("Stop Loss", "Distance from entry for stop loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 20m);

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetRange(0m, 1000m)
			.SetDisplay("Take Profit", "Distance from entry for take profit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 20m);

		_trailingStop = Param(nameof(TrailingStop), 0m)
			.SetRange(0m, 1000m)
			.SetDisplay("Trailing Stop", "Trailing distance after price moves", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 20m);

		_useAutoVolume = Param(nameof(UseAutoVolume), true)
			.SetDisplay("Auto Volume", "Size positions by risk percent", "Money Management");

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetRange(0m, 100m)
			.SetDisplay("Risk Percent", "Percentage of equity risked per trade", "Money Management");

		_manualVolume = Param(nameof(ManualVolume), 0.1m)
			.SetRange(0.01m, 100m)
			.SetDisplay("Manual Volume", "Fixed volume when auto sizing is off", "Money Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousRsi = null;
		_longStop = null;
		_shortStop = null;
		_longTakeProfit = null;
		_shortTakeProfit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, Process)
			.Start();

		StartProtection();
	}

	private void Process(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return; // Wait for completed candles only.

		if (!_rsi.IsFormed)
		{
			_previousRsi = rsiValue;
			return; // Indicator still gathering enough data.
		}

		var previous = _previousRsi;
		_previousRsi = rsiValue;

		if (ManageOpenPosition(candle))
			return; // Exit orders were submitted, wait for fills before new decisions.

		var crossAboveBuy = previous.HasValue && previous.Value < RsiBuyLevel && rsiValue > RsiBuyLevel;
		var crossBelowSell = previous.HasValue && previous.Value > RsiSellLevel && rsiValue < RsiSellLevel;

		if (CloseBySignal)
		{
			if (Position > 0 && crossBelowSell)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return; // Close long trades when RSI drops below the sell level.
			}

			if (Position < 0 && crossAboveBuy)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return; // Close short trades when RSI rises above the buy level.
			}
		}

		if (Position != 0)
			return; // Do not add hedged positions in the netted environment.

		if (EnableShort && crossBelowSell)
		{
			var volume = CalculateVolume();
			if (volume > 0m)
				SellMarket(volume);
			return;
		}

		if (EnableLong && crossAboveBuy)
		{
			var volume = CalculateVolume();
			if (volume > 0m)
				BuyMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
			ResetProtection();
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var entryPrice = Position.AveragePrice;

			if (_longStop is null && StopLoss > 0m)
				_longStop = entryPrice - StopLoss; // Initial protective stop below entry.

			if (_longTakeProfit is null && TakeProfit > 0m)
				_longTakeProfit = entryPrice + TakeProfit; // Profit target above entry.

			if (TrailingStop > 0m && candle.ClosePrice > entryPrice)
			{
				var candidate = candle.ClosePrice - TrailingStop;
				if (!_longStop.HasValue || candle.ClosePrice - 2m * TrailingStop > _longStop.Value)
					_longStop = candidate; // Trail only when price advances enough.
			}

			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}

			if (_longTakeProfit.HasValue && candle.HighPrice >= _longTakeProfit.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}
		}
		else if (Position < 0)
		{
			var entryPrice = Position.AveragePrice;

			if (_shortStop is null && StopLoss > 0m)
				_shortStop = entryPrice + StopLoss; // Protective stop above entry.

			if (_shortTakeProfit is null && TakeProfit > 0m)
				_shortTakeProfit = entryPrice - TakeProfit; // Profit target below entry.

			if (TrailingStop > 0m && candle.ClosePrice < entryPrice)
			{
				var candidate = candle.ClosePrice + TrailingStop;
				if (!_shortStop.HasValue || candle.ClosePrice + 2m * TrailingStop < _shortStop.Value)
					_shortStop = candidate; // Trail short stops only after favorable move.
			}

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}

			if (_shortTakeProfit.HasValue && candle.LowPrice <= _shortTakeProfit.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}
		}
		else
		{
			ResetProtection(); // Ensure cached levels are cleared when flat.
		}

		return false;
	}

	private decimal CalculateVolume()
	{
		if (!UseAutoVolume)
			return ManualVolume; // Use fixed size when auto sizing is disabled.

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return ManualVolume; // Fallback if equity information is unavailable.

		var stopDistance = StopLoss > 0m ? StopLoss : TrailingStop;
		if (stopDistance <= 0m)
			return ManualVolume; // Cannot compute risk-based size without a stop.

		var riskAmount = equity * RiskPercent / 100m;
		if (riskAmount <= 0m)
			return ManualVolume;

		var volume = riskAmount / stopDistance;
		return volume > 0m ? volume : ManualVolume;
	}

	private void ResetProtection()
	{
		_longStop = null;
		_shortStop = null;
		_longTakeProfit = null;
		_shortTakeProfit = null;
	}
}
