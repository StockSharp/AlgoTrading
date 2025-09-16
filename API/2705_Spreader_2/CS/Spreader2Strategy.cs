using System;
using System.Collections.Generic;

using Ecng.ComponentModel;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pair trading strategy inspired by the "Spreader 2" MetaTrader expert.
/// Looks for short term mean-reverting moves between two correlated symbols
/// and trades the spread once correlation and volatility filters align.
/// </summary>
public class Spreader2Strategy : Strategy
{
	private const int DayBars = 1440;

	private readonly StrategyParam<Security> _secondSecurityParam;
	private readonly StrategyParam<decimal> _primaryVolumeParam;
	private readonly StrategyParam<decimal> _targetProfitParam;
	private readonly StrategyParam<int> _shiftParam;
	private readonly StrategyParam<DataType> _candleTypeParam;

	private readonly Queue<ICandleMessage> _firstPending = new();
	private readonly Queue<ICandleMessage> _secondPending = new();
	private readonly List<decimal> _firstCloses = new();
	private readonly List<decimal> _secondCloses = new();

	private decimal _lastFirstClose;
	private decimal _lastSecondClose;

	private decimal _firstEntryPrice;
	private decimal _secondEntryPrice;
	private decimal _secondPosition;

	private Portfolio _secondPortfolio;
	private bool _contractsMatch = true;

