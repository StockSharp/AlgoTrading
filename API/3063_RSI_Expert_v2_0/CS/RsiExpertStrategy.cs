using System;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI crossover expert advisor converted from the "RSI_Expert_v2.0" MetaTrader 5 strategy.
/// Combines RSI threshold crosses with an optional moving average trend filter, fixed/percentage risk sizing, and martingale recovery.
/// </summary>
public class RsiExpertStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<MoneyManagementMode> _moneyMode;
	private readonly StrategyParam<decimal> _volumeOrRiskValue;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLevelUp;
	private readonly StrategyParam<decimal> _rsiLevelDown;
	private readonly StrategyParam<MaTradeMode> _maMode;

	private RelativeStrengthIndex _rsi = null!;
	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;

	private decimal? _previousRsi;
	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _pipSize;
	private bool _closeRequested;
	private bool _closeByStop;
	private bool _lastTradeWasLoss;
	private decimal _prevRealizedPnL;

	/// <summary>
	/// Money management modes supported by the strategy.
	/// </summary>
	public enum MoneyManagementMode
	{
		/// <summary>
		/// Use a fixed volume for every trade.
		/// </summary>
		FixedVolume,

		/// <summary>
		/// Calculate volume from risk percent and stop-loss distance.
		/// </summary>
		RiskPercent
	}

	/// <summary>
	/// Moving average filter configuration copied from the original EA.
	/// </summary>
	public enum MaTradeMode
	{
		/// <summary>
		/// Ignore the moving average filter.
		/// </summary>
		Off,

		/// <summary>
		/// Trade in the direction of the fast and slow moving average crossover.
		/// </summary>
		Forward,

		/// <summary>
		/// Trade in the opposite direction of the moving average crossover.
		/// </summary>
		Reverse
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RsiExpertStrategy"/> class.
	/// </summary>
	public RsiExpertStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for generating signals", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance of the protective stop in pips", "Risk Management")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Distance of the profit target in pips", "Risk Management")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing distance applied after activation", "Risk Management")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Additional pips required before trailing moves again", "Risk Management")
			.SetCanOptimize(true);

		_moneyMode = Param(nameof(MoneyMode), MoneyManagementMode.FixedVolume)
			.SetDisplay("Money Mode", "Choose fixed volume or percent risk sizing", "Money Management");

		_volumeOrRiskValue = Param(nameof(VolumeOrRiskValue), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume / Risk", "Lot size for fixed mode or percent risk when using risk mode", "Money Management")
			.SetCanOptimize(true);

		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Use Martingale", "Double the next volume after a losing trade", "Money Management");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Period of the fast moving average", "Indicators")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Period of the slow moving average", "Indicators")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Averaging period for RSI", "Indicators")
			.SetCanOptimize(true);

		_rsiLevelUp = Param(nameof(RsiLevelUp), 70m)
			.SetRange(1m, 99m)
			.SetDisplay("RSI Level Up", "Upper RSI threshold for shorts", "Indicators")
			.SetCanOptimize(true);

		_rsiLevelDown = Param(nameof(RsiLevelDown), 30m)
			.SetRange(1m, 99m)
			.SetDisplay("RSI Level Down", "Lower RSI threshold for longs", "Indicators")
			.SetCanOptimize(true);

		_maMode = Param(nameof(MaMode), MaTradeMode.Forward)
			.SetDisplay("MA Trade Mode", "Direction of the moving average confirmation", "Indicators");
	}

	/// <summary>
	/// Candle type that drives indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Extra pips required before the trailing stop is tightened again.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Selected money management approach.
	/// </summary>
	public MoneyManagementMode MoneyMode
	{
		get => _moneyMode.Value;
		set => _moneyMode.Value = value;
	}

	/// <summary>
	/// Lot size in fixed mode or risk percent when money mode equals <see cref="MoneyManagementMode.RiskPercent"/>.
	/// </summary>
	public decimal VolumeOrRiskValue
	{
		get => _volumeOrRiskValue.Value;
		set => _volumeOrRiskValue.Value = value;
	}

	/// <summary>
	/// Enables martingale doubling after losing positions.
	/// </summary>
	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	/// <summary>
	/// Period for the fast moving average.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the slow moving average.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Averaging period for the RSI indicator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold used to detect short entries.
	/// </summary>
	public decimal RsiLevelUp
	{
		get => _rsiLevelUp.Value;
		set => _rsiLevelUp.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold used to detect long entries.
	/// </summary>
	public decimal RsiLevelDown
	{
		get => _rsiLevelDown.Value;
		set => _rsiLevelDown.Value = value;
	}

	/// <summary>
	/// Moving average confirmation mode.
	/// </summary>
	public MaTradeMode MaMode
	{
		get => _maMode.Value;
		set => _maMode.Value = value;
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

		_previousRsi = null;
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_pipSize = 0m;
		_closeRequested = false;
		_closeByStop = false;
		_lastTradeWasLoss = false;
		_prevRealizedPnL = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips == 0)
			throw new InvalidOperationException("Trailing is not possible when the trailing step is zero.");

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_fastMa = new SimpleMovingAverage
		{
			Length = FastMaPeriod
		};

		_slowMa = new SimpleMovingAverage
		{
			Length = SlowMaPeriod
		};

		_pipSize = CalculatePipSize();
		_prevRealizedPnL = PnL;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _fastMa, _slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageActivePosition(candle);

		var currentRsi = rsiValue;
		var previousRsi = _previousRsi;

		// Wait until indicators have accumulated enough history.
		if (!_rsi.IsFormed || (MaMode != MaTradeMode.Off && (!_fastMa.IsFormed || !_slowMa.IsFormed)))
		{
			_previousRsi = currentRsi;
			return;
		}

		if (Position != 0m || HasActiveOrders())
		{
			_previousRsi = currentRsi;
			return;
		}

		var rsiSignal = 0;
		if (previousRsi.HasValue)
		{
			if (currentRsi > RsiLevelDown && previousRsi.Value < RsiLevelDown)
				rsiSignal = 1;
			else if (currentRsi < RsiLevelUp && previousRsi.Value > RsiLevelUp)
				rsiSignal = -1;
		}

		var maSignal = 0;
		switch (MaMode)
		{
			case MaTradeMode.Forward:
				if (fastValue > slowValue)
					maSignal = 1;
				else if (fastValue < slowValue)
					maSignal = -1;
				break;
			case MaTradeMode.Reverse:
				if (fastValue < slowValue)
					maSignal = 1;
				else if (fastValue > slowValue)
					maSignal = -1;
				break;
		}

		var finalSignal = 0;
		if (rsiSignal == 1 && (MaMode == MaTradeMode.Off || maSignal == 1))
			finalSignal = 1;
		else if (rsiSignal == -1 && (MaMode == MaTradeMode.Off || maSignal == -1))
			finalSignal = -1;

		if (finalSignal > 0 && AllowLong())
		{
			var volume = GetOrderVolume();
			if (volume > 0m)
			{
				BuyMarket(volume);
				_closeRequested = false;
				_closeByStop = false;
			}
		}
		else if (finalSignal < 0 && AllowShort())
		{
			var volume = GetOrderVolume();
			if (volume > 0m)
			{
				SellMarket(volume);
				_closeRequested = false;
				_closeByStop = false;
			}
		}

		_previousRsi = currentRsi;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position == 0m)
			return;

		if (_entryPrice == null)
			return;

		var isLong = Position > 0m;
		var absPosition = Math.Abs(Position);
		var stepDistance = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;
		var trailingDistance = TrailingStopPips > 0 ? TrailingStopPips * _pipSize : 0m;

		if (TrailingStopPips > 0 && trailingDistance > 0m)
		{
			if (isLong)
			{
				var profitDistance = candle.ClosePrice - _entryPrice.Value;
				if (profitDistance > trailingDistance + stepDistance)
				{
					var candidate = candle.ClosePrice - trailingDistance;
					if (!_stopLossPrice.HasValue || _stopLossPrice.Value < candidate)
						_stopLossPrice = candidate;
				}
			}
			else
			{
				var profitDistance = _entryPrice.Value - candle.ClosePrice;
				if (profitDistance > trailingDistance + stepDistance)
				{
					var candidate = candle.ClosePrice + trailingDistance;
					if (!_stopLossPrice.HasValue || _stopLossPrice.Value > candidate)
						_stopLossPrice = candidate;
				}
			}
		}

		if (!_closeRequested && _takeProfitPrice.HasValue)
		{
			if (isLong && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Position);
				_closeRequested = true;
				_closeByStop = false;
				return;
			}

			if (!isLong && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(absPosition);
				_closeRequested = true;
				_closeByStop = false;
				return;
			}
		}

		if (!_closeRequested && _stopLossPrice.HasValue)
		{
			if (isLong && candle.LowPrice <= _stopLossPrice.Value)
			{
				SellMarket(Position);
				_closeRequested = true;
				_closeByStop = true;
				return;
			}

			if (!isLong && candle.HighPrice >= _stopLossPrice.Value)
			{
				BuyMarket(absPosition);
				_closeRequested = true;
				_closeByStop = true;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			if (_closeRequested)
			{
				_lastTradeWasLoss = _closeByStop;
				_closeRequested = false;
				_closeByStop = false;
				_prevRealizedPnL = PnL;
			}
			else
			{
				var realizedPnL = PnL;
				if (realizedPnL > _prevRealizedPnL)
					_lastTradeWasLoss = false;
				else if (realizedPnL < _prevRealizedPnL)
					_lastTradeWasLoss = true;

				_prevRealizedPnL = realizedPnL;
			}

			ResetPositionState();
		}
		else
		{
			_entryPrice ??= PositionPrice;

			InitializePositionState(Position > 0m, PositionPrice);
			_closeRequested = false;
			_closeByStop = false;
		}
	}

	private decimal GetOrderVolume()
	{
		var volume = MoneyMode == MoneyManagementMode.FixedVolume
			? VolumeOrRiskValue
			: CalculateRiskVolume();

		if (UseMartingale && _lastTradeWasLoss)
			volume *= 2m;

		return NormalizeVolume(volume);
	}

	private decimal CalculateRiskVolume()
	{
		if (StopLossPips <= 0)
			return VolumeOrRiskValue;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return VolumeOrRiskValue;

		var riskAmount = equity * VolumeOrRiskValue / 100m;
		var stopDistance = StopLossPips * _pipSize;

		return stopDistance > 0m ? riskAmount / stopDistance : VolumeOrRiskValue;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var minVolume = Security?.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		return volume > 0m ? volume : 0m;
	}

	private void InitializePositionState(bool isLong, decimal entryPrice)
	{
		_entryPrice = entryPrice;

		_stopLossPrice = StopLossPips > 0
			? entryPrice + (isLong ? -1m : 1m) * StopLossPips * _pipSize
			: null;

		_takeProfitPrice = TakeProfitPips > 0
			? entryPrice + (isLong ? 1m : -1m) * TakeProfitPips * _pipSize
			: null;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
			return step;

		var decimals = Security?.Decimals ?? 4;
		decimal pip = 1m;
		for (var i = 0; i < decimals; i++)
			pip /= 10m;

		return pip;
	}

	private bool HasActiveOrders()
	{
		return Orders.Any(o => o.State.IsActive());
	}
}
