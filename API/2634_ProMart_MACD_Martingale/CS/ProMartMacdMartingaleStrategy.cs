using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD-based martingale strategy converted from the ProMart MQL expert.
/// </summary>
public class ProMartMacdMartingaleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _balanceDivider;
	private readonly StrategyParam<int> _maxDoublingCount;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _macd1Fast;
	private readonly StrategyParam<int> _macd1Slow;
	private readonly StrategyParam<int> _macd1Signal;
	private readonly StrategyParam<int> _macd2Fast;
	private readonly StrategyParam<int> _macd2Slow;
	private readonly StrategyParam<int> _macd2Signal;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _macdPrimaryHistory = new(3);
	private readonly List<decimal> _macdSecondaryHistory = new(2);

	private MovingAverageConvergenceDivergence _macdPrimary = null!;
	private MovingAverageConvergenceDivergence _macdSecondary = null!;

	private Sides? _currentSide;
	private Sides? _lastTradeSide;
	private bool _lastTradeWasLoss;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private int _martingaleCounter;
	private decimal _lastEntryVolume;

	public decimal BalanceDivider
	{
		get => _balanceDivider.Value;
		set => _balanceDivider.Value = value;
	}

	public int MaxDoublingCount
	{
		get => _maxDoublingCount.Value;
		set => _maxDoublingCount.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int Macd1Fast
	{
		get => _macd1Fast.Value;
		set => _macd1Fast.Value = value;
	}

	public int Macd1Slow
	{
		get => _macd1Slow.Value;
		set => _macd1Slow.Value = value;
	}

	public int Macd1Signal
	{
		get => _macd1Signal.Value;
		set => _macd1Signal.Value = value;
	}

	public int Macd2Fast
	{
		get => _macd2Fast.Value;
		set => _macd2Fast.Value = value;
	}

	public int Macd2Slow
	{
		get => _macd2Slow.Value;
		set => _macd2Slow.Value = value;
	}

	public int Macd2Signal
	{
		get => _macd2Signal.Value;
		set => _macd2Signal.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ProMartMacdMartingaleStrategy()
	{
		_balanceDivider = Param(nameof(BalanceDivider), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Balance Divider", "Divider to derive base volume from portfolio equity.", "Risk")
			.SetCanOptimize(true);

		_maxDoublingCount = Param(nameof(MaxDoublingCount), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Max Doubling", "Maximum number of volume doublings after losses.", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Points", "Stop-loss distance expressed in price points.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1500m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Points", "Take-profit distance expressed in price points.", "Risk")
			.SetCanOptimize(true);

		_macd1Fast = Param(nameof(Macd1Fast), 5)
			.SetGreaterThanZero()
			.SetDisplay("MACD1 Fast", "Fast EMA period for the primary MACD.", "Signal")
			.SetCanOptimize(true);

		_macd1Slow = Param(nameof(Macd1Slow), 20)
			.SetGreaterThanZero()
			.SetDisplay("MACD1 Slow", "Slow EMA period for the primary MACD.", "Signal")
			.SetCanOptimize(true);

		_macd1Signal = Param(nameof(Macd1Signal), 3)
			.SetGreaterThanZero()
			.SetDisplay("MACD1 Signal", "Signal SMA period for the primary MACD.", "Signal")
			.SetCanOptimize(true);

		_macd2Fast = Param(nameof(Macd2Fast), 10)
			.SetGreaterThanZero()
			.SetDisplay("MACD2 Fast", "Fast EMA period for the secondary MACD.", "Filter")
			.SetCanOptimize(true);

		_macd2Slow = Param(nameof(Macd2Slow), 15)
			.SetGreaterThanZero()
			.SetDisplay("MACD2 Slow", "Slow EMA period for the secondary MACD.", "Filter")
			.SetCanOptimize(true);

		_macd2Signal = Param(nameof(Macd2Signal), 3)
			.SetGreaterThanZero()
			.SetDisplay("MACD2 Signal", "Signal SMA period for the secondary MACD.", "Filter")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Data type used for signal generation.", "General");

		Volume = 1m;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Configure MACD indicators that emulate the MQL version.
		_macdPrimary = new MovingAverageConvergenceDivergence
		{
			Fast = Macd1Fast,
			Slow = Macd1Slow,
			Signal = Macd1Signal
		};

		_macdSecondary = new MovingAverageConvergenceDivergence
		{
			Fast = Macd2Fast,
			Slow = Macd2Slow,
			Signal = Macd2Signal
		};

		// Subscribe to candle data and bind indicator updates.
		SubscribeCandles(CandleType)
			.BindEx(_macdPrimary, _macdSecondary, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdPrimaryValue, IIndicatorValue macdSecondaryValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdPrimary = (MovingAverageConvergenceDivergenceValue)macdPrimaryValue;
		var macdSecondary = (MovingAverageConvergenceDivergenceValue)macdSecondaryValue;

		if (macdPrimary.Macd is not decimal macd1 || macdSecondary.Macd is not decimal macd2)
			return;

		// Manage open positions before looking for new signals.
		if (ManageOpenPosition(candle))
		{
			UpdateHistory(macd1, macd2);
			return;
		}

		var direction = DetermineSignal();

		if (direction != 0 && Position == 0)
			OpenPosition(direction, candle);

		UpdateHistory(macd1, macd2);
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		if (_currentSide is null || Position == 0)
			return false;

		// Use candle extremes to approximate intrabar stop-loss and take-profit fills.
		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		var shouldClose = false;
		var isLoss = false;

		if (_currentSide == Sides.Buy)
		{
			if (StopLossPoints > 0m && candle.LowPrice <= _stopPrice)
			{
				shouldClose = true;
				isLoss = true;
			}
			else if (TakeProfitPoints > 0m && candle.HighPrice >= _takeProfitPrice)
			{
				shouldClose = true;
				isLoss = false;
			}
		}
		else if (_currentSide == Sides.Sell)
		{
			if (StopLossPoints > 0m && candle.HighPrice >= _stopPrice)
			{
				shouldClose = true;
				isLoss = true;
			}
			else if (TakeProfitPoints > 0m && candle.LowPrice <= _takeProfitPrice)
			{
				shouldClose = true;
				isLoss = false;
			}
		}

		if (!shouldClose)
			return false;

		// Close the active position at market and update martingale state.
		ClosePosition();

		_lastTradeSide = _currentSide;
		_lastTradeWasLoss = isLoss;
		_currentSide = null;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;

		if (isLoss)
		{
			// Keep the counter unchanged; it will be incremented when the next order size is calculated.
		}
		else
		{
			_martingaleCounter = 0;
		}

		return true;
	}

	private int DetermineSignal()
	{
		if (_macdPrimaryHistory.Count < 3 || _macdSecondaryHistory.Count < 2)
			return 0;

		var macd1Prev1 = _macdPrimaryHistory[0];
		var macd1Prev2 = _macdPrimaryHistory[1];
		var macd1Prev3 = _macdPrimaryHistory[2];
		var macd2Prev1 = _macdSecondaryHistory[0];
		var macd2Prev2 = _macdSecondaryHistory[1];

		// Detect local turning points that mimic the original MACD shape conditions.
		var buySignal = macd1Prev1 > macd1Prev2 && macd1Prev2 < macd1Prev3 && macd2Prev2 > macd2Prev1;
		var sellSignal = macd1Prev1 < macd1Prev2 && macd1Prev2 > macd1Prev3 && macd2Prev2 < macd2Prev1;

		if (_lastTradeSide is null)
		{
			return buySignal ? 1 : sellSignal ? -1 : 0;
		}

		if (_lastTradeSide == Sides.Buy)
		{
			if (_lastTradeWasLoss)
				return -1;

			if (buySignal)
				return 1;

			if (sellSignal)
				return -1;
		}
		else if (_lastTradeSide == Sides.Sell)
		{
			if (_lastTradeWasLoss)
				return 1;

			if (buySignal)
				return 1;

			if (sellSignal)
				return -1;
		}

		return 0;
	}

	private void OpenPosition(int direction, ICandleMessage candle)
	{
		var volume = CalculateOrderVolume();

		if (volume <= 0m)
			return;

		if (direction > 0)
		{
			BuyMarket(volume);
			_currentSide = Sides.Buy;
		}
		else
		{
			SellMarket(volume);
			_currentSide = Sides.Sell;
		}

		// Store entry price to monitor custom stop-loss and take-profit levels.
		_entryPrice = candle.ClosePrice;
		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		if (_currentSide == Sides.Buy)
		{
			_stopPrice = _entryPrice - stopDistance;
			_takeProfitPrice = _entryPrice + takeDistance;
		}
		else if (_currentSide == Sides.Sell)
		{
			_stopPrice = _entryPrice + stopDistance;
			_takeProfitPrice = _entryPrice - takeDistance;
		}
	}

	private decimal CalculateOrderVolume()
	{
		var baseVolume = CalculateBaseVolume();
		var step = Security?.VolumeStep ?? 1m;
		var minVolume = Security?.MinVolume ?? step;
		var maxVolume = Security?.MaxVolume;

		var volume = baseVolume;

		if (MaxDoublingCount > 0 && _lastTradeWasLoss)
		{
			if (_martingaleCounter < MaxDoublingCount && _lastEntryVolume > 0m)
			{
				volume = _lastEntryVolume * 2m;
				_martingaleCounter++;
			}
			else
			{
				_martingaleCounter = 0;
				volume = baseVolume;
			}
		}
		else
		{
			_martingaleCounter = 0;
			volume = baseVolume;
		}

		if (maxVolume != null)
			volume = Math.Min(volume, maxVolume.Value);

		volume = Math.Max(minVolume, Math.Floor(volume / step) * step);

		if (volume <= 0m)
			volume = minVolume;

		_lastEntryVolume = volume;

		return volume;
	}

	private decimal CalculateBaseVolume()
	{
		var step = Security?.VolumeStep ?? 1m;
		var minVolume = Security?.MinVolume ?? step;
		var maxVolume = Security?.MaxVolume;

		var balance = Portfolio?.CurrentValue ?? 0m;
		var baseVolume = 0m;

		if (balance > 0m && BalanceDivider > 0m)
		{
			var raw = balance / BalanceDivider;
			baseVolume = Math.Floor(raw / step) * step;
		}

		if (baseVolume <= 0m)
		{
			baseVolume = Volume > 0m ? Volume : minVolume;
		}

		baseVolume = Math.Max(minVolume, Math.Floor(baseVolume / step) * step);

		if (maxVolume != null)
			baseVolume = Math.Min(baseVolume, maxVolume.Value);

		if (baseVolume <= 0m)
			baseVolume = minVolume;

		return baseVolume;
	}

	private void UpdateHistory(decimal macd1, decimal macd2)
	{
		// Store MACD history so the next candle can analyze the previous pattern.
		_macdPrimaryHistory.Insert(0, macd1);
		if (_macdPrimaryHistory.Count > 3)
			_macdPrimaryHistory.RemoveAt(_macdPrimaryHistory.Count - 1);

		_macdSecondaryHistory.Insert(0, macd2);
		if (_macdSecondaryHistory.Count > 2)
			_macdSecondaryHistory.RemoveAt(_macdSecondaryHistory.Count - 1);
	}

	private void ClosePosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
