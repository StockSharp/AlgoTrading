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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// StockSharp high level conversion of the MetaTrader expert advisor "RRS Impulse".
/// The strategy combines RSI, Stochastic and Bollinger Bands filters with multiple signal strength modes.
/// </summary>
public class RrsImpulseStrategy : Strategy
{
	private static readonly DataType Minute1 = TimeSpan.FromMinutes(1).TimeFrame();
	private static readonly DataType Minute5 = TimeSpan.FromMinutes(5).TimeFrame();
	private static readonly DataType Minute15 = TimeSpan.FromMinutes(15).TimeFrame();
	private static readonly DataType Minute30 = TimeSpan.FromMinutes(30).TimeFrame();
	private static readonly DataType Hour1 = TimeSpan.FromHours(1).TimeFrame();
	private static readonly DataType Hour4 = TimeSpan.FromHours(4).TimeFrame();

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStartPips;
	private readonly StrategyParam<int> _trailingGapPips;
	private readonly StrategyParam<RrsIndicatorMode> _indicatorMode;
	private readonly StrategyParam<TradeDirectionMode> _tradeDirection;
	private readonly StrategyParam<SignalStrengthMode> _signalStrength;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticUpperLevel;
	private readonly StrategyParam<decimal> _stochasticLowerLevel;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;

	private readonly Dictionary<DataType, TimeFrameState> _timeFrames = new(DataTypeComparer.Instance);

	private decimal? _longTrailingPrice;
	private decimal? _shortTrailingPrice;

	/// <summary>
	/// Available indicator combinations from the original robot.
	/// </summary>
	public enum RrsIndicatorMode
	{
		Rsi,
		Stochastic,
		BollingerBands,
		RsiStochasticBollinger
	}

	/// <summary>
	/// Trade with the indicator direction or counter to it.
	/// </summary>
	public enum TradeDirectionMode
	{
		Trend,
		CounterTrend
	}

	/// <summary>
	/// Strength filter that requires additional timeframes to align.
	/// </summary>
	public enum SignalStrengthMode
	{
		SingleTimeFrame,
		MultiTimeFrame,
		Strong,
		VeryStrong
	}

