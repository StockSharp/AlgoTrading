using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Detects runs of identical candles and trades in the direction of the streak.
/// </summary>
public class NCandlesV6Strategy : Strategy
{
	/// <summary>
	/// Defines how positions are closed when a candle breaks the streak.
	/// </summary>
	public enum BlackSheepCloseMode
	{
		/// <summary>
		/// Close every open position regardless of direction.
		/// </summary>
		All,

		/// <summary>
		/// Close only positions that oppose the detected streak.
		/// </summary>
		Opposite,

		/// <summary>
		/// Close only positions that follow the detected streak.
		/// </summary>
		Unidirectional,
	}

	private readonly StrategyParam<int> _candlesCount;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _maxPositionVolume;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<BlackSheepCloseMode> _blackSheepMode;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal? _trailingLong;
	private decimal? _trailingShort;
	private int _streakDirection;
	private int _bullCount;
	private int _bearCount;
	private bool _blackSheepTriggered;

	public int CandlesCount { get => _candlesCount.Value; set => _candlesCount.Value = value; }
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public decimal TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }
	public decimal TrailingStepPips { get => _trailingStepPips.Value; set => _trailingStepPips.Value = value; }
	public decimal MaxPositionVolume { get => _maxPositionVolume.Value; set => _maxPositionVolume.Value = value; }
	public bool UseTradingHours { get => _useTradingHours.Value; set => _useTradingHours.Value = value; }
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }
	public BlackSheepCloseMode ClosingMode { get => _blackSheepMode.Value; set => _blackSheepMode.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NCandlesV6Strategy()
	{
		_candlesCount = Param(nameof(CandlesCount), 3)
		.SetGreaterThanZero()
		.SetDisplay("Candles", "Number of identical candles", "Pattern");

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Base order size", "Orders");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 4m)
		.SetDisplay("Trailing Step (pips)", "Minimum move before trailing updates", "Risk");

		_maxPositionVolume = Param(nameof(MaxPositionVolume), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Max Position Volume", "Maximum absolute net position", "Risk");

		_useTradingHours = Param(nameof(UseTradingHours), true)
		.SetDisplay("Use Trading Hours", "Enable trading window", "Timing");

		_startHour = Param(nameof(StartHour), 11)
		.SetDisplay("Start Hour", "Hour when trading can start", "Timing");

		_endHour = Param(nameof(EndHour), 18)
		.SetDisplay("End Hour", "Hour when trading stops", "Timing");

		_blackSheepMode = Param(nameof(ClosingMode), BlackSheepCloseMode.All)
		.SetDisplay("Closing Mode", "Reaction to a black sheep candle", "Pattern");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "Pattern");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (UseTradingHours && StartHour >= EndHour)
		throw new InvalidOperationException("Start hour must be less than end hour when trading window is enabled.");

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		Volume = OrderVolume;

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		UpdateTrailingLevels(candle);

		if (ApplyRiskManagement(candle))
		return;

		var direction = GetDirection(candle);

		if (direction == 0)
		{
			if (_streakDirection != 0 && !_blackSheepTriggered)
			HandleBlackSheep(_streakDirection);

			ResetCounters();
			return;
		}

		if (_streakDirection == direction)
		{
			if (direction == 1)
			{
				_bullCount = Math.Min(CandlesCount, _bullCount + 1);
				_bearCount = 0;
			}
			else
			{
				_bearCount = Math.Min(CandlesCount, _bearCount + 1);
				_bullCount = 0;
			}
		}
		else
		{
			if (_streakDirection != 0 && !_blackSheepTriggered)
			HandleBlackSheep(_streakDirection);

			_streakDirection = direction;
			_bullCount = direction == 1 ? 1 : 0;
			_bearCount = direction == -1 ? 1 : 0;
		}

		var allowTrading = !UseTradingHours || IsWithinTradingHours(candle.OpenTime);

		if (_bullCount >= CandlesCount && allowTrading)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (_bearCount >= CandlesCount && allowTrading)
		{
			EnterShort(candle.ClosePrice);
		}
	}

	private void EnterLong(decimal price)
	{
		if (OrderVolume <= 0m)
		return;

		var volume = OrderVolume;

		if (Position < 0m)
		volume += Math.Abs(Position);

		var projected = Position + volume;

		if (projected > MaxPositionVolume)
		return;

		BuyMarket(volume);

		_entryPrice = price;
		_stopLossPrice = StopLossPips > 0m ? price - GetPriceOffset(StopLossPips) : null;
		_takeProfitPrice = TakeProfitPips > 0m ? price + GetPriceOffset(TakeProfitPips) : null;
		_trailingLong = null;
		_trailingShort = null;
		_blackSheepTriggered = false;
	}

	private void EnterShort(decimal price)
	{
		if (OrderVolume <= 0m)
		return;

		var volume = OrderVolume;

		if (Position > 0m)
		volume += Math.Abs(Position);

		var projected = Position - volume;

		if (Math.Abs(projected) > MaxPositionVolume)
		return;

		SellMarket(volume);

		_entryPrice = price;
		_stopLossPrice = StopLossPips > 0m ? price + GetPriceOffset(StopLossPips) : null;
		_takeProfitPrice = TakeProfitPips > 0m ? price - GetPriceOffset(TakeProfitPips) : null;
		_trailingLong = null;
		_trailingShort = null;
		_blackSheepTriggered = false;
	}

	private void HandleBlackSheep(int direction)
	{
		if (direction == 0 || _blackSheepTriggered)
		return;

		switch (ClosingMode)
		{
			case BlackSheepCloseMode.All:
			{
				ClosePosition();
				break;
			}

			case BlackSheepCloseMode.Opposite:
			{
				if (direction == 1 && Position < 0m)
				{
					BuyMarket(Math.Abs(Position));
					ResetPositionState();
				}
				else if (direction == -1 && Position > 0m)
				{
					SellMarket(Math.Abs(Position));
					ResetPositionState();
				}

				break;
			}

			case BlackSheepCloseMode.Unidirectional:
			{
				if (direction == 1 && Position > 0m)
				{
					SellMarket(Math.Abs(Position));
					ResetPositionState();
				}
				else if (direction == -1 && Position < 0m)
				{
					BuyMarket(Math.Abs(Position));
					ResetPositionState();
				}

				break;
			}
		}

		_blackSheepTriggered = true;
	}

	private void ClosePosition()
	{
		if (Position > 0m)
		{
			SellMarket(Math.Abs(Position));
			ResetPositionState();
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
		}
	}

	private void UpdateTrailingLevels(ICandleMessage candle)
	{
		var trailingStop = GetPriceOffset(TrailingStopPips);

		if (trailingStop <= 0m)
		return;

		var trailingStep = GetPriceOffset(TrailingStepPips);

		if (Position > 0m)
		{
			var profit = candle.ClosePrice - _entryPrice;

			if (profit > trailingStop + trailingStep)
			{
				var candidate = candle.ClosePrice - trailingStop;

				if (_trailingLong == null || candidate > _trailingLong.Value + trailingStep)
				_trailingLong = candidate;
			}
		}
		else if (Position < 0m)
		{
			var profit = _entryPrice - candle.ClosePrice;

			if (profit > trailingStop + trailingStep)
			{
				var candidate = candle.ClosePrice + trailingStop;

				if (_trailingShort == null || candidate < _trailingShort.Value - trailingStep)
				_trailingShort = candidate;
			}
		}
	}

	private bool ApplyRiskManagement(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_stopLossPrice is decimal longSl && candle.LowPrice <= longSl)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal longTp && candle.HighPrice >= longTp)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}

			if (_trailingLong is decimal trail && candle.LowPrice <= trail)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0m)
		{
			var absPosition = Math.Abs(Position);

			if (_stopLossPrice is decimal shortSl && candle.HighPrice >= shortSl)
			{
				BuyMarket(absPosition);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal shortTp && candle.LowPrice <= shortTp)
			{
				BuyMarket(absPosition);
				ResetPositionState();
				return true;
			}

			if (_trailingShort is decimal trail && candle.HighPrice >= trail)
			{
				BuyMarket(absPosition);
				ResetPositionState();
				return true;
			}
		}

		return false;
	}

	private void ResetCounters()
	{
		_streakDirection = 0;
		_bullCount = 0;
		_bearCount = 0;
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_trailingLong = null;
		_trailingShort = null;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.TimeOfDay.Hours;
		return hour >= StartHour && hour <= EndHour;
	}

	private decimal GetPriceOffset(decimal pips)
	{
		if (pips <= 0m)
		return 0m;

		return pips * _pipSize;
	}

	private static int GetDirection(ICandleMessage candle)
	{
		if (candle.ClosePrice > candle.OpenPrice)
		return 1;

		if (candle.ClosePrice < candle.OpenPrice)
		return -1;

		return 0;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
		return 1m;

		var decimals = CountDecimals(step);

		return decimals == 3 || decimals == 5
		? step * 10m
		: step;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);
		var count = 0;

		while (value != Math.Truncate(value) && count < 10)
		{
			value *= 10m;
			count++;
		}

		return count;
	}
}
