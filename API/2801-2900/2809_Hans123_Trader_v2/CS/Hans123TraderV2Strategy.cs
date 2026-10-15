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
/// Hans123 Trader v2 breakout strategy converted from the original MQL expert.
/// Enters on breakout of recent range extremes and manages trailing protection.
/// </summary>
public class Hans123TraderV2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _breakoutPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private decimal _entryPrice;
	private decimal _pipSize;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;
	private decimal _highestStopPrice;
	private decimal? _prevBreakoutHigh;
	private decimal? _prevBreakoutLow;

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Session start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Lookback length for calculating highs and lows.
	/// </summary>
	public int BreakoutPeriod
	{
		get => _breakoutPeriod.Value;
		set => _breakoutPeriod.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="Hans123TraderV2Strategy"/>.
	/// </summary>
	public Hans123TraderV2Strategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Target distance", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Extra profit before trailing", "Risk");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Session start hour", "Session");

		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Session end hour", "Session");

		_breakoutPeriod = Param(nameof(BreakoutPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Period", "High/low lookback", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Processed candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pipSize = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_trailingStopDistance = 0m;
		_trailingStepDistance = 0m;
		_highest = null;
		_lowest = null;
		_entryPrice = 0m;
		_highestStopPrice = 0m;
		_prevBreakoutHigh = null;
		_prevBreakoutLow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pipSize = Security?.PriceStep ?? 1m;
		UpdateDistanceCache();

		_highest = new Highest { Length = BreakoutPeriod };
		_lowest = new Lowest { Length = BreakoutPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void UpdateDistanceCache()
	{
		_stopLossDistance = StopLossPips * _pipSize;
		_takeProfitDistance = TakeProfitPips * _pipSize;
		_trailingStopDistance = TrailingStopPips * _pipSize;
		_trailingStepDistance = TrailingStepPips * _pipSize;
	}

	private void ProcessCandle(ICandleMessage candle, decimal breakoutHigh, decimal breakoutLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		// Manage existing position: trailing stop and SL/TP
		if (Position != 0)
		{
			ManagePosition(candle);
			return;
		}

		// Session filter
		var hour = candle.OpenTime.TimeOfDay.Hours;
		if (hour < StartHour || hour >= EndHour)
		{
			_prevBreakoutHigh = breakoutHigh;
			_prevBreakoutLow = breakoutLow;
			return;
		}

		// Breakout entry: buy when price breaks above previous bar's high, sell when below previous bar's low
		if (_prevBreakoutHigh.HasValue && _prevBreakoutLow.HasValue)
		{
			if (candle.HighPrice > _prevBreakoutHigh.Value)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_highestStopPrice = 0m;
			}
			else if (candle.LowPrice < _prevBreakoutLow.Value)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_highestStopPrice = 0m;
			}
		}

		_prevBreakoutHigh = breakoutHigh;
		_prevBreakoutLow = breakoutLow;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		var price = candle.ClosePrice;

		if (Position > 0)
		{
			// Check stop loss
			if (_stopLossDistance > 0m && candle.LowPrice <= _entryPrice - _stopLossDistance)
			{
				SellMarket(Position);
				return;
			}

			// Check take profit
			if (_takeProfitDistance > 0m && candle.HighPrice >= _entryPrice + _takeProfitDistance)
			{
				SellMarket(Position);
				return;
			}

			// Trailing stop
			if (_trailingStopDistance > 0m)
			{
				var moveFromEntry = price - _entryPrice;
				if (moveFromEntry > _trailingStopDistance + _trailingStepDistance)
				{
					var newStop = price - _trailingStopDistance;
					if (newStop > _highestStopPrice + _trailingStepDistance)
						_highestStopPrice = newStop;

					if (_highestStopPrice > 0m && candle.LowPrice <= _highestStopPrice)
					{
						SellMarket(Position);
						return;
					}
				}
			}
		}
		else if (Position < 0)
		{
			var vol = Math.Abs(Position);

			// Check stop loss
			if (_stopLossDistance > 0m && candle.HighPrice >= _entryPrice + _stopLossDistance)
			{
				BuyMarket(vol);
				return;
			}

			// Check take profit
			if (_takeProfitDistance > 0m && candle.LowPrice <= _entryPrice - _takeProfitDistance)
			{
				BuyMarket(vol);
				return;
			}

			// Trailing stop
			if (_trailingStopDistance > 0m)
			{
				var moveFromEntry = _entryPrice - price;
				if (moveFromEntry > _trailingStopDistance + _trailingStepDistance)
				{
					var newStop = price + _trailingStopDistance;
					if (_highestStopPrice == 0m || newStop < _highestStopPrice - _trailingStepDistance)
						_highestStopPrice = newStop;

					if (_highestStopPrice > 0m && candle.HighPrice >= _highestStopPrice)
					{
						BuyMarket(vol);
						return;
					}
				}
			}
		}
	}
}