	private enum SignalDirection
	{
		None,
		Up,
		Down
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RrsImpulseStrategy"/> class.
	/// </summary>
	public RrsImpulseStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Base Candle", "Primary timeframe used for execution", "Data");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Volume", "Order volume for entries", "Orders")
		.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 200)
		.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk")
		.SetRange(0, 5000)
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
		.SetDisplay("Take Profit", "Take profit distance in pips", "Risk")
		.SetRange(0, 5000)
		.SetCanOptimize(true);

		_trailingStartPips = Param(nameof(TrailingStartPips), 50)
		.SetDisplay("Trailing Start", "Profit in pips required to arm trailing", "Risk")
		.SetRange(0, 5000);

		_trailingGapPips = Param(nameof(TrailingGapPips), 20)
		.SetDisplay("Trailing Gap", "Distance in pips between price and trailing stop", "Risk")
		.SetRange(0, 5000);

		_indicatorMode = Param(nameof(IndicatorMode), RrsIndicatorMode.Rsi)
		.SetDisplay("Indicator Mode", "Select indicator combination", "Signals");

		_tradeDirection = Param(nameof(TradeDirection), TradeDirectionMode.CounterTrend)
		.SetDisplay("Trade Direction", "Follow or fade indicator direction", "Signals");

		_signalStrength = Param(nameof(SignalStrength), SignalStrengthMode.SingleTimeFrame)
		.SetDisplay("Signal Strength", "Required timeframe confirmation", "Signals");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "Number of bars for RSI", "RSI")
		.SetGreaterThanZero();

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 80m)
		.SetDisplay("RSI Upper", "Overbought threshold", "RSI")
		.SetRange(0m, 100m);

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 20m)
		.SetDisplay("RSI Lower", "Oversold threshold", "RSI")
		.SetRange(0m, 100m);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 10)
		.SetDisplay("Stochastic %K", "%K period", "Stochastic")
		.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
		.SetDisplay("Stochastic %D", "%D period", "Stochastic")
		.SetGreaterThanZero();

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
		.SetDisplay("Stochastic Slowing", "Slowing applied to %K", "Stochastic")
		.SetGreaterThanZero();

		_stochasticUpperLevel = Param(nameof(StochasticUpperLevel), 80m)
		.SetDisplay("Stochastic Upper", "Overbought threshold", "Stochastic")
		.SetRange(0m, 100m);

		_stochasticLowerLevel = Param(nameof(StochasticLowerLevel), 20m)
		.SetDisplay("Stochastic Lower", "Oversold threshold", "Stochastic")
		.SetRange(0m, 100m);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetDisplay("Bollinger Period", "Length for Bollinger Bands", "Bollinger")
		.SetGreaterThanZero();

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
		.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Bollinger")
		.SetGreaterThanZero();
	}

	/// <summary>
	/// Primary candle type used for trading decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume issued when entering new positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
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
	/// Profit distance required to start trailing the stop loss.
	/// </summary>
	public int TrailingStartPips
	{
		get => _trailingStartPips.Value;
		set => _trailingStartPips.Value = value;
	}

	/// <summary>
	/// Gap maintained between the current price and the trailing stop.
	/// </summary>
	public int TrailingGapPips
	{
		get => _trailingGapPips.Value;
		set => _trailingGapPips.Value = value;
	}

	/// <summary>
	/// Indicator combination that produces trading signals.
	/// </summary>
	public RrsIndicatorMode IndicatorMode
	{
		get => _indicatorMode.Value;
		set => _indicatorMode.Value = value;
	}

	/// <summary>
	/// Directional mode: follow or fade indicator suggestions.
	/// </summary>
	public TradeDirectionMode TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// How many timeframes must agree before a trade is executed.
	/// </summary>
	public SignalStrengthMode SignalStrength
	{
		get => _signalStrength.Value;
		set => _signalStrength.Value = value;
	}

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Overbought RSI level.
	/// </summary>
	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	/// <summary>
	/// Oversold RSI level.
	/// </summary>
	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	/// <summary>
	/// %K period for the stochastic oscillator.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// %D smoothing period for the stochastic oscillator.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Additional slowing applied to the %K line.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Overbought threshold for the stochastic oscillator.
	/// </summary>
	public decimal StochasticUpperLevel
	{
		get => _stochasticUpperLevel.Value;
		set => _stochasticUpperLevel.Value = value;
	}

	/// <summary>
	/// Oversold threshold for the stochastic oscillator.
	/// </summary>
	public decimal StochasticLowerLevel
	{
		get => _stochasticLowerLevel.Value;
		set => _stochasticLowerLevel.Value = value;
	}

	/// <summary>
	/// Bollinger Bands look-back length.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

		var types = GetProcessingTypes();
		foreach (var type in types)
		yield return (Security, type);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_timeFrames.Clear();
		_longTrailingPrice = null;
		_shortTrailingPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = NormalizeVolume(TradeVolume);

		InitializeTimeframes();

		var baseSubscription = SubscribeCandles(CandleType);
		ConfigureSubscription(CandleType, baseSubscription);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawOwnTrades(area);
		}
	}

	private void InitializeTimeframes()
	{
		_timeFrames.Clear();

		foreach (var type in GetProcessingTypes())
		{
			var state = new TimeFrameState
			{
				Rsi = new RelativeStrengthIndex { Length = Math.Max(1, RsiPeriod) },
				Stochastic = new StochasticOscillator
				{
					Length = Math.Max(1, StochasticKPeriod),
					K = { Length = Math.Max(1, StochasticSlowing) },
					D = { Length = Math.Max(1, StochasticDPeriod) }
				},
				Bollinger = new BollingerBands
				{
					Length = Math.Max(1, BollingerPeriod),
					Width = BollingerDeviation
				}
			};

			_timeFrames[type] = state;

			if (type == CandleType)
			continue;

			var subscription = SubscribeCandles(type);
			ConfigureSubscription(type, subscription);
		}
	}

	private IReadOnlyCollection<DataType> GetProcessingTypes()
	{
		var set = new HashSet<DataType>(DataTypeComparer.Instance) { CandleType };

		foreach (var type in GetConfirmationTypes())
		set.Add(type);

		return set;
	}

	private IReadOnlyCollection<DataType> GetConfirmationTypes()
	{
		return SignalStrength switch
		{
			SignalStrengthMode.SingleTimeFrame => new[] { CandleType },
			SignalStrengthMode.MultiTimeFrame => new[] { Minute1, Minute5, Minute15, Minute30, Hour1, Hour4 },
			SignalStrengthMode.Strong => new[] { Minute1, Minute5, Minute15, Minute30 },
			SignalStrengthMode.VeryStrong => new[] { Minute1, Minute5, Minute15, Minute30, Hour1, Hour4 },
			_ => new[] { CandleType }
		};
	}

	private void ConfigureSubscription(DataType type, MarketDataMessage subscription)
	{
		if (!_timeFrames.TryGetValue(type, out var state))
		return;

		subscription
		.Bind(state.Rsi, (candle, value) => ProcessRsi(state, candle, value))
		.BindEx(state.Stochastic, (candle, indicatorValue) => ProcessStochastic(state, candle, indicatorValue))
		.Bind(state.Bollinger, (candle, middle, upper, lower) => ProcessBollinger(state, candle, middle, upper, lower))
		.WhenCandlesFinished(candle => OnCandleFinished(type, candle))
		.Start();
	}

	private void ProcessRsi(TimeFrameState state, ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		state.RsiValue = rsiValue;
	}

	private void ProcessStochastic(TimeFrameState state, ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!indicatorValue.IsFinal)
		return;

		if (indicatorValue is not StochasticOscillatorValue stoch)
		return;

		if (stoch.K is decimal k)
		state.StochasticMain = k;

		if (stoch.D is decimal d)
		state.StochasticSignal = d;
	}

	private void ProcessBollinger(TimeFrameState state, ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;

		state.BollingerMiddle = middle;
		state.BollingerUpper = upper;
		state.BollingerLower = lower;
	}

	private void OnCandleFinished(DataType type, ICandleMessage candle)
	{
		if (!_timeFrames.TryGetValue(type, out var state))
		return;

		if (candle.State != CandleStates.Finished)
		return;

		state.LastClosePrice = candle.ClosePrice;
		state.Signal = DetermineSignal(state, candle.ClosePrice);
		state.LastUpdateTime = candle.CloseTime;

		if (!DataTypeComparer.Instance.Equals(type, CandleType))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var pipSize = GetPipSize();

		if (UpdateExits(candle.ClosePrice, pipSize))
		{
			return;
		}

		ExecuteTrading();
	}

	private bool UpdateExits(decimal closePrice, decimal pipSize)
	{
		if (Position > 0m)
		{
			var entryPrice = Position.AveragePrice;

			if (StopLossPips > 0)
			{
				var stopPrice = entryPrice - StopLossPips * pipSize;
				if (closePrice <= stopPrice)
				{
					_longTrailingPrice = null;
					ClosePosition();
					return true;
				}
			}

			if (TakeProfitPips > 0)
			{
				var takePrice = entryPrice + TakeProfitPips * pipSize;
				if (closePrice >= takePrice)
				{
					_longTrailingPrice = null;
					ClosePosition();
					return true;
				}
			}

			if (TrailingStartPips > 0 && TrailingGapPips > 0)
			{
				var activation = entryPrice + TrailingStartPips * pipSize;
				if (closePrice >= activation)
				{
					var newTrail = closePrice - TrailingGapPips * pipSize;
					if (!_longTrailingPrice.HasValue || newTrail > _longTrailingPrice.Value)
					_longTrailingPrice = newTrail;
				}

				if (_longTrailingPrice.HasValue && closePrice <= _longTrailingPrice.Value && closePrice > entryPrice)
				{
					_longTrailingPrice = null;
					ClosePosition();
					return true;
				}
			}
			else
			{
				_longTrailingPrice = null;
			}
		}
		else
		{
			_longTrailingPrice = null;
		}

		if (Position < 0m)
		{
			var entryPrice = Position.AveragePrice;

			if (StopLossPips > 0)
			{
				var stopPrice = entryPrice + StopLossPips * pipSize;
				if (closePrice >= stopPrice)
				{
					_shortTrailingPrice = null;
					ClosePosition();
					return true;
				}
			}

			if (TakeProfitPips > 0)
			{
				var takePrice = entryPrice - TakeProfitPips * pipSize;
				if (closePrice <= takePrice)
				{
					_shortTrailingPrice = null;
					ClosePosition();
					return true;
				}
			}

			if (TrailingStartPips > 0 && TrailingGapPips > 0)
			{
				var activation = entryPrice - TrailingStartPips * pipSize;
				if (closePrice <= activation)
				{
					var newTrail = closePrice + TrailingGapPips * pipSize;
					if (!_shortTrailingPrice.HasValue || newTrail < _shortTrailingPrice.Value)
					_shortTrailingPrice = newTrail;
				}

				if (_shortTrailingPrice.HasValue && closePrice >= _shortTrailingPrice.Value && closePrice < entryPrice)
				{
					_shortTrailingPrice = null;
					ClosePosition();
					return true;
				}
			}
			else
			{
				_shortTrailingPrice = null;
			}
		}
		else
		{
			_shortTrailingPrice = null;
		}

		return false;
	}

	private void ExecuteTrading()
	{
		var signal = AggregateSignal();
		if (signal == SignalDirection.None)
		return;

		var desiredSide = GetDesiredSide(signal);
		if (desiredSide == null)
		return;

		if (Position > 0m && desiredSide == Sides.Sell)
		{
			_longTrailingPrice = null;
			ClosePosition();
			return;
		}

		if (Position < 0m && desiredSide == Sides.Buy)
		{
			_shortTrailingPrice = null;
			ClosePosition();
			return;
		}

		if (Position != 0m)
		return;

		var volume = NormalizeVolume(TradeVolume);
		if (volume <= 0m)
		return;

		if (desiredSide == Sides.Buy)
		{
			_longTrailingPrice = null;
			BuyMarket(volume);
		}
		else
		{
			_shortTrailingPrice = null;
			SellMarket(volume);
		}
	}

	private SignalDirection AggregateSignal()
	{
		SignalDirection? direction = null;

		foreach (var type in GetConfirmationTypes())
		{
			if (!_timeFrames.TryGetValue(type, out var state))
			return SignalDirection.None;

			if (state.Signal == SignalDirection.None)
			return SignalDirection.None;

			if (direction == null)
			{
				direction = state.Signal;
				continue;
			}

			if (direction != state.Signal)
			return SignalDirection.None;
		}

		return direction ?? SignalDirection.None;
	}

	private Sides? GetDesiredSide(SignalDirection signal)
	{
		return (TradeDirection, signal) switch
		{
			(TradeDirectionMode.Trend, SignalDirection.Up) => Sides.Buy,
			(TradeDirectionMode.Trend, SignalDirection.Down) => Sides.Sell,
			(TradeDirectionMode.CounterTrend, SignalDirection.Up) => Sides.Sell,
			(TradeDirectionMode.CounterTrend, SignalDirection.Down) => Sides.Buy,
			_ => null
		};
	}

	private SignalDirection DetermineSignal(TimeFrameState state, decimal closePrice)
	{
		switch (IndicatorMode)
		{
			case RrsIndicatorMode.Rsi:
				{
					if (state.RsiValue is not decimal rsi)
					return SignalDirection.None;

					if (rsi >= RsiUpperLevel && rsi > RsiLowerLevel)
					return SignalDirection.Up;

					if (rsi <= RsiLowerLevel && rsi < RsiUpperLevel)
					return SignalDirection.Down;

					break;
				}
				case RrsIndicatorMode.Stochastic:
					{
						if (state.StochasticMain is not decimal k || state.StochasticSignal is not decimal d)
						return SignalDirection.None;

						var up = k >= StochasticUpperLevel && d >= StochasticUpperLevel;
						var down = k <= StochasticLowerLevel && d <= StochasticLowerLevel;

						if (up == down)
						return SignalDirection.None;

						return up ? SignalDirection.Up : SignalDirection.Down;
					}
					case RrsIndicatorMode.BollingerBands:
						{
							if (state.BollingerUpper is not decimal upper || state.BollingerLower is not decimal lower)
							return SignalDirection.None;

							if (closePrice >= upper && closePrice > lower)
							return SignalDirection.Up;

							if (closePrice <= lower && closePrice < upper)
							return SignalDirection.Down;

							break;
						}
						case RrsIndicatorMode.RsiStochasticBollinger:
							{
								if (state.RsiValue is not decimal rsi ||
								state.StochasticMain is not decimal k ||
								state.StochasticSignal is not decimal d ||
								state.BollingerUpper is not decimal upper ||
								state.BollingerLower is not decimal lower)
								{
									return SignalDirection.None;
								}

								var comboUp = rsi >= RsiUpperLevel &&
								k >= StochasticUpperLevel &&
								d >= StochasticUpperLevel &&
								closePrice >= upper;

								var comboDown = rsi <= RsiLowerLevel &&
								k <= StochasticLowerLevel &&
								d <= StochasticLowerLevel &&
								closePrice <= lower;

								if (comboUp == comboDown)
								return SignalDirection.None;

								return comboUp ? SignalDirection.Up : SignalDirection.Down;
							}
						}

						return SignalDirection.None;
					}

					private decimal GetPipSize()
					{
						if (Security?.PriceStep is decimal step && step > 0m)
						return step;

						return 0.0001m;
					}

					private decimal NormalizeVolume(decimal volume)
					{
						if (Security?.VolumeStep is decimal step && step > 0m)
						{
							var rounded = Math.Round(volume / step) * step;
							return Math.Max(step, rounded);
						}

						return volume;
					}

					private sealed class TimeFrameState
					{
						public RelativeStrengthIndex Rsi { get; init; } = null!;
						public StochasticOscillator Stochastic { get; init; } = null!;
						public BollingerBands Bollinger { get; init; } = null!;
						public decimal? RsiValue { get; set; }
						public decimal? StochasticMain { get; set; }
						public decimal? StochasticSignal { get; set; }
						public decimal? BollingerMiddle { get; set; }
						public decimal? BollingerUpper { get; set; }
						public decimal? BollingerLower { get; set; }
						public decimal? LastClosePrice { get; set; }
						public DateTimeOffset? LastUpdateTime { get; set; }
						public SignalDirection Signal { get; set; }
					}
				}

