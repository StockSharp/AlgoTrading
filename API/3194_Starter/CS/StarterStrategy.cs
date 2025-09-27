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
/// Port of the MetaTrader strategy Starter.mq5.
/// Aligns three stochastic oscillators across multi-timeframe moving averages.
/// Implements MetaTrader-style money management, stop handling, and trailing logic.
/// </summary>
public class StarterStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<MoneyManagementMode> _moneyMode;
	private readonly StrategyParam<decimal> _moneyValue;
	private readonly StrategyParam<DataType> _fastCandleType;
	private readonly StrategyParam<DataType> _normalCandleType;
	private readonly StrategyParam<DataType> _slowCandleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<AppliedPriceType> _maPriceType;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _stochSlowing;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;

	private LengthIndicator<decimal> _fastMa = null!;
	private LengthIndicator<decimal> _normalMa = null!;
	private LengthIndicator<decimal> _slowMa = null!;
	private StochasticOscillator _fastStochastic = null!;
	private StochasticOscillator _normalStochastic = null!;
	private StochasticOscillator _slowStochastic = null!;

	private readonly List<decimal> _fastMaHistory = new();
	private readonly List<decimal> _normalMaHistory = new();
	private readonly List<decimal> _slowMaHistory = new();

	private decimal? _fastMaShifted;
	private decimal? _normalMaShifted;
	private decimal? _slowMaShifted;

	private decimal? _fastStochMain;
	private decimal? _fastStochSignal;
	private decimal? _normalStochMain;
	private decimal? _normalStochSignal;
	private decimal? _slowStochMain;
	private decimal? _slowStochSignal;

	private decimal _pipSize;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="StarterStrategy"/> class.
	/// </summary>
	public StarterStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 45)
		.SetRange(0, 1000)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips (0 disables)", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 105)
		.SetRange(0, 2000)
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips (0 disables)", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
		.SetRange(0, 500)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop offset measured in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetRange(0, 500)
		.SetDisplay("Trailing Step (pips)", "Minimum advance before trailing stop moves", "Risk");

		_moneyMode = Param(nameof(MoneyMode), MoneyManagementMode.RiskPercent)
		.SetDisplay("Money Mode", "Choose fixed volume or risk based sizing", "Money Management");

		_moneyValue = Param(nameof(MoneyValue), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Money Value", "Lot size or risk percent depending on mode", "Money Management");

		_fastCandleType = Param(nameof(FastCandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
		.SetDisplay("Fast Timeframe", "Timeframe for the fast moving average and stochastic", "Data");

		_normalCandleType = Param(nameof(NormalCandleType), DataType.TimeFrame(TimeSpan.FromMinutes(30)))
		.SetDisplay("Normal Timeframe", "Intermediate timeframe for filters", "Data");

		_slowCandleType = Param(nameof(SlowCandleType), DataType.TimeFrame(TimeSpan.FromHours(2)))
		.SetDisplay("Slow Timeframe", "Slow timeframe that triggers entries", "Data");

		_maPeriod = Param(nameof(MaPeriod), 20)
		.SetRange(1, 500)
		.SetDisplay("MA Period", "Length of the moving averages", "Indicators");

		_maShift = Param(nameof(MaShift), 1)
		.SetRange(0, 20)
		.SetDisplay("MA Shift", "Horizontal shift in completed bars", "Indicators");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Simple)
		.SetDisplay("MA Method", "Moving average smoothing method", "Indicators");

		_maPriceType = Param(nameof(MaPriceType), AppliedPriceType.Close)
		.SetDisplay("MA Price", "Price source for the moving averages", "Indicators");

		_stochKPeriod = Param(nameof(StochasticKPeriod), 5)
		.SetRange(1, 200)
		.SetDisplay("Stoch %K", "%K period for all stochastic oscillators", "Indicators");

		_stochDPeriod = Param(nameof(StochasticDPeriod), 3)
		.SetRange(1, 200)
		.SetDisplay("Stoch %D", "%D period for stochastic smoothing", "Indicators");

		_stochSlowing = Param(nameof(StochasticSlowing), 3)
		.SetRange(1, 200)
		.SetDisplay("Stoch Slowing", "Final smoothing applied to %K", "Indicators");

		_reverseSignals = Param(nameof(ReverseSignals), false)
		.SetDisplay("Reverse Signals", "Invert the long and short rules", "Trading");

		_closeOpposite = Param(nameof(CloseOppositePositions), false)
		.SetDisplay("Close Opposite", "Close the opposite exposure before entering", "Trading");
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
	/// Trailing stop offset expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum move required to shift the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Money management mode replicating the MetaTrader EA behaviour.
	/// </summary>
	public MoneyManagementMode MoneyMode
	{
		get => _moneyMode.Value;
		set => _moneyMode.Value = value;
	}

	/// <summary>
	/// Either fixed volume or risk percent depending on <see cref="MoneyMode"/>.
	/// </summary>
	public decimal MoneyValue
	{
		get => _moneyValue.Value;
		set => _moneyValue.Value = value;
	}

	/// <summary>
	/// Timeframe used for the fast indicator set.
	/// </summary>
	public DataType FastCandleType
	{
		get => _fastCandleType.Value;
		set => _fastCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used for the intermediate indicator set.
	/// </summary>
	public DataType NormalCandleType
	{
		get => _normalCandleType.Value;
		set => _normalCandleType.Value = value;
	}

	/// <summary>
	/// Slow timeframe that orchestrates trade execution.
	/// </summary>
	public DataType SlowCandleType
	{
		get => _slowCandleType.Value;
		set => _slowCandleType.Value = value;
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
	/// Horizontal shift applied to moving average values.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source fed into the moving average.
	/// </summary>
	public AppliedPriceType MaPriceType
	{
		get => _maPriceType.Value;
		set => _maPriceType.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing parameter.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochSlowing.Value;
		set => _stochSlowing.Value = value;
	}

	/// <summary>
	/// Inverts the entry logic when set to <c>true</c>.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Determines whether opposite exposure is closed before a new trade.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_fastMa = CreateMovingAverage(MaMethod, MaPeriod);
		_normalMa = CreateMovingAverage(MaMethod, MaPeriod);
		_slowMa = CreateMovingAverage(MaMethod, MaPeriod);

		_fastStochastic = CreateStochastic();
		_normalStochastic = CreateStochastic();
		_slowStochastic = CreateStochastic();

		var fastSubscription = SubscribeCandles(FastCandleType);
		fastSubscription
		.Bind(ProcessFastCandle)
		.BindEx(_fastStochastic, ProcessFastStochastic)
		.Start();

		var normalSubscription = SubscribeCandles(NormalCandleType);
		normalSubscription
		.Bind(ProcessNormalCandle)
		.BindEx(_normalStochastic, ProcessNormalStochastic)
		.Start();

		var slowSubscription = SubscribeCandles(SlowCandleType);
		slowSubscription
		.Bind(ProcessSlowCandle)
		.BindEx(_slowStochastic, ProcessSlowStochastic)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, slowSubscription);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _slowStochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessFastCandle(ICandleMessage candle)
	{
		ProcessMovingAverage(candle, _fastMa, _fastMaHistory, value => _fastMaShifted = value);
	}

	private void ProcessNormalCandle(ICandleMessage candle)
	{
		ProcessMovingAverage(candle, _normalMa, _normalMaHistory, value => _normalMaShifted = value);
	}

	private void ProcessSlowCandle(ICandleMessage candle)
	{
		ProcessMovingAverage(candle, _slowMa, _slowMaHistory, value => _slowMaShifted = value);

		if (candle.State == CandleStates.Finished && Position != 0)
		{
			ManageTrailing(candle.ClosePrice);
			if (CheckStops(candle))
			return;
		}
	}

	private void ProcessFastStochastic(ICandleMessage candle, IIndicatorValue value)
	{
		UpdateStochastic(value, ref _fastStochMain, ref _fastStochSignal);
	}

	private void ProcessNormalStochastic(ICandleMessage candle, IIndicatorValue value)
	{
		UpdateStochastic(value, ref _normalStochMain, ref _normalStochSignal);
	}

	private void ProcessSlowStochastic(ICandleMessage candle, IIndicatorValue value)
	{
		UpdateStochastic(value, ref _slowStochMain, ref _slowStochSignal);

		if (candle.State != CandleStates.Finished)
		return;

		TryExecuteSignals(candle);
	}

	private void TryExecuteSignals(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_fastMaShifted is not decimal fastMa ||
		_fastStochMain is not decimal fastMain ||
		_fastStochSignal is not decimal fastSignal ||
		_normalMaShifted is not decimal normalMa ||
		_normalStochMain is not decimal normalMain ||
		_normalStochSignal is not decimal normalSignal ||
		_slowMaShifted is not decimal slowMa ||
		_slowStochMain is not decimal slowMain ||
		_slowStochSignal is not decimal slowSignal)
		{
			return;
		}

		if (!_fastMa.IsFormed || !_normalMa.IsFormed || !_slowMa.IsFormed ||
		!_fastStochastic.IsFormed || !_normalStochastic.IsFormed || !_slowStochastic.IsFormed)
		{
			return;
		}

		var price = candle.ClosePrice;

		var buySignal = slowMain > slowSignal && slowMain < 50m && price < slowMa &&
		fastMain > fastSignal && fastMain < 50m && price < fastMa &&
		normalMain > normalSignal && normalMain < 50m && price < normalMa;

		var sellSignal = slowMain < slowSignal && slowMain > 50m && price > slowMa &&
		fastMain < fastSignal && fastMain > 50m && price > fastMa &&
		normalMain < normalSignal && normalMain > 50m && price > normalMa;

		if (ReverseSignals)
		{
			(buySignal, sellSignal) = (sellSignal, buySignal);
		}

		if (buySignal)
		{
			EnterLong(price);
		}
		else if (sellSignal)
		{
			EnterShort(price);
		}
	}

	private void EnterLong(decimal price)
	{
		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		if (Position < 0)
		{
			if (!CloseOppositePositions)
			return;

			volume += Math.Abs(Position);
		}
		else if (Position > 0)
		{
			return;
		}

		BuyMarket(volume);
		ConfigureProtection(price, true);
	}

	private void EnterShort(decimal price)
	{
		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		if (Position > 0)
		{
			if (!CloseOppositePositions)
			return;

			volume += Math.Abs(Position);
		}
		else if (Position < 0)
		{
			return;
		}

		SellMarket(volume);
		ConfigureProtection(price, false);
	}

	private void ConfigureProtection(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;

		_stopPrice = stopDistance > 0m ? (isLong ? entryPrice - stopDistance : entryPrice + stopDistance) : null;
		_takeProfitPrice = takeDistance > 0m ? (isLong ? entryPrice + takeDistance : entryPrice - takeDistance) : null;
	}

	private void ManageTrailing(decimal price)
	{
		if (TrailingStopPips <= 0 || Position == 0 || _entryPrice is null)
		return;

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0)
		{
			var profit = price - _entryPrice.Value;
			if (profit > trailingStop + trailingStep)
			{
				var minTrigger = price - (trailingStop + trailingStep);
				if (_stopPrice is null || _stopPrice.Value < minTrigger)
				_stopPrice = price - trailingStop;
			}
		}
		else if (Position < 0)
		{
			var profit = _entryPrice.Value - price;
			if (profit > trailingStop + trailingStep)
			{
				var minTrigger = price + (trailingStop + trailingStep);
				if (_stopPrice is null || _stopPrice.Value > minTrigger)
				_stopPrice = price + trailingStop;
			}
		}
	}

	private bool CheckStops(ICandleMessage candle)
	{
		if (Position == 0)
		return false;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return false;

		if (_stopPrice is decimal stop)
		{
			if (Position > 0 && candle.LowPrice <= stop)
			{
				SellMarket(volume);
				ResetTradeState();
				return true;
			}

			if (Position < 0 && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				ResetTradeState();
				return true;
			}
		}

		if (_takeProfitPrice is decimal take)
		{
			if (Position > 0 && candle.HighPrice >= take)
			{
				SellMarket(volume);
				ResetTradeState();
				return true;
			}

			if (Position < 0 && candle.LowPrice <= take)
			{
				BuyMarket(volume);
				ResetTradeState();
				return true;
			}
		}

		return false;
	}

	private void ResetTradeState()
	{
		_stopPrice = null;
		_takeProfitPrice = null;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		ResetTradeState();
	}

	private void ProcessMovingAverage(ICandleMessage candle, LengthIndicator<decimal> indicator, List<decimal> history, Action<decimal?> setter)
	{
		var price = GetAppliedPrice(candle, MaPriceType);
		var value = indicator.Process(price, candle.OpenTime, candle.State == CandleStates.Finished);

		if (value == null || !value.IsFinal)
		{
			setter(history.Count > MaShift ? history[^ (1 + MaShift)] : null);
			return;
		}

		var maDecimal = value.ToDecimal();
		UpdateMaHistory(history, maDecimal);
		setter(history.Count > MaShift ? history[^ (1 + MaShift)] : null);
	}

	private void UpdateStochastic(IIndicatorValue value, ref decimal? main, ref decimal? signal)
	{
		if (!value.IsFinal)
		return;

		var stoch = (StochasticOscillatorValue)value;
		main = stoch.K;
		signal = stoch.D;
	}

	private void UpdateMaHistory(List<decimal> history, decimal value)
	{
		history.Add(value);
		var limit = Math.Max(MaPeriod + MaShift + 5, MaPeriod * 2);
		if (history.Count > limit)
		history.RemoveRange(0, history.Count - limit);
	}

	private decimal CalculateVolume()
	{
		return MoneyMode switch
		{
			MoneyManagementMode.FixedLot => MoneyValue,
			MoneyManagementMode.RiskPercent => CalculateRiskVolume(),
			_ => MoneyValue
		};
	}

	private decimal CalculateRiskVolume()
	{
		if (StopLossPips <= 0 || _pipSize <= 0m)
		return 0m;

		var portfolio = Portfolio;
		var portfolioValue = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		if (portfolioValue <= 0m)
		return 0m;

		var riskAmount = portfolioValue * (MoneyValue / 100m);
		var stopDistance = StopLossPips * _pipSize;
		if (stopDistance <= 0m)
		return 0m;

		return riskAmount / stopDistance;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? 0;

		if (decimals == 3 || decimals == 5)
		return step * 10m;

		return step;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType priceType)
	{
		return priceType switch
		{
			AppliedPriceType.Close => candle.ClosePrice,
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
			_ => candle.ClosePrice
		};
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private StochasticOscillator CreateStochastic()
	{
		return new StochasticOscillator
		{
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod,
			Slowing = StochasticSlowing
		};
	}

	/// <summary>
	/// Money management modes supported by the strategy.
	/// </summary>
	public enum MoneyManagementMode
	{
		FixedLot,
		RiskPercent,
	}

	/// <summary>
	/// Moving average smoothing modes matching the MetaTrader enumerations.
	/// </summary>
	public enum MovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
	}

	/// <summary>
	/// Price types compatible with the MetaTrader applied price options.
	/// </summary>
	public enum AppliedPriceType
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
	}
}

