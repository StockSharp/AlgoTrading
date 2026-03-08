using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale grid that alternates long and short entries while doubling volume.
/// </summary>
public class MartinMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _stepPoints;
	private readonly StrategyParam<int> _entryOffsetPoints;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<int> _maxLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stepSize;
	private decimal _entryOffset;
	private decimal _lastTradePrice;
	private decimal _lastTradeVolume;
	private int _martingaleLevel;
	private Sides? _lastTradeSide;
	private bool _isClosing;
	private decimal? _initialPrice;

	/// <summary>
	/// Distance in points that defines when the next reversal is triggered.
	/// </summary>
	public int StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Offset in points for the initial breakout entry.
	/// </summary>
	public int EntryOffsetPoints
	{
		get => _entryOffsetPoints.Value;
		set => _entryOffsetPoints.Value = value;
	}

	/// <summary>
	/// Aggregated profit required to close the entire martingale cycle.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Maximum martingale doubling level before resetting.
	/// </summary>
	public int MaxLevel
	{
		get => _maxLevel.Value;
		set => _maxLevel.Value = value;
	}

	/// <summary>
	/// Candle type used to monitor the price.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MartinMartingaleStrategy"/>.
	/// </summary>
	public MartinMartingaleStrategy()
	{
		_stepPoints = Param(nameof(StepPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Step (points)", "Distance multiplier for reversals", "General")
			;

		_entryOffsetPoints = Param(nameof(EntryOffsetPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Entry Offset (points)", "Offset for initial breakout entry", "General")
			;

		_profitTarget = Param(nameof(ProfitTarget), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target", "Total profit to close all positions", "Risk")
			;

		_maxLevel = Param(nameof(MaxLevel), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Level", "Maximum martingale levels", "Risk")
			;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles for price monitoring", "Data");
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
		ResetCycle();
		_isClosing = false;
		_initialPrice = null;
		_stepSize = 0;
		_entryOffset = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		UpdateStepSettings();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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
			return;

		UpdateStepSettings();

		if (_stepSize <= 0m || Volume <= 0m)
			return;

		var price = candle.ClosePrice;

		// If closing, flatten and wait
		if (_isClosing)
		{
			if (Position == 0)
			{
				_isClosing = false;
				ResetCycle();
			}
			return;
		}

		// If flat after a cycle, reset
		if (Position == 0 && _martingaleLevel > 0)
		{
			ResetCycle();
		}

		// Check profit target
		if (ProfitTarget > 0m && PnL >= ProfitTarget && Position != 0)
		{
			_isClosing = true;
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
			return;
		}

		// Max level reached -> close and reset
		if (_martingaleLevel >= MaxLevel && Position != 0)
		{
			_isClosing = true;
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
			return;
		}

		// Initial entry: wait for breakout from first candle
		if (_martingaleLevel == 0 && Position == 0)
		{
			if (!_initialPrice.HasValue)
			{
				_initialPrice = price;
				return;
			}

			if (_entryOffset <= 0m)
				return;

			if (price >= _initialPrice.Value + _entryOffset)
			{
				BuyMarket();
				_lastTradePrice = price;
				_lastTradeVolume = Volume;
				_lastTradeSide = Sides.Buy;
				_martingaleLevel = 1;
				_initialPrice = null;
			}
			else if (price <= _initialPrice.Value - _entryOffset)
			{
				SellMarket();
				_lastTradePrice = price;
				_lastTradeVolume = Volume;
				_lastTradeSide = Sides.Sell;
				_martingaleLevel = 1;
				_initialPrice = null;
			}

			return;
		}

		if (_lastTradeSide is null || _martingaleLevel == 0)
			return;

		var threshold = _stepSize;

		if (_lastTradeSide == Sides.Buy)
		{
			if (price <= _lastTradePrice - threshold)
			{
				var nextVolume = _lastTradeVolume * 2m;
				var totalVolume = nextVolume + Math.Abs(Position);
				SellMarket();
				_lastTradePrice = price;
				_lastTradeVolume = nextVolume;
				_lastTradeSide = Sides.Sell;
				_martingaleLevel++;
			}
		}
		else
		{
			if (price >= _lastTradePrice + threshold)
			{
				var nextVolume = _lastTradeVolume * 2m;
				var totalVolume = nextVolume + Math.Abs(Position);
				BuyMarket();
				_lastTradePrice = price;
				_lastTradeVolume = nextVolume;
				_lastTradeSide = Sides.Buy;
				_martingaleLevel++;
			}
		}
	}

	private void UpdateStepSettings()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			priceStep = 1m;
		}

		_stepSize = StepPoints * priceStep;
		_entryOffset = EntryOffsetPoints * priceStep;
	}

	private void ResetCycle()
	{
		_martingaleLevel = 0;
		_lastTradePrice = 0m;
		_lastTradeVolume = 0m;
		_lastTradeSide = null;
	}
}