	/// <summary>
	/// Secondary security involved in the spread.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurityParam.Value;
		set => _secondSecurityParam.Value = value;
	}

	/// <summary>
	/// Trading volume for the primary security.
	/// </summary>
	public decimal PrimaryVolume
	{
		get => _primaryVolumeParam.Value;
		set => _primaryVolumeParam.Value = value;
	}

	/// <summary>
	/// Target profit (absolute money) for the combined position.
	/// </summary>
	public decimal TargetProfit
	{
		get => _targetProfitParam.Value;
		set => _targetProfitParam.Value = value;
	}

	/// <summary>
	/// Number of bars between comparison points.
	/// </summary>
	public int ShiftLength
	{
		get => _shiftParam.Value;
		set => _shiftParam.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Spreader2Strategy"/> class.
	/// </summary>
	public Spreader2Strategy()
	{
		_secondSecurityParam = Param<Security>(nameof(SecondSecurity))
			.SetDisplay("Second Symbol", "Secondary instrument for the spread trade", "General")
			.SetRequired();

		_primaryVolumeParam = Param(nameof(PrimaryVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Primary Volume", "Order volume for the primary symbol", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_targetProfitParam = Param(nameof(TargetProfit), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Target Profit", "Total profit target for the pair position", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 20m);

		_shiftParam = Param(nameof(ShiftLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Shift Length", "Number of bars between comparison points", "Logic")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 10);

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for pair analysis", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (SecondSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_firstPending.Clear();
		_secondPending.Clear();
		_firstCloses.Clear();
		_secondCloses.Clear();

		_lastFirstClose = 0m;
		_lastSecondClose = 0m;

		_firstEntryPrice = 0m;
		_secondEntryPrice = 0m;
		_secondPosition = 0m;

		_secondPortfolio = null;
		_contractsMatch = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SecondSecurity == null)
			throw new InvalidOperationException("Second security is not specified.");

		_secondPortfolio = Portfolio ?? throw new InvalidOperationException("Portfolio is not specified.");

		if (Security?.Multiplier != null && SecondSecurity?.Multiplier != null && Security.Multiplier != SecondSecurity.Multiplier)
		{
			LogWarning($"Contract size mismatch between {Security?.Code} and {SecondSecurity?.Code}. Trading disabled.");
			_contractsMatch = false;
		}

		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		var secondarySubscription = SubscribeCandles(CandleType, security: SecondSecurity);
		secondarySubscription
			.Bind(ProcessSecondaryCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastFirstClose = candle.ClosePrice;
		_firstPending.Enqueue(candle);

		ProcessPendingCandles();
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastSecondClose = candle.ClosePrice;
		_secondPending.Enqueue(candle);

		ProcessPendingCandles();
	}

	private void ProcessPendingCandles()
	{
		while (_firstPending.Count > 0 && _secondPending.Count > 0)
		{
			var first = _firstPending.Peek();
			var second = _secondPending.Peek();

			if (first.CloseTime < second.CloseTime)
			{
				_firstPending.Dequeue();
				continue;
			}

			if (second.CloseTime < first.CloseTime)
			{
				_secondPending.Dequeue();
				continue;
			}

			_firstPending.Dequeue();
			_secondPending.Dequeue();

			HandlePairedCandles(first, second);
		}
	}

	private void HandlePairedCandles(ICandleMessage firstCandle, ICandleMessage secondCandle)
	{
		var maxHistory = Math.Max(DayBars, ShiftLength * 2) + 10;
		AppendHistory(_firstCloses, firstCandle.ClosePrice, maxHistory);
		AppendHistory(_secondCloses, secondCandle.ClosePrice, maxHistory);

		if (!UpdateProfitCheck(firstCandle.ClosePrice, secondCandle.ClosePrice))
			return;

		if (!_contractsMatch)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (PrimaryVolume <= 0m)
			return;

		if (_firstCloses.Count <= ShiftLength * 2 || _secondCloses.Count <= ShiftLength * 2)
			return;

		if (_firstCloses.Count <= DayBars || _secondCloses.Count <= DayBars)
			return;

		var currentIndex = _firstCloses.Count - 1;
		var secondIndex = _secondCloses.Count - 1;
		var shift = ShiftLength;
		var shiftIndex = currentIndex - shift;
		var shiftIndex2 = currentIndex - (shift * 2);
		var dayIndex = currentIndex - DayBars;
		var secondShiftIndex = secondIndex - shift;
		var secondShiftIndex2 = secondIndex - (shift * 2);
		var secondDayIndex = secondIndex - DayBars;

		if (shiftIndex < 0 || shiftIndex2 < 0 || dayIndex < 0)
			return;

		if (secondShiftIndex < 0 || secondShiftIndex2 < 0 || secondDayIndex < 0)
			return;

		var closeCur0 = _firstCloses[currentIndex];
		var closeCurShift = _firstCloses[shiftIndex];
		var closeCurShift2 = _firstCloses[shiftIndex2];
		var closeCurDay = _firstCloses[dayIndex];

		var closeSec0 = _secondCloses[secondIndex];
		var closeSecShift = _secondCloses[secondShiftIndex];
		var closeSecShift2 = _secondCloses[secondShiftIndex2];
		var closeSecDay = _secondCloses[secondDayIndex];

		var x1 = closeCur0 - closeCurShift;
		var x2 = closeCurShift - closeCurShift2;
		var y1 = closeSec0 - closeSecShift;
		var y2 = closeSecShift - closeSecShift2;

		if ((x1 * x2) > 0m)
		{
			LogInfo($"Trend detected on {Security?.Code}, skipping correlation check.");
			return;
		}

		if ((y1 * y2) > 0m)
		{
			LogInfo($"Trend detected on {SecondSecurity?.Code}, skipping correlation check.");
			return;
		}

		if ((x1 * y1) <= 0m)
		{
			LogInfo("Negative correlation detected. Waiting for better alignment.");
			return;
		}

		var a = Math.Abs(x1) + Math.Abs(x2);
		var b = Math.Abs(y1) + Math.Abs(y2);

		if (b == 0m)
			return;

		var ratio = a / b;

		if (ratio > 3m)
			return;

		if (ratio < 0.3m)
			return;

		var secondVolume = AdjustSecondaryVolume(ratio * PrimaryVolume);

		if (secondVolume <= 0m)
		{
			LogInfo("Secondary volume too small after adjustment. Skipping trade.");
			return;
		}

		var x3 = closeCur0 - closeCurDay;
		var y3 = closeSec0 - closeSecDay;

		var primarySide = x1 * b > y1 * a ? Sides.Buy : Sides.Sell;
		var secondarySide = primarySide == Sides.Buy ? Sides.Sell : Sides.Buy;

		if (primarySide == Sides.Buy && (x3 * b) < (y3 * a))
		{
			LogInfo("Buy signal rejected by daily confirmation check.");
			return;
		}

		if (primarySide == Sides.Sell && (x3 * b) > (y3 * a))
		{
			LogInfo("Sell signal rejected by daily confirmation check.");
			return;
		}

		OpenPair(primarySide, secondarySide, secondVolume);
	}

	private bool UpdateProfitCheck(decimal firstClose, decimal secondClose)
	{
		var primaryPosition = Position;
		var hasSecondary = _secondPosition != 0m;

		if (primaryPosition == 0m && !hasSecondary)
			return true;

		if (primaryPosition != 0m && !hasSecondary)
		{
			LogInfo("Secondary position missing. Closing primary exposure.");
			ClosePrimaryPosition();
			return false;
		}

		if (primaryPosition == 0m && hasSecondary)
		{
			if (!IsFormedAndOnlineAndAllowTrading())
				return false;

			var requiredSide = _secondPosition > 0m ? Sides.Sell : Sides.Buy;
			LogInfo("Primary position missing. Opening trade to balance spread.");
			OpenPrimary(requiredSide, PrimaryVolume);
			return false;
		}

		if (_firstEntryPrice == 0m || _secondEntryPrice == 0m)
			return false;

		var primaryVolume = Math.Abs(primaryPosition);
		var secondaryVolume = Math.Abs(_secondPosition);

		var primaryProfit = primaryPosition > 0m
			? (firstClose - _firstEntryPrice) * primaryVolume
			: (_firstEntryPrice - firstClose) * primaryVolume;

		var secondaryProfit = _secondPosition > 0m
			? (secondClose - _secondEntryPrice) * secondaryVolume
			: (_secondEntryPrice - secondClose) * secondaryVolume;

		var totalProfit = primaryProfit + secondaryProfit;

		if (totalProfit >= TargetProfit)
		{
			LogInfo($"Target profit reached ({totalProfit:F2}). Closing both legs.");
			ClosePair();
		}

		return false;
	}

	private void OpenPair(Sides primarySide, Sides secondarySide, decimal secondaryVolume)
	{
		OpenSecondary(secondarySide, secondaryVolume);
		OpenPrimary(primarySide, PrimaryVolume);

		LogInfo($"Opened spread: {primarySide} {PrimaryVolume} {Security?.Code}, {secondarySide} {secondaryVolume} {SecondSecurity?.Code}.");
	}

	private void OpenPrimary(Sides side, decimal volume)
	{
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_firstEntryPrice = _lastFirstClose;
	}

	private void OpenSecondary(Sides side, decimal volume)
	{
		if (volume <= 0m || SecondSecurity == null || _secondPortfolio == null)
			return;

		var order = CreateOrder(side, _lastSecondClose, volume);
		order.Type = OrderTypes.Market;
		order.Security = SecondSecurity;
		order.Portfolio = _secondPortfolio;

		RegisterOrder(order);

		_secondPosition = side == Sides.Buy ? volume : -volume;
		_secondEntryPrice = _lastSecondClose;
	}

	private void ClosePair()
	{
		ClosePrimaryPosition();
		CloseSecondaryPosition();
	}

	private void ClosePrimaryPosition()
	{
		var primaryPosition = Position;

		if (primaryPosition > 0m)
			SellMarket(primaryPosition);
		else if (primaryPosition < 0m)
			BuyMarket(Math.Abs(primaryPosition));

		_firstEntryPrice = 0m;
	}

	private void CloseSecondaryPosition()
	{
		if (_secondPosition == 0m || SecondSecurity == null || _secondPortfolio == null)
			return;

		var side = _secondPosition > 0m ? Sides.Sell : Sides.Buy;
		var volume = Math.Abs(_secondPosition);

		var order = CreateOrder(side, _lastSecondClose, volume);
		order.Type = OrderTypes.Market;
		order.Security = SecondSecurity;
		order.Portfolio = _secondPortfolio;

		RegisterOrder(order);

		_secondPosition = 0m;
		_secondEntryPrice = 0m;
	}

	private decimal AdjustSecondaryVolume(decimal requestedVolume)
	{
		if (SecondSecurity == null)
			return 0m;

		var volume = Math.Abs(requestedVolume);
		var step = SecondSecurity.VolumeStep ?? 0m;

		if (step > 0m)
			volume = decimal.Floor(volume / step) * step;

		var min = SecondSecurity.MinVolume ?? 0m;
		if (min > 0m && volume < min)
			return 0m;

		var max = SecondSecurity.MaxVolume;
		if (max != null && volume > max.Value)
			volume = max.Value;

		return volume;
	}

	private static void AppendHistory(List<decimal> storage, decimal value, int maxHistory)
	{
		storage.Add(value);

		if (storage.Count > maxHistory)
			storage.RemoveAt(0);
	}
}
