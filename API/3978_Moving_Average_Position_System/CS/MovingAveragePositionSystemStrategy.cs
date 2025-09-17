using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the FORTRADER MovingAveragePositionSystem expert advisor.
/// The strategy opens or closes positions on moving average crossings and optionally applies
/// a martingale-like position sizing routine based on cumulative results expressed in MetaTrader points.
/// </summary>
public class MovingAveragePositionSystemStrategy : Strategy
{
	/// <summary>
	/// Moving average calculation mode.
	/// </summary>
	public enum MovingAverageMode
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted,
	}

	private readonly StrategyParam<MovingAverageMode> _maType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _lossThresholdPips;
	private readonly StrategyParam<decimal> _profitThresholdPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closeHistory = new();
	private readonly List<decimal> _maHistory = new();

	private decimal _currentVolume;
	private decimal _cycleStartRealizedPnL;
	private decimal _priceStep;
	private decimal _stepPrice;

	/// <summary>
	/// Moving average type used for signal calculation.
	/// </summary>
	public MovingAverageMode MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the moving average before generating signals.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initial lot size before the martingale routine modifies it.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Base lot size restored after profitable cycles.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed lot size.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Loss threshold in MetaTrader points that doubles the next trade volume.
	/// </summary>
	public decimal LossThresholdPips
	{
		get => _lossThresholdPips.Value;
		set => _lossThresholdPips.Value = value;
	}

	/// <summary>
	/// Profit target in MetaTrader points that resets the volume to the starting lot.
	/// </summary>
	public decimal ProfitThresholdPips
	{
		get => _profitThresholdPips.Value;
		set => _profitThresholdPips.Value = value;
	}

	/// <summary>
	/// Fixed take profit distance in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables the martingale-style money management block.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAveragePositionSystemStrategy"/> class.
	/// </summary>
	public MovingAveragePositionSystemStrategy()
	{
		_maType = Param(nameof(MaType), MovingAverageMode.LinearWeighted)
		.SetDisplay("MA Type", "Moving average method", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 240)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Moving average length", "Indicators");

		_maShift = Param(nameof(MaShift), 0)
		.SetRange(0, 100)
		.SetDisplay("MA Shift", "Forward shift for the moving average", "Indicators");

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
		.SetGreaterThan(0m)
		.SetDisplay("Initial Volume", "Starting lot size", "Trading");

		_startVolume = Param(nameof(StartVolume), 0.1m)
		.SetGreaterThan(0m)
		.SetDisplay("Start Volume", "Base lot restored after profits", "Trading");

		_maxVolume = Param(nameof(MaxVolume), 10m)
		.SetGreaterThan(0m)
		.SetDisplay("Max Volume", "Maximum allowed lot size", "Trading");

		_lossThresholdPips = Param(nameof(LossThresholdPips), 90m)
		.SetGreaterThan(0m)
		.SetDisplay("Loss Threshold (pts)", "Loss in points that doubles the lot", "Risk");

		_profitThresholdPips = Param(nameof(ProfitThresholdPips), 170m)
		.SetGreaterThan(0m)
		.SetDisplay("Profit Target (pts)", "Profit in points that resets the lot", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 1000m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Take Profit (pts)", "Fixed take profit distance", "Risk");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), true)
		.SetDisplay("Use Money Management", "Enable martingale volume control", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for calculations", "Market Data");
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

		_closeHistory.Clear();
		_maHistory.Clear();
		_currentVolume = InitialVolume;
		Volume = _currentVolume;
		_cycleStartRealizedPnL = PnLManager?.RealizedPnL ?? 0m;
		_priceStep = 0m;
		_stepPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_currentVolume = InitialVolume;
		Volume = _currentVolume;
		_cycleStartRealizedPnL = PnLManager?.RealizedPnL ?? 0m;

		_priceStep = Security?.PriceStep ?? 0m;
		_stepPrice = Security?.StepPrice ?? 0m;

		var movingAverage = CreateMovingAverage();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(movingAverage, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, movingAverage);
			DrawOwnTrades(area);
		}

		var takeProfitUnit = TakeProfitPips > 0m ? new Unit(TakeProfitPips, UnitTypes.Step) : null;
		StartProtection(takeProfit: takeProfitUnit);

		base.OnStarted(time);
	}

	private MovingAverage CreateMovingAverage()
	{
		return MaType switch
		{
			MovingAverageMode.Exponential => new ExponentialMovingAverage { Length = MaPeriod },
			MovingAverageMode.Smoothed => new SmoothedMovingAverage { Length = MaPeriod },
			MovingAverageMode.LinearWeighted => new LinearWeightedMovingAverage { Length = MaPeriod },
			_ => new SimpleMovingAverage { Length = MaPeriod },
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		// Work only with finished candles to reproduce the MQL4 behaviour.
		if (candle.State != CandleStates.Finished)
		return;

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		var previousClose = _closeHistory.Count >= 1 ? _closeHistory[^1] : (decimal?)null;
		var previousPreviousClose = _closeHistory.Count >= 2 ? _closeHistory[^2] : (decimal?)null;

		decimal? shiftedMa = null;
		if (_maHistory.Count > MaShift)
		{
			var index = _maHistory.Count - 1 - MaShift;
			if (index >= 0)
			shiftedMa = _maHistory[index];
		}

		if (previousClose.HasValue && previousPreviousClose.HasValue && shiftedMa.HasValue)
		{
			// Manage existing positions based on the opposite crossing.
			ManageOpenPosition(previousClose.Value, shiftedMa.Value);

			// Update the working volume according to the martingale routine.
			UpdateVolume(previousClose.Value, shiftedMa.Value);

			if (canTrade)
			{
				TryEnter(previousClose.Value, previousPreviousClose.Value, shiftedMa.Value);
			}
		}

		// Store the latest values for the next iteration.
		_closeHistory.Add(candle.ClosePrice);
		_maHistory.Add(maValue);
	}

	private void ManageOpenPosition(decimal previousClose, decimal shiftedMa)
	{
		// Close long positions when the latest closed candle falls back below the moving average.
		if (Position > 0 && previousClose < shiftedMa)
		{
			ClosePosition();
			return;
		}

		// Close short positions when the latest closed candle climbs back above the average.
		if (Position < 0 && previousClose > shiftedMa)
		{
			ClosePosition();
		}
	}

	private void UpdateVolume(decimal previousClose, decimal shiftedMa)
	{
		if (!UseMoneyManagement)
		return;

		var realizedPnL = PnLManager?.RealizedPnL ?? 0m;
		var realizedDiff = realizedPnL - _cycleStartRealizedPnL;

		var stepPrice = _stepPrice != 0m ? _stepPrice : Security?.StepPrice ?? 1m;
		var priceStep = _priceStep != 0m ? _priceStep : Security?.PriceStep ?? 1m;

		var resultInSteps = stepPrice != 0m ? realizedDiff / stepPrice : 0m;

		if (Position != 0 && priceStep > 0m && PositionPrice != null)
		{
			// Consider only floating losses as in the original script.
			var diff = Position > 0
			? previousClose - PositionPrice.Value
			: PositionPrice.Value - previousClose;

			if (diff < 0m)
			{
				resultInSteps += diff / priceStep;
			}
		}

		if (resultInSteps <= -LossThresholdPips)
		{
			// Double the lot size while keeping it within the maximum allowed range.
			var newVolume = Math.Min(_currentVolume * 2m, MaxVolume);
			if (newVolume > 0m)
			{
				_currentVolume = newVolume;
				NormalizeVolume();
				Volume = _currentVolume;
			}

			if (Position != 0)
			{
				ClosePosition();
			}

			_cycleStartRealizedPnL = realizedPnL;
		}
		else if (resultInSteps >= ProfitThresholdPips)
		{
			// Reset the lot size to the configured starting volume and lock in profits.
			_currentVolume = StartVolume;
			NormalizeVolume();
			Volume = _currentVolume;

			if (Position != 0)
			{
				ClosePosition();
			}

			_cycleStartRealizedPnL = realizedPnL;
		}
		else
		{
			NormalizeVolume();
		}
	}

	private void TryEnter(decimal previousClose, decimal previousPreviousClose, decimal shiftedMa)
	{
		NormalizeVolume();

		if (_currentVolume <= 0m)
		return;

		// Detect upward crossing: price moved from below the moving average to above it.
		var crossedUp = previousClose > shiftedMa && previousPreviousClose < shiftedMa;
		if (crossedUp && Position <= 0)
		{
			BuyMarket(_currentVolume);
			return;
		}

		// Detect downward crossing: price moved from above the moving average to below it.
		var crossedDown = previousClose < shiftedMa && previousPreviousClose > shiftedMa;
		if (crossedDown && Position >= 0)
		{
			SellMarket(_currentVolume);
		}
	}

	private void NormalizeVolume()
	{
		// Reduce the working lot if it exceeds the maximum allowed size.
		while (_currentVolume > MaxVolume && _currentVolume > 0m)
		{
			_currentVolume /= 2m;
		}

		if (Portfolio is not null)
		{
			var portfolioValue = Portfolio.CurrentValue ?? Portfolio.CurrentBalance ?? Portfolio.BeginValue ?? 0m;
			var marginThreshold = 1000m * _currentVolume;

			while (_currentVolume > 0m && portfolioValue < marginThreshold)
			{
				_currentVolume /= 2m;
				marginThreshold = 1000m * _currentVolume;
			}
		}

		if (_currentVolume < 0m)
		{
			_currentVolume = 0m;
		}
	}
}
