using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot and EMA based strategy adapted from the PivotEMA3RLHv4 Expert Advisor.
/// Combines EMA(3) on open and close prices, Heiken Ashi candles and daily pivot level.
/// Includes optional stop-loss, take-profit and several trailing stop modes.
/// </summary>
public class PivotEma3Rlhv4Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<int> _trailingStopType;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _firstMovePips;
	private readonly StrategyParam<int> _firstStopLossPips;
	private readonly StrategyParam<int> _secondMovePips;
	private readonly StrategyParam<int> _secondStopLossPips;
	private readonly StrategyParam<int> _thirdMovePips;
	private readonly StrategyParam<int> _trailingStop3Pips;

	private ExponentialMovingAverage _emaOpen;
	private ExponentialMovingAverage _emaClose;
	private AverageTrueRange _atr1;
	private AverageTrueRange _atr4;
	private AverageTrueRange _atr8;
	private AverageTrueRange _atr12;
	private AverageTrueRange _atr24;

	private decimal? _prevEmaOpen;
	private decimal? _prevHaOpen;
	private decimal? _prevHaClose;

	private decimal? _prevAtr1;
	private decimal? _prevAtr1Prev;
	private decimal? _prevAtr4;
	private decimal? _prevAtr8;
	private decimal? _prevAtr12;
	private decimal? _prevAtr24;

	private decimal _pivot;
	private decimal _prevDailyHigh;
	private decimal _prevDailyLow;
	private decimal _prevDailyClose;
	private bool _pivotReady;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	private decimal _pipSize;

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables trailing stop logic.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop mode. Matches original expert options 1..3.
	/// </summary>
	public int TrailingStopType
	{
		get => _trailingStopType.Value;
		set => _trailingStopType.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for type 2 in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// First profit level distance for type 3 trailing in pips.
	/// </summary>
	public int FirstMovePips
	{
		get => _firstMovePips.Value;
		set => _firstMovePips.Value = value;
	}

	/// <summary>
	/// Stop loss offset applied after first move (type 3).
	/// </summary>
	public int FirstStopLossPips
	{
		get => _firstStopLossPips.Value;
		set => _firstStopLossPips.Value = value;
	}

	/// <summary>
	/// Second profit level distance for type 3 trailing in pips.
	/// </summary>
	public int SecondMovePips
	{
		get => _secondMovePips.Value;
		set => _secondMovePips.Value = value;
	}

	/// <summary>
	/// Stop loss offset applied after second move (type 3).
	/// </summary>
	public int SecondStopLossPips
	{
		get => _secondStopLossPips.Value;
		set => _secondStopLossPips.Value = value;
	}

	/// <summary>
	/// Third profit level distance for type 3 trailing in pips.
	/// </summary>
	public int ThirdMovePips
	{
		get => _thirdMovePips.Value;
		set => _thirdMovePips.Value = value;
	}

	/// <summary>
	/// Trailing distance used after the third move (type 3).
	/// </summary>
	public int TrailingStop3Pips
	{
		get => _trailingStop3Pips.Value;
		set => _trailingStop3Pips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PivotEma3Rlhv4Strategy"/>.
	/// </summary>
	public PivotEma3Rlhv4Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Working candle type", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Initial stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 350)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in pips", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop management", "Risk");

		_trailingStopType = Param(nameof(TrailingStopType), 2)
			.SetDisplay("Trailing Type", "Trailing stop type (1-3)", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing distance for type 2", "Risk");

		_firstMovePips = Param(nameof(FirstMovePips), 30)
			.SetGreaterThanZero()
			.SetDisplay("First Move", "First profit threshold for type 3", "Risk");

		_firstStopLossPips = Param(nameof(FirstStopLossPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("First Stop", "Stop offset after first move", "Risk");

		_secondMovePips = Param(nameof(SecondMovePips), 40)
			.SetGreaterThanZero()
			.SetDisplay("Second Move", "Second profit threshold for type 3", "Risk");

		_secondStopLossPips = Param(nameof(SecondStopLossPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Second Stop", "Stop offset after second move", "Risk");

		_thirdMovePips = Param(nameof(ThirdMovePips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Third Move", "Third profit threshold for type 3", "Risk");

		_trailingStop3Pips = Param(nameof(TrailingStop3Pips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop 3", "Trailing distance after third move", "Risk");
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

		_emaOpen = default;
		_emaClose = default;
		_atr1 = default;
		_atr4 = default;
		_atr8 = default;
		_atr12 = default;
		_atr24 = default;

		_prevEmaOpen = null;
		_prevHaOpen = null;
		_prevHaClose = null;

		_prevAtr1 = null;
		_prevAtr1Prev = null;
		_prevAtr4 = null;
		_prevAtr8 = null;
		_prevAtr12 = null;
		_prevAtr24 = null;

		_pivot = 0m;
		_prevDailyHigh = 0m;
		_prevDailyLow = 0m;
		_prevDailyClose = 0m;
		_pivotReady = false;

		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;

		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0.0001m;

		_emaOpen = new ExponentialMovingAverage { Length = 3 };
		_emaClose = new ExponentialMovingAverage { Length = 3 };
		_atr1 = new AverageTrueRange { Length = 1 };
		_atr4 = new AverageTrueRange { Length = 4 };
		_atr8 = new AverageTrueRange { Length = 8 };
		_atr12 = new AverageTrueRange { Length = 12 };
		_atr24 = new AverageTrueRange { Length = 24 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription.Bind(ProcessDailyCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaOpen);
			DrawIndicator(area, _emaClose);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_pivotReady)
			_pivot = (_prevDailyHigh + _prevDailyLow + _prevDailyClose) / 3m;

		_prevDailyHigh = candle.HighPrice;
		_prevDailyLow = candle.LowPrice;
		_prevDailyClose = candle.ClosePrice;
		_pivotReady = true;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageActivePosition(candle);

		var emaOpenValue = _emaOpen.Process(new CandleIndicatorValue(candle, candle.OpenPrice));
		var emaCloseValue = _emaClose.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		var atr1Value = _atr1.Process(new CandleIndicatorValue(_atr1, candle));
		var atr4Value = _atr4.Process(new CandleIndicatorValue(_atr4, candle));
		var atr8Value = _atr8.Process(new CandleIndicatorValue(_atr8, candle));
		var atr12Value = _atr12.Process(new CandleIndicatorValue(_atr12, candle));
		var atr24Value = _atr24.Process(new CandleIndicatorValue(_atr24, candle));

		if (!emaOpenValue.IsFinal || !emaCloseValue.IsFinal || !atr1Value.IsFinal || !atr4Value.IsFinal || !atr8Value.IsFinal || !atr12Value.IsFinal || !atr24Value.IsFinal)
			return;

		var emaOpen = emaOpenValue.GetValue<decimal>();
		var emaClose = emaCloseValue.GetValue<decimal>();
		var atr1 = atr1Value.GetValue<decimal>();
		var atr4 = atr4Value.GetValue<decimal>();
		var atr8 = atr8Value.GetValue<decimal>();
		var atr12 = atr12Value.GetValue<decimal>();
		var atr24 = atr24Value.GetValue<decimal>();

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;

		if (_prevHaOpen is null || _prevHaClose is null)
		{
			_prevHaOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			_prevHaClose = haClose;
			_prevEmaOpen = emaOpen;
			_prevAtr1 = atr1;
			_prevAtr4 = atr4;
			_prevAtr8 = atr8;
			_prevAtr12 = atr12;
			_prevAtr24 = atr24;
			return;
		}

		if (_prevEmaOpen is null || !_pivotReady || _prevAtr4 is null || _prevAtr8 is null || _prevAtr12 is null || _prevAtr24 is null)
		{
			_prevEmaOpen = emaOpen;
			_prevAtr1Prev = _prevAtr1;
			_prevAtr1 = atr1;
			_prevAtr4 = atr4;
			_prevAtr8 = atr8;
			_prevAtr12 = atr12;
			_prevAtr24 = atr24;
			_prevHaOpen = (_prevHaOpen.Value + _prevHaClose.Value) / 2m;
			_prevHaClose = haClose;
			return;
		}

		var prevEmaOpen = _prevEmaOpen.Value;
		var haOpen = (_prevHaOpen.Value + _prevHaClose.Value) / 2m;

		var atrGrowthCount = 0;
		if (_prevAtr4.HasValue && atr4 > _prevAtr4.Value)
			atrGrowthCount++;
		if (_prevAtr8.HasValue && atr8 > _prevAtr8.Value)
			atrGrowthCount++;
		if (_prevAtr12.HasValue && atr12 > _prevAtr12.Value)
			atrGrowthCount++;
		if (_prevAtr24.HasValue && atr24 > _prevAtr24.Value)
			atrGrowthCount++;

		var volatilityExpansion = false;
		if (_prevAtr1.HasValue)
		{
			if (atr1 > _prevAtr1.Value)
				volatilityExpansion = true;
			else if (_prevAtr1Prev.HasValue && _prevAtr1.Value > _prevAtr1Prev.Value)
				volatilityExpansion = true;
		}

		var buySignal = _pivotReady && atrGrowthCount > 0 && volatilityExpansion && emaClose > emaOpen && haClose > haOpen && prevEmaOpen <= _pivot && emaOpen > _pivot;
		var sellSignal = _pivotReady && atrGrowthCount > 0 && volatilityExpansion && emaClose < emaOpen && haClose < haOpen && prevEmaOpen >= _pivot && emaOpen < _pivot;

		var exitLongSignal = _pivotReady && atrGrowthCount > 0 && volatilityExpansion && emaClose < emaOpen && haClose < haOpen && prevEmaOpen >= _pivot && emaOpen < _pivot;
		var exitShortSignal = _pivotReady && atrGrowthCount > 0 && volatilityExpansion && emaClose > emaOpen && haClose > haOpen && prevEmaOpen <= _pivot && emaOpen > _pivot;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (buySignal && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				if (volume > 0)
				{
					BuyMarket(volume);
					InitializePositionState(candle.ClosePrice, true);
				}
			}
			else if (sellSignal && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				if (volume > 0)
				{
					SellMarket(volume);
					InitializePositionState(candle.ClosePrice, false);
				}
			}
			else if (exitLongSignal && Position > 0)
			{
				SellMarket(Position);
				ResetPositionState();
			}
			else if (exitShortSignal && Position < 0)
			{
				BuyMarket(-Position);
				ResetPositionState();
			}
		}

		_prevAtr1Prev = _prevAtr1;
		_prevAtr1 = atr1;
		_prevAtr4 = atr4;
		_prevAtr8 = atr8;
		_prevAtr12 = atr12;
		_prevAtr24 = atr24;
		_prevEmaOpen = emaOpen;
		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_takePrice > 0m && candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_stopPrice > 0m && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (UseTrailingStop)
				UpdateLongTrailing(candle.ClosePrice);
		}
		else if (Position < 0)
		{
			if (_takePrice > 0m && candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return;
			}

			if (_stopPrice > 0m && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return;
			}

			if (UseTrailingStop)
				UpdateShortTrailing(candle.ClosePrice);
		}
		else
		{
			ResetPositionState();
		}
	}

	private void InitializePositionState(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;

		if (isLong)
		{
			_stopPrice = stopDistance > 0m ? entryPrice - stopDistance : 0m;
			_takePrice = takeDistance > 0m ? entryPrice + takeDistance : 0m;
		}
		else
		{
			_stopPrice = stopDistance > 0m ? entryPrice + stopDistance : 0m;
			_takePrice = takeDistance > 0m ? entryPrice - takeDistance : 0m;
		}
	}

	private void ResetPositionState()
	{
		if (Position == 0)
		{
			_entryPrice = 0m;
			_stopPrice = 0m;
			_takePrice = 0m;
		}
	}

	private void UpdateLongTrailing(decimal currentPrice)
	{
		switch (TrailingStopType)
		{
			case 1:
			{
				var distance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
				if (distance <= 0m)
					break;

				if (_stopPrice <= 0m)
				{
					_stopPrice = currentPrice - distance;
				}
				else if (currentPrice - _stopPrice > distance)
				{
					var newStop = currentPrice - distance;
					if (newStop > _stopPrice)
						_stopPrice = Math.Min(newStop, currentPrice - _pipSize);
				}

				break;
			}
			case 2:
			{
				var trailing = TrailingStopPips > 0 ? TrailingStopPips * _pipSize : 0m;
				if (trailing <= 0m)
					break;

				if (currentPrice - _entryPrice > trailing)
				{
					var newStop = currentPrice - trailing;
					if (newStop > _stopPrice)
						_stopPrice = Math.Min(newStop, currentPrice - _pipSize);
				}

				break;
			}
			case 3:
			{
				var firstMove = FirstMovePips > 0 ? FirstMovePips * _pipSize : 0m;
				var firstStop = FirstStopLossPips > 0 ? FirstStopLossPips * _pipSize : 0m;
				if (firstMove > 0m && currentPrice - _entryPrice > firstMove)
				{
					var newStop = _entryPrice + firstMove - firstStop;
					if (newStop > _stopPrice)
						_stopPrice = Math.Min(newStop, currentPrice - _pipSize);
				}

				var secondMove = SecondMovePips > 0 ? SecondMovePips * _pipSize : 0m;
				var secondStop = SecondStopLossPips > 0 ? SecondStopLossPips * _pipSize : 0m;
				if (secondMove > 0m && currentPrice - _entryPrice > secondMove)
				{
					var newStop = _entryPrice + secondMove - secondStop;
					if (newStop > _stopPrice)
						_stopPrice = Math.Min(newStop, currentPrice - _pipSize);
				}

				var thirdMove = ThirdMovePips > 0 ? ThirdMovePips * _pipSize : 0m;
				var trailing = TrailingStop3Pips > 0 ? TrailingStop3Pips * _pipSize : 0m;
				if (thirdMove > 0m && trailing > 0m && currentPrice - _entryPrice > thirdMove)
				{
					var newStop = currentPrice - trailing;
					if (newStop > _stopPrice)
						_stopPrice = Math.Min(newStop, currentPrice - _pipSize);
				}

				break;
			}
		}
	}

	private void UpdateShortTrailing(decimal currentPrice)
	{
		switch (TrailingStopType)
		{
			case 1:
			{
				var distance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
				if (distance <= 0m)
					break;

				if (_stopPrice <= 0m)
				{
					_stopPrice = currentPrice + distance;
				}
				else if (_stopPrice - currentPrice > distance)
				{
					var newStop = currentPrice + distance;
					if (newStop < _stopPrice)
						_stopPrice = Math.Max(newStop, currentPrice + _pipSize);
				}

				break;
			}
			case 2:
			{
				var trailing = TrailingStopPips > 0 ? TrailingStopPips * _pipSize : 0m;
				if (trailing <= 0m)
					break;

				if (_entryPrice - currentPrice > trailing)
				{
					var newStop = currentPrice + trailing;
					if (_stopPrice == 0m || newStop < _stopPrice)
						_stopPrice = Math.Max(newStop, currentPrice + _pipSize);
				}

				break;
			}
			case 3:
			{
				var firstMove = FirstMovePips > 0 ? FirstMovePips * _pipSize : 0m;
				var firstStop = FirstStopLossPips > 0 ? FirstStopLossPips * _pipSize : 0m;
				if (firstMove > 0m && _entryPrice - currentPrice > firstMove)
				{
					var newStop = _entryPrice - firstMove + firstStop;
					if (_stopPrice == 0m || newStop < _stopPrice)
						_stopPrice = Math.Max(newStop, currentPrice + _pipSize);
				}

				var secondMove = SecondMovePips > 0 ? SecondMovePips * _pipSize : 0m;
				var secondStop = SecondStopLossPips > 0 ? SecondStopLossPips * _pipSize : 0m;
				if (secondMove > 0m && _entryPrice - currentPrice > secondMove)
				{
					var newStop = _entryPrice - secondMove + secondStop;
					if (_stopPrice == 0m || newStop < _stopPrice)
						_stopPrice = Math.Max(newStop, currentPrice + _pipSize);
				}

				var thirdMove = ThirdMovePips > 0 ? ThirdMovePips * _pipSize : 0m;
				var trailing = TrailingStop3Pips > 0 ? TrailingStop3Pips * _pipSize : 0m;
				if (thirdMove > 0m && trailing > 0m && _entryPrice - currentPrice > thirdMove)
				{
					var newStop = currentPrice + trailing;
					if (_stopPrice == 0m || newStop < _stopPrice)
						_stopPrice = Math.Max(newStop, currentPrice + _pipSize);
				}

				break;
			}
		}
	}
}
