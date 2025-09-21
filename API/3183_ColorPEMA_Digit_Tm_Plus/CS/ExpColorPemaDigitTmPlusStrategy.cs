namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class ExpColorPemaDigitTmPlusStrategy : Strategy
{
	private readonly StrategyParam<decimal> _moneyManagement;
	private readonly StrategyParam<MoneyManagementMode> _moneyMode;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _deviationPoints;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<bool> _useTimeExit;
	private readonly StrategyParam<int> _holdingMinutes;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _emaLength;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<int> _digitPrecision;
	private readonly StrategyParam<int> _signalBar;

	private ExponentialMovingAverage[]? _emaStages;
	private readonly List<decimal> _pemaValues = new();
	private readonly List<TrendState> _trendStates = new();

	private bool _pendingLongEntry;
	private bool _pendingShortEntry;
	private bool _pendingLongExit;
	private bool _pendingShortExit;
	private DateTimeOffset? _longSignalTime;
	private DateTimeOffset? _shortSignalTime;

	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private TimeSpan? _timeFrame;

	public ExpColorPemaDigitTmPlusStrategy()
	{
		_moneyManagement = Param(nameof(MoneyManagement), 0.1m)
			.SetDisplay("Money Management", "Base value used for position sizing.", "Trading")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_moneyMode = Param(nameof(MoneyMode), MoneyManagementMode.Lot)
			.SetDisplay("Money Mode", "Position sizing model replicated from the MetaTrader expert.", "Trading")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss (points)", "Distance between entry price and stop loss expressed in price points.", "Risk")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit (points)", "Distance between entry price and take profit expressed in price points.", "Risk")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true);

		_deviationPoints = Param(nameof(DeviationPoints), 10)
			.SetDisplay("Allowed Deviation", "Maximum price deviation tolerated by the MetaTrader order logic.", "Risk")
			.SetGreaterOrEqualZero();

		_allowBuyOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Allow Long Entries", "Enable opening long positions when the indicator turns bullish.", "Trading");

		_allowSellOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Allow Short Entries", "Enable opening short positions when the indicator turns bearish.", "Trading");

		_allowBuyClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Allow Long Exits", "Close long positions when the indicator flips to bearish.", "Trading");

		_allowSellClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Allow Short Exits", "Close short positions when the indicator flips to bullish.", "Trading");

		_useTimeExit = Param(nameof(TimeTrade), true)
			.SetDisplay("Use Time Exit", "Enable the time based exit originally present in the MetaTrader expert.", "Risk");

		_holdingMinutes = Param(nameof(HoldingMinutes), 960)
			.SetDisplay("Holding Minutes", "Maximum lifetime of an open position in minutes.", "Risk")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle series processed by the strategy.", "General");

		_emaLength = Param(nameof(EmaLength), 50.01m)
			.SetDisplay("PEMA Length", "Base length used for each exponential average in the Pentuple EMA stack.", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_appliedPrice = Param(nameof(PriceMode), AppliedPrice.Close)
			.SetDisplay("Applied Price", "Price source used to feed the Pentuple EMA calculation.", "Indicator");

		_digitPrecision = Param(nameof(DigitPrecision), 2)
			.SetDisplay("Rounding Digits", "Number of decimal digits used to round the Pentuple EMA output.", "Indicator")
			.SetGreaterOrEqualZero();

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Number of completed candles to wait before reacting to an indicator color change.", "Indicator")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true);
	}

	public decimal MoneyManagement
	{
		get => _moneyManagement.Value;
		set => _moneyManagement.Value = value;
	}

	public MoneyManagementMode MoneyMode
	{
		get => _moneyMode.Value;
		set => _moneyMode.Value = value;
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

	public int DeviationPoints
	{
		get => _deviationPoints.Value;
		set => _deviationPoints.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	public bool TimeTrade
	{
		get => _useTimeExit.Value;
		set => _useTimeExit.Value = value;
	}

	public int HoldingMinutes
	{
		get => _holdingMinutes.Value;
		set => _holdingMinutes.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public AppliedPrice PriceMode
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	public int DigitPrecision
	{
		get => _digitPrecision.Value;
		set => _digitPrecision.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var length = Math.Max(1, (int)Math.Round(EmaLength));
		_emaStages = new ExponentialMovingAverage[8];
		for (var i = 0; i < _emaStages.Length; i++)
		{
			_emaStages[i] = new ExponentialMovingAverage
			{
				Length = length
			};
		}

		_pemaValues.Clear();
		_trendStates.Clear();

		_pendingLongEntry = false;
		_pendingShortEntry = false;
		_pendingLongExit = false;
		_pendingShortExit = false;
		_longSignalTime = null;
		_shortSignalTime = null;

		_longEntryTime = null;
		_shortEntryTime = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		_timeFrame = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_emaStages.Length > 0)
			{
				foreach (var ema in _emaStages)
				{
					DrawIndicator(area, ema);
				}
			}
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_timeFrame ??= candle.CloseTime - candle.OpenTime;

		if (_emaStages == null)
			return;

		var price = GetAppliedPrice(candle);
		if (price is null)
			return;

		var stageValues = new decimal[_emaStages.Length];
		var stageInput = price.Value;
		for (var i = 0; i < _emaStages.Length; i++)
		{
			var ema = _emaStages[i];
			var value = ema.Process(stageInput, candle.CloseTime, true);
			if (!ema.IsFormed)
				return;

			stageInput = value.ToDecimal();
			stageValues[i] = stageInput;
		}

		var pema = 8m * stageValues[0]
			- 28m * stageValues[1]
			+ 56m * stageValues[2]
			- 70m * stageValues[3]
			+ 56m * stageValues[4]
			- 28m * stageValues[5]
			+ 8m * stageValues[6]
			- stageValues[7];

		var rounded = RoundToDigits(pema);
		_pemaValues.Add(rounded);

		var maxHistory = Math.Max(SignalBar + 5, 20);
		TrimHistory(_pemaValues, maxHistory);

		var trend = TrendState.Flat;
		if (_pemaValues.Count > 1)
		{
			var previous = _pemaValues[^2];
			if (rounded > previous)
				trend = TrendState.Up;
			else if (rounded < previous)
				trend = TrendState.Down;
			else if (_trendStates.Count > 0)
				trend = _trendStates[^1];
		}

		_trendStates.Add(trend);
		TrimHistory(_trendStates, maxHistory);

		var shift = Math.Max(0, SignalBar);
		var currentIndex = _trendStates.Count - 1 - shift;
		if (currentIndex <= 0)
		{
			ExecuteRiskManagement(candle);
			return;
		}

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
		{
			ExecuteRiskManagement(candle);
			return;
		}

		var currentState = _trendStates[currentIndex];
		var previousState = _trendStates[previousIndex];

		if (currentState == TrendState.Up && previousState != TrendState.Up)
		{
			if (BuyPosOpen)
			{
				_pendingLongEntry = true;
				_longSignalTime = candle.CloseTime;
			}

			if (SellPosClose)
			{
				_pendingShortExit = true;
			}
		}
		else if (currentState == TrendState.Down && previousState != TrendState.Down)
		{
			if (SellPosOpen)
			{
				_pendingShortEntry = true;
				_shortSignalTime = candle.CloseTime;
			}

			if (BuyPosClose)
			{
				_pendingLongExit = true;
			}
		}

		ExecuteRiskManagement(candle);
	}

	private void ExecuteRiskManagement(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var now = candle.CloseTime;
		var closePrice = candle.ClosePrice;

		if (TimeTrade && HoldingMinutes > 0)
		{
			var lifetime = TimeSpan.FromMinutes(HoldingMinutes);
			if (Position > 0m && _longEntryTime.HasValue && now - _longEntryTime.Value >= lifetime)
			{
				SellMarket(Position);
				_pendingLongEntry = false;
			}

			if (Position < 0m && _shortEntryTime.HasValue && now - _shortEntryTime.Value >= lifetime)
			{
				BuyMarket(Math.Abs(Position));
				_pendingShortEntry = false;
			}
		}

		if (Position > 0m)
		{
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Position);
				_pendingLongEntry = false;
			}

			if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(Position);
				_pendingLongEntry = false;
			}
		}
		else if (Position < 0m)
		{
			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				_pendingShortEntry = false;
			}

			if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				_pendingShortEntry = false;
			}
		}

		if (_pendingLongExit && BuyPosClose && Position > 0m)
		{
			SellMarket(Position);
			_pendingLongExit = false;
		}

		if (_pendingShortExit && SellPosClose && Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			_pendingShortExit = false;
		}

		if (_pendingLongEntry && BuyPosOpen && Position == 0m)
		{
			var activation = _longSignalTime ?? DateTimeOffset.MinValue;
			if (now >= activation)
			{
				var volume = GetEntryVolume(true, closePrice);
				if (volume > 0m)
				{
					BuyMarket(volume);
					_pendingLongEntry = false;
				}
			}
		}

		if (_pendingShortEntry && SellPosOpen && Position == 0m)
		{
			var activation = _shortSignalTime ?? DateTimeOffset.MinValue;
			if (now >= activation)
			{
				var volume = GetEntryVolume(false, closePrice);
				if (volume > 0m)
				{
					SellMarket(volume);
					_pendingShortEntry = false;
				}
			}
		}
	}

	private decimal? GetAppliedPrice(ICandleMessage candle)
	{
		return PriceMode switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			AppliedPrice.Simplified => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			AppliedPrice.TrendFollow0 => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow1 => (2m * candle.ClosePrice + candle.OpenPrice + candle.HighPrice + candle.LowPrice) / 5m,
			AppliedPrice.Demark =>
				candle.OpenPrice <= candle.ClosePrice
					? (2m * candle.LowPrice + candle.HighPrice + candle.ClosePrice) / 4m
					: (2m * candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private decimal RoundToDigits(decimal value)
	{
		var digits = Math.Max(0, DigitPrecision);
		return Math.Round(value, digits, MidpointRounding.AwayFromZero);
	}

	private static void TrimHistory<T>(IList<T> list, int max)
	{
		while (list.Count > max)
		{
			list.RemoveAt(0);
		}
	}

	private decimal GetEntryVolume(bool isLong, decimal price)
	{
		if (price <= 0m)
			return 0m;

		var step = GetPriceStep();
		var stopDistance = StopLossPoints > 0m ? StopLossPoints * step : 0m;
		var capital = Portfolio?.CurrentValue ?? 0m;
		var mmValue = MoneyManagement;

		switch (MoneyMode)
		{
			case MoneyManagementMode.Lot:
				return mmValue;
			case MoneyManagementMode.Balance:
			case MoneyManagementMode.FreeMargin:
				return capital > 0m ? capital * mmValue / price : 0m;
			case MoneyManagementMode.LossBalance:
			case MoneyManagementMode.LossFreeMargin:
				if (stopDistance > 0m)
					return capital > 0m ? capital * mmValue / stopDistance : 0m;

				return capital > 0m ? capital * mmValue / price : 0m;
			default:
				return mmValue;
		}
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		if (security.PriceStep > 0m)
			return security.PriceStep;

		if (security.MinStep > 0m)
			return security.MinStep;

		return 0.0001m;
	}

	private decimal? CalculateStopPrice(bool isLong, decimal? entryPrice)
	{
		if (!entryPrice.HasValue || StopLossPoints <= 0m)
			return null;

		var distance = StopLossPoints * GetPriceStep();
		return isLong ? entryPrice - distance : entryPrice + distance;
	}

	private decimal? CalculateTakePrice(bool isLong, decimal? entryPrice)
	{
		if (!entryPrice.HasValue || TakeProfitPoints <= 0m)
			return null;

		var distance = TakeProfitPoints * GetPriceStep();
		return isLong ? entryPrice + distance : entryPrice - distance;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position > 0m && trade.OrderDirection == Sides.Buy)
		{
			_longEntryTime = trade.ServerTime;
			_longEntryPrice = trade.Price;
			_longStopPrice = CalculateStopPrice(true, _longEntryPrice);
			_longTakePrice = CalculateTakePrice(true, _longEntryPrice);
		}
		else if (Position < 0m && trade.OrderDirection == Sides.Sell)
		{
			_shortEntryTime = trade.ServerTime;
			_shortEntryPrice = trade.Price;
			_shortStopPrice = CalculateStopPrice(false, _shortEntryPrice);
			_shortTakePrice = CalculateTakePrice(false, _shortEntryPrice);
		}

		if (Position == 0m)
		{
			_longEntryTime = null;
			_shortEntryTime = null;
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_longStopPrice = null;
			_shortStopPrice = null;
			_longTakePrice = null;
			_shortTakePrice = null;
		}
	}

	public enum MoneyManagementMode
	{
		FreeMargin,
		Balance,
		LossFreeMargin,
		LossBalance,
		Lot
	}

	public enum AppliedPrice
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simplified,
		Quarter,
		TrendFollow0,
		TrendFollow1,
		Demark
	}

	private enum TrendState
	{
		Down,
		Flat,
		Up
	}
}
