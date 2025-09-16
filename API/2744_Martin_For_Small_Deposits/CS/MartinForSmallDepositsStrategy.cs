using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale averaging strategy for small deposits.
/// </summary>
public class MartinForSmallDepositsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stepPips;
	private readonly StrategyParam<int> _barsToSkip;
	private readonly StrategyParam<decimal> _increaseFactor;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _minProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _positionVolume;
	private decimal _avgPrice;
	private decimal _extremePrice;
	private decimal _lastEntryPrice;
	private int _currentTradeCount;
	private int _currentDirection;
	private int _barsSinceLastEntry;
	private decimal _pendingOpenVolume;
	private int _pendingOpenDirection;
	private decimal _pendingCloseVolume;
	private int _pendingCloseDirection;
	private decimal _pipSize;
	private readonly decimal[] _closeHistory = new decimal[15];
	private int _closeHistoryCount;
	private int _latestIndex = -1;

	/// <summary>
	/// Initializes a new instance of the <see cref="MartinForSmallDepositsStrategy"/> class.
	/// </summary>
	public MartinForSmallDepositsStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 0.01m)
			.SetDisplay("Initial Volume", "Base lot size for the first order", "Position Sizing")
			.SetCanOptimize();

		_takeProfitPips = Param(nameof(TakeProfitPips), 65)
			.SetDisplay("Take Profit (pips)", "Take profit distance from the latest entry", "Risk")
			.SetCanOptimize();

		_stepPips = Param(nameof(StepPips), 15)
			.SetDisplay("Step (pips)", "Adverse price move required to add a new trade", "Position Sizing")
			.SetCanOptimize();

		_barsToSkip = Param(nameof(BarsToSkip), 45)
			.SetDisplay("Bars To Skip", "Number of finished candles to wait before averaging", "Timing")
			.SetCanOptimize();

		_increaseFactor = Param(nameof(IncreaseFactor), 1.7m)
			.SetDisplay("Increase Factor", "Multiplier applied to the volume of each new order", "Position Sizing")
			.SetCanOptimize();

		_maxVolume = Param(nameof(MaxVolume), 6m)
			.SetDisplay("Max Volume", "Maximum allowed aggregated volume", "Risk")
			.SetCanOptimize();

		_minProfit = Param(nameof(MinProfit), 10m)
			.SetDisplay("Min Profit", "Net profit threshold to close all positions", "Risk")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signal generation", "General");
	}

	/// <summary>
	/// Base lot size for the first trade in the sequence.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Price move in pips that triggers an averaging order.
	/// </summary>
	public int StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Number of candles to wait between additional averaging trades.
	/// </summary>
	public int BarsToSkip
	{
		get => _barsToSkip.Value;
		set => _barsToSkip.Value = value;
	}

	/// <summary>
	/// Multiplier for the martingale position sizing.
	/// </summary>
	public decimal IncreaseFactor
	{
		get => _increaseFactor.Value;
		set => _increaseFactor.Value = value;
	}

	/// <summary>
	/// Maximum allowed aggregated volume across all open trades.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Profit target that closes the whole grid.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Candle type used to build signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_positionVolume = 0m;
		_avgPrice = 0m;
		_extremePrice = 0m;
		_lastEntryPrice = 0m;
		_currentTradeCount = 0;
		_currentDirection = 0;
		_barsSinceLastEntry = 0;
		_pendingOpenVolume = 0m;
		_pendingOpenDirection = 0;
		_pendingCloseVolume = 0m;
		_pendingCloseDirection = 0;
		_pipSize = 0m;
		Array.Clear(_closeHistory, 0, _closeHistory.Length);
		_closeHistoryCount = 0;
		_latestIndex = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateCloseHistory(candle.ClosePrice);

		var pipSize = EnsurePipSize();
		if (pipSize <= 0m)
			return;

		var stepDistance = StepPips > 0 ? StepPips * pipSize : 0m;
		var takeProfitDistance = TakeProfitPips > 0 ? TakeProfitPips * pipSize : 0m;

		var hasPosition = _positionVolume > 0m || Position != 0m || _pendingOpenDirection != 0 || _pendingCloseDirection != 0;

		if (!hasPosition)
		{
			if (!IsHistoryReady())
				return;

			var referenceClose = GetReferenceClose();
			if (candle.ClosePrice < referenceClose)
			{
				TryOpenBuy(candle.ClosePrice);
			}
			else if (candle.ClosePrice > referenceClose)
			{
				TryOpenSell(candle.ClosePrice);
			}

			return;
		}

		if (_pendingCloseDirection != 0)
			return;

		if (_positionVolume <= 0m || _currentDirection == 0)
			return;

		_barsSinceLastEntry++;

		var price = candle.ClosePrice;
		var openPnL = CalculateOpenProfit(price);

		if (openPnL > MinProfit)
		{
			CloseAllPositions();
			return;
		}

		if (_currentDirection > 0)
		{
			if (takeProfitDistance > 0m && price >= _lastEntryPrice + takeProfitDistance)
			{
				CloseAllPositions();
				return;
			}

			if (_barsSinceLastEntry <= BarsToSkip)
				return;

			if (stepDistance > 0m && _extremePrice - price > stepDistance)
				TryOpenBuy(price);
		}
		else if (_currentDirection < 0)
		{
			if (takeProfitDistance > 0m && price <= _lastEntryPrice - takeProfitDistance)
			{
				CloseAllPositions();
				return;
			}

			if (_barsSinceLastEntry <= BarsToSkip)
				return;

			if (stepDistance > 0m && price - _extremePrice > stepDistance)
				TryOpenSell(price);
		}
	}

	private void TryOpenBuy(decimal price)
	{
		if (_pendingOpenDirection != 0 && _pendingOpenDirection != 1)
			return;

		var volume = GetNextVolume(1);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_pendingOpenDirection = 1;
		_pendingOpenVolume += volume;
	}

	private void TryOpenSell(decimal price)
	{
		if (_pendingOpenDirection != 0 && _pendingOpenDirection != -1)
			return;

		var volume = GetNextVolume(-1);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_pendingOpenDirection = -1;
		_pendingOpenVolume += volume;
	}

	private void CloseAllPositions()
	{
		if (_pendingCloseDirection != 0)
			return;

		var volume = Position;

		if (volume > 0m)
		{
			SellMarket(volume);
			_pendingCloseDirection = -1;
			_pendingCloseVolume += volume;
		}
		else if (volume < 0m)
		{
			var closeVolume = -volume;
			BuyMarket(closeVolume);
			_pendingCloseDirection = 1;
			_pendingCloseVolume += closeVolume;
		}
		else if (_positionVolume > 0m)
		{
			if (_currentDirection > 0)
			{
				SellMarket(_positionVolume);
				_pendingCloseDirection = -1;
				_pendingCloseVolume += _positionVolume;
			}
			else if (_currentDirection < 0)
			{
				BuyMarket(_positionVolume);
				_pendingCloseDirection = 1;
				_pendingCloseVolume += _positionVolume;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null)
			return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_pendingCloseDirection == 1)
			{
				ApplyClose(volume);
				_pendingCloseVolume -= volume;
				if (_pendingCloseVolume <= 0m)
					_pendingCloseDirection = 0;
				return;
			}

			if (_pendingOpenDirection == 1)
			{
				ApplyLongOpen(price, volume);
				_pendingOpenVolume -= volume;
				if (_pendingOpenVolume <= 0m)
					_pendingOpenDirection = 0;
				return;
			}

			if (_currentDirection < 0)
				ApplyClose(volume);
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (_pendingCloseDirection == -1)
			{
				ApplyClose(volume);
				_pendingCloseVolume -= volume;
				if (_pendingCloseVolume <= 0m)
					_pendingCloseDirection = 0;
				return;
			}

			if (_pendingOpenDirection == -1)
			{
				ApplyShortOpen(price, volume);
				_pendingOpenVolume -= volume;
				if (_pendingOpenVolume <= 0m)
					_pendingOpenDirection = 0;
				return;
			}

			if (_currentDirection > 0)
				ApplyClose(volume);
		}
	}

	private void ApplyLongOpen(decimal price, decimal volume)
	{
		var previousVolume = _positionVolume;
		_positionVolume += volume;
		_avgPrice = previousVolume == 0m ? price : ((_avgPrice * previousVolume) + (price * volume)) / _positionVolume;
		_extremePrice = previousVolume == 0m ? price : Math.Min(_extremePrice, price);
		_lastEntryPrice = price;
		_currentDirection = 1;
		_currentTradeCount++;
		_barsSinceLastEntry = 0;
	}

	private void ApplyShortOpen(decimal price, decimal volume)
	{
		var previousVolume = _positionVolume;
		_positionVolume += volume;
		_avgPrice = previousVolume == 0m ? price : ((_avgPrice * previousVolume) + (price * volume)) / _positionVolume;
		_extremePrice = previousVolume == 0m ? price : Math.Max(_extremePrice, price);
		_lastEntryPrice = price;
		_currentDirection = -1;
		_currentTradeCount++;
		_barsSinceLastEntry = 0;
	}

	private void ApplyClose(decimal volume)
	{
		_positionVolume -= volume;
		if (_positionVolume <= 0m)
		{
			ResetPositionState();
		}
	}

	private void ResetPositionState()
	{
		_positionVolume = 0m;
		_avgPrice = 0m;
		_extremePrice = 0m;
		_lastEntryPrice = 0m;
		_currentTradeCount = 0;
		_currentDirection = 0;
		_barsSinceLastEntry = 0;
		_pendingOpenDirection = 0;
		_pendingOpenVolume = 0m;
		_pendingCloseDirection = 0;
		_pendingCloseVolume = 0m;
	}

	private decimal CalculateOpenProfit(decimal price)
	{
		if (_currentDirection > 0)
			return (price - _avgPrice) * _positionVolume;

		if (_currentDirection < 0)
			return (_avgPrice - price) * _positionVolume;

		return 0m;
	}

	private decimal GetNextVolume(int direction)
	{
		var baseVolume = InitialVolume;
		if (baseVolume <= 0m)
			return 0m;

		var depth = _currentDirection == direction ? _currentTradeCount : 0;
		var factor = IncreaseFactor <= 0m ? 1m : (decimal)Math.Pow((double)IncreaseFactor, depth);
		var volume = baseVolume * factor;

		if (MaxVolume > 0m && volume > MaxVolume)
			volume = MaxVolume;

		volume = NormalizeVolume(volume);

		return volume;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return 0m;

		if (security.VolumeStep is decimal step && step > 0m)
		{
			var steps = decimal.Truncate(volume / step);
			volume = steps * step;
		}

		if (security.MinVolume is decimal min && volume < min)
			return 0m;

		if (security.MaxVolume is decimal max && volume > max)
			volume = max;

		return volume;
	}

	private decimal EnsurePipSize()
	{
		if (_pipSize > 0m)
			return _pipSize;

		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step == 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null)
			{
				step = (decimal)Math.Pow(10, -decimals.Value);
			}
		}

		if (step == 0m)
			return 0m;

		var decimalsCount = security.Decimals ?? 0;
		_pipSize = (decimalsCount == 3 || decimalsCount == 5) ? step * 10m : step;

		if (_pipSize == 0m)
			_pipSize = step;

		return _pipSize;
	}

	private void UpdateCloseHistory(decimal closePrice)
	{
		if (_closeHistory.Length == 0)
			return;

		_latestIndex = (_latestIndex + 1) % _closeHistory.Length;
		_closeHistory[_latestIndex] = closePrice;

		if (_closeHistoryCount < _closeHistory.Length)
			_closeHistoryCount++;
	}

	private bool IsHistoryReady()
	{
		return _closeHistoryCount >= _closeHistory.Length;
	}

	private decimal GetReferenceClose()
	{
		var index = (_latestIndex + 1) % _closeHistory.Length;
		return _closeHistory[index];
	}
}
