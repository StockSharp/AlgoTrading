namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo.Candles;

/// <summary>
/// Port of the MetaTrader expert advisor NTOqF (versions V1-V3).
/// Combines RSI, Stochastic, ADX, Parabolic SAR, and Moving Average filters with optional multi-timeframe analysis.
/// Supports classic stop/take-profit levels and a configurable trailing stop measured in pips.
/// </summary>
public class NtoQfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _shift;

	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<int> _rsiTimeFrame;

	private readonly StrategyParam<bool> _useStochastic;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<bool> _useStochasticHighLow;
	private readonly StrategyParam<decimal> _stochasticHigh;
	private readonly StrategyParam<decimal> _stochasticLow;
	private readonly StrategyParam<int> _stochasticTimeFrame;

	private readonly StrategyParam<bool> _useAdx;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _adxTimeFrame;
	private readonly StrategyParam<bool> _useAdxMain;
	private readonly StrategyParam<decimal> _adxMainThreshold;
	private readonly StrategyParam<int> _adxMainTimeFrame;

	private readonly StrategyParam<bool> _useSar;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _sarTimeFrame;

	private readonly StrategyParam<bool> _useMa;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethods> _maMethod;
	private readonly StrategyParam<AppliedPrices> _maAppliedPrices;
	private readonly StrategyParam<int> _maTimeFrame;

	private decimal _pipSize;

	private ValueBuffer<decimal> _closeBuffer = null!;
	private readonly Dictionary<DataType, ISubscriptionHandler<ICandleMessage>> _subscriptions = new();
	private readonly Dictionary<DataType, ValueBuffer<decimal>> _rsiBuffers = new();
	private readonly Dictionary<DataType, ValueBuffer<StochasticSnapshot>> _stochasticBuffers = new();
	private readonly Dictionary<DataType, ValueBuffer<AdxSnapshot>> _adxDirectionalBuffers = new();
	private readonly Dictionary<DataType, ValueBuffer<decimal>> _adxMainBuffers = new();
	private readonly Dictionary<DataType, ValueBuffer<decimal>> _sarBuffers = new();
	private readonly Dictionary<DataType, ValueBuffer<decimal>> _maBuffers = new();

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Trading timeframe for candle subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
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
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enables the trailing stop manager.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Offset (bars ago) used when reading indicator values.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Enables the RSI filter.
	/// </summary>
	public bool UseRsi
	{
		get => _useRsi.Value;
		set => _useRsi.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought threshold (short signal).
	/// </summary>
	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	/// <summary>
	/// RSI oversold threshold (long signal).
	/// </summary>
	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	/// <summary>
	/// RSI timeframe in minutes (0 uses the trading timeframe).
	/// </summary>
	public int RsiTimeFrame
	{
		get => _rsiTimeFrame.Value;
		set => _rsiTimeFrame.Value = value;
	}

	/// <summary>
	/// Enables the stochastic oscillator filter.
	/// </summary>
	public bool UseStochastic
	{
		get => _useStochastic.Value;
		set => _useStochastic.Value = value;
	}

	/// <summary>
	/// %K length for the stochastic oscillator.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// %D length for the stochastic oscillator.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing factor (slowing) applied to %K.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Enables the stochastic overbought/oversold validation.
	/// </summary>
	public bool UseStochasticHighLow
	{
		get => _useStochasticHighLow.Value;
		set => _useStochasticHighLow.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level used with the high/low filter.
	/// </summary>
	public decimal StochasticHigh
	{
		get => _stochasticHigh.Value;
		set => _stochasticHigh.Value = value;
	}

	/// <summary>
	/// Stochastic oversold level used with the high/low filter.
	/// </summary>
	public decimal StochasticLow
	{
		get => _stochasticLow.Value;
		set => _stochasticLow.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator timeframe in minutes (0 uses the trading timeframe).
	/// </summary>
	public int StochasticTimeFrame
	{
		get => _stochasticTimeFrame.Value;
		set => _stochasticTimeFrame.Value = value;
	}

	/// <summary>
	/// Enables ADX +DI/-DI directional filter.
	/// </summary>
	public bool UseAdx
	{
		get => _useAdx.Value;
		set => _useAdx.Value = value;
	}

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Timeframe for ADX directional components (minutes, 0 for trading timeframe).
	/// </summary>
	public int AdxTimeFrame
	{
		get => _adxTimeFrame.Value;
		set => _adxTimeFrame.Value = value;
	}

	/// <summary>
	/// Enables ADX main-line strength filter.
	/// </summary>
	public bool UseAdxMain
	{
		get => _useAdxMain.Value;
		set => _useAdxMain.Value = value;
	}

	/// <summary>
	/// Minimum ADX main value required when the strength filter is enabled.
	/// </summary>
	public decimal AdxMainThreshold
	{
		get => _adxMainThreshold.Value;
		set => _adxMainThreshold.Value = value;
	}

	/// <summary>
	/// Timeframe for the ADX main strength filter (minutes, 0 for trading timeframe).
	/// </summary>
	public int AdxMainTimeFrame
	{
		get => _adxMainTimeFrame.Value;
		set => _adxMainTimeFrame.Value = value;
	}

	/// <summary>
	/// Enables the Parabolic SAR filter.
	/// </summary>
	public bool UseSar
	{
		get => _useSar.Value;
		set => _useSar.Value = value;
	}

	/// <summary>
	/// SAR acceleration factor.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum SAR acceleration.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Timeframe for Parabolic SAR (minutes, 0 for trading timeframe).
	/// </summary>
	public int SarTimeFrame
	{
		get => _sarTimeFrame.Value;
		set => _sarTimeFrame.Value = value;
	}

	/// <summary>
	/// Enables the moving-average filter.
	/// </summary>
	public bool UseMa
	{
		get => _useMa.Value;
		set => _useMa.Value = value;
	}

	/// <summary>
	/// Moving-average lookback period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Additional shift applied to the moving average buffer.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving-average calculation method.
	/// </summary>
	public MovingAverageMethods MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price field applied when calculating the moving average.
	/// </summary>
	public AppliedPrices MaAppliedPrices
	{
		get => _maAppliedPrices.Value;
		set => _maAppliedPrices.Value = value;
	}

	/// <summary>
	/// Timeframe for the moving-average filter (minutes, 0 for trading timeframe).
	/// </summary>
	public int MaTimeFrame
	{
		get => _maTimeFrame.Value;
		set => _maTimeFrame.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NtoQfStrategy"/>.
	/// </summary>
	public NtoQfStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary trading timeframe", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Volume", "Order volume (lots)", "General")
		.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 80m)
		.SetDisplay("Take Profit (pips)", "Profit target distance", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 10m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 6m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_shift = Param(nameof(Shift), 1)
		.SetDisplay("Shift", "Number of completed candles back", "General")
		.SetNotNegative();

		_useRsi = Param(nameof(UseRsi), true)
		.SetDisplay("Use RSI", "Enable RSI filter", "RSI");

		_rsiPeriod = Param(nameof(RsiPeriod), 4)
		.SetDisplay("RSI Period", "RSI lookback", "RSI")
		.SetGreaterThanZero();

		_rsiUpper = Param(nameof(RsiUpper), 90m)
		.SetDisplay("RSI Upper", "Sell threshold", "RSI");

		_rsiLower = Param(nameof(RsiLower), 10m)
		.SetDisplay("RSI Lower", "Buy threshold", "RSI");

		_rsiTimeFrame = Param(nameof(RsiTimeFrame), 0)
		.SetDisplay("RSI TF (min)", "RSI timeframe in minutes", "RSI");

		_useStochastic = Param(nameof(UseStochastic), true)
		.SetDisplay("Use Stochastic", "Enable stochastic filter", "Stochastic");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
		.SetDisplay("%K Length", "Stochastic %K period", "Stochastic")
		.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
		.SetDisplay("%D Length", "Stochastic %D period", "Stochastic")
		.SetGreaterThanZero();

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
		.SetDisplay("Slowing", "Stochastic slowing factor", "Stochastic")
		.SetGreaterThanZero();

		_useStochasticHighLow = Param(nameof(UseStochasticHighLow), true)
		.SetDisplay("Use High/Low", "Require stoch above/below bands", "Stochastic");

		_stochasticHigh = Param(nameof(StochasticHigh), 80m)
		.SetDisplay("Stoch High", "Overbought level", "Stochastic");

		_stochasticLow = Param(nameof(StochasticLow), 20m)
		.SetDisplay("Stoch Low", "Oversold level", "Stochastic");

		_stochasticTimeFrame = Param(nameof(StochasticTimeFrame), 0)
		.SetDisplay("Stoch TF (min)", "Stochastic timeframe", "Stochastic");

		_useAdx = Param(nameof(UseAdx), true)
		.SetDisplay("Use ADX", "Enable ADX DI filter", "ADX");

		_adxPeriod = Param(nameof(AdxPeriod), 50)
		.SetDisplay("ADX Period", "ADX lookback", "ADX")
		.SetGreaterThanZero();

		_adxTimeFrame = Param(nameof(AdxTimeFrame), 0)
		.SetDisplay("ADX TF (min)", "ADX DI timeframe", "ADX");

		_useAdxMain = Param(nameof(UseAdxMain), false)
		.SetDisplay("Use ADX Main", "Enable ADX strength filter", "ADX");

		_adxMainThreshold = Param(nameof(AdxMainThreshold), 25m)
		.SetDisplay("ADX Main", "Minimum ADX main value", "ADX");

		_adxMainTimeFrame = Param(nameof(AdxMainTimeFrame), 0)
		.SetDisplay("ADX Main TF", "ADX main timeframe", "ADX");

		_useSar = Param(nameof(UseSar), false)
		.SetDisplay("Use SAR", "Enable Parabolic SAR filter", "SAR");

		_sarStep = Param(nameof(SarStep), 0.02m)
		.SetDisplay("SAR Step", "Acceleration factor", "SAR")
		.SetGreaterThanZero();

		_sarMax = Param(nameof(SarMax), 0.2m)
		.SetDisplay("SAR Max", "Maximum acceleration", "SAR")
		.SetGreaterThanZero();

		_sarTimeFrame = Param(nameof(SarTimeFrame), 0)
		.SetDisplay("SAR TF (min)", "Parabolic SAR timeframe", "SAR");

		_useMa = Param(nameof(UseMa), false)
		.SetDisplay("Use MA", "Enable moving-average filter", "MA");

		_maPeriod = Param(nameof(MaPeriod), 50)
		.SetDisplay("MA Period", "Moving-average length", "MA")
		.SetGreaterThanZero();

		_maShift = Param(nameof(MaShift), 0)
		.SetDisplay("MA Shift", "Extra bars applied to MA", "MA");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethods.Simple)
		.SetDisplay("MA Method", "Moving-average type", "MA");

		_maAppliedPrices = Param(nameof(MaAppliedPrices), AppliedPrices.Close)
		.SetDisplay("MA Price", "Price source for MA", "MA");

		_maTimeFrame = Param(nameof(MaTimeFrame), 0)
		.SetDisplay("MA TF (min)", "Moving-average timeframe", "MA");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var types = new HashSet<DataType> { CandleType };

		if (UseRsi)
			types.Add(ResolveTimeFrame(RsiTimeFrame));

		if (UseStochastic)
			types.Add(ResolveTimeFrame(StochasticTimeFrame));

		if (UseAdx)
			types.Add(ResolveTimeFrame(AdxTimeFrame));

		if (UseAdxMain)
			types.Add(ResolveTimeFrame(AdxMainTimeFrame));

		if (UseSar)
			types.Add(ResolveTimeFrame(SarTimeFrame));

		if (UseMa)
			types.Add(ResolveTimeFrame(MaTimeFrame));

		foreach (var type in types)
			yield return (Security, type);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();
		_closeBuffer = new ValueBuffer<decimal>(GetRequiredCapacity(Math.Max(0, MaShift)));

		_subscriptions.Clear();
		_rsiBuffers.Clear();
		_stochasticBuffers.Clear();
		_adxDirectionalBuffers.Clear();
		_adxMainBuffers.Clear();
		_sarBuffers.Clear();
		_maBuffers.Clear();

		SetupRsi();
		SetupStochastic();
		SetupAdx();
		SetupAdxMain();
		SetupSar();
		SetupMa();
		SetupBaseSubscription();

		foreach (var subscription in _subscriptions.Values)
			subscription.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		_subscriptions.Clear();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position > 0m)
		{
			_entryPrice = PositionPrice ?? _entryPrice;
			_takePrice = TakeProfitPips > 0m ? _entryPrice + TakeProfitPips * _pipSize : 0m;
			_stopPrice = StopLossPips > 0m ? _entryPrice - StopLossPips * _pipSize : 0m;
		}
		else if (Position < 0m)
		{
			_entryPrice = PositionPrice ?? _entryPrice;
			_takePrice = TakeProfitPips > 0m ? _entryPrice - TakeProfitPips * _pipSize : 0m;
			_stopPrice = StopLossPips > 0m ? _entryPrice + StopLossPips * _pipSize : 0m;
		}
		else
		{
			ResetProtection();
		}
	}

	private void SetupBaseSubscription()
	{
		var subscription = GetOrCreateSubscription(CandleType);
		subscription.Bind(ProcessBaseCandle);
	}

	private void SetupRsi()
	{
		if (!UseRsi)
			return;

		var timeframe = ResolveTimeFrame(RsiTimeFrame);
		var buffer = CreateBuffer(_rsiBuffers, timeframe, GetRequiredCapacity());
		var indicator = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = GetOrCreateSubscription(timeframe);

		subscription.Bind(indicator, (candle, value) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			buffer.Add(value);
		});
	}

	private void SetupStochastic()
	{
		if (!UseStochastic)
			return;

		var timeframe = ResolveTimeFrame(StochasticTimeFrame);
		var buffer = CreateBuffer(_stochasticBuffers, timeframe, GetRequiredCapacity());

		var stochastic = new StochasticOscillator
		{
			Length = StochasticKPeriod,
			K = { Length = StochasticSlowing },
			D = { Length = StochasticDPeriod },
		};

		var subscription = GetOrCreateSubscription(timeframe);
		subscription.Bind(stochastic, (candle, main, signal) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			buffer.Add(new StochasticSnapshot(main, signal));
		});
	}

	private void SetupAdx()
	{
		if (!UseAdx)
			return;

		var timeframe = ResolveTimeFrame(AdxTimeFrame);
		var buffer = CreateBuffer(_adxDirectionalBuffers, timeframe, GetRequiredCapacity());
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var subscription = GetOrCreateSubscription(timeframe);

		subscription.BindEx(adx, (candle, value) =>
		{
			if (candle.State != CandleStates.Finished || !value.IsFinal)
				return;

			var data = (AverageDirectionalIndexValue)value;
			if (data.Dx.Plus is not decimal plus || data.Dx.Minus is not decimal minus)
				return;

			var main = data.MovingAverage is decimal adxMain ? adxMain : (decimal?)null;
			buffer.Add(new AdxSnapshot(plus, minus, main));
		});
	}

	private void SetupAdxMain()
	{
		if (!UseAdxMain)
			return;

		var strengthFrame = ResolveTimeFrame(AdxMainTimeFrame);

		if (UseAdx && strengthFrame == ResolveTimeFrame(AdxTimeFrame))
			return;

		var buffer = CreateBuffer(_adxMainBuffers, strengthFrame, GetRequiredCapacity());
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var subscription = GetOrCreateSubscription(strengthFrame);

		subscription.BindEx(adx, (candle, value) =>
		{
			if (candle.State != CandleStates.Finished || !value.IsFinal)
				return;

			var data = (AverageDirectionalIndexValue)value;
			if (data.MovingAverage is not decimal adxMain)
				return;

			buffer.Add(adxMain);
		});
	}

	private void SetupSar()
	{
		if (!UseSar)
			return;

		var timeframe = ResolveTimeFrame(SarTimeFrame);
		var buffer = CreateBuffer(_sarBuffers, timeframe, GetRequiredCapacity());
		var sar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMax,
		};

		var subscription = GetOrCreateSubscription(timeframe);
		subscription.Bind(sar, (candle, value) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			buffer.Add(value);
		});
	}

	private void SetupMa()
	{
		if (!UseMa)
			return;

		var timeframe = ResolveTimeFrame(MaTimeFrame);
		var buffer = CreateBuffer(_maBuffers, timeframe, GetRequiredCapacity(Math.Max(0, MaShift)));
		var ma = CreateMovingAverageIndicator();
		var subscription = GetOrCreateSubscription(timeframe);

		subscription.Bind(ma, (candle, value) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			buffer.Add(value);
		});
	}

	private void ProcessBaseCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closeBuffer.Add(candle.ClosePrice);

		if (ManagePosition(candle))
			return;

		if (Position != 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var signal = GenerateSignal();
		if (signal is null)
			return;

		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		if (signal == TrendDirections.Long)
		{
			BuyMarket(volume);
			LogInfo($"Enter long at {candle.ClosePrice}");
		}
		else
		{
			SellMarket(volume);
			LogInfo($"Enter short at {candle.ClosePrice}");
		}
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position == 0m)
			return false;

		var trailingDistance = TrailingStopPips * _pipSize;

		if (Position > 0m)
		{
			var stopHit = _stopPrice > 0m && candle.LowPrice <= _stopPrice;
			var takeHit = _takePrice > 0m && candle.HighPrice >= _takePrice;

			if (stopHit || takeHit)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}

			if (UseTrailingStop && TrailingStopPips > 0m && _entryPrice > 0m && trailingDistance > 0m)
			{
				var profitPips = (_pipSize > 0m) ? (candle.ClosePrice - _entryPrice) / _pipSize : 0m;
				var distancePips = (_pipSize > 0m && _stopPrice > 0m) ? (candle.ClosePrice - _stopPrice) / _pipSize : decimal.MaxValue;

				if (profitPips > TrailingStopPips && distancePips > TrailingStopPips)
				{
					var desiredStop = candle.ClosePrice - trailingDistance;
					if (_stopPrice <= 0m || desiredStop > _stopPrice)
					{
						_stopPrice = desiredStop;
						LogInfo($"Update long trailing stop to {_stopPrice}");
					}
				}
			}
		}
		else
		{
			var stopHit = _stopPrice > 0m && candle.HighPrice >= _stopPrice;
			var takeHit = _takePrice > 0m && candle.LowPrice <= _takePrice;

			if (stopHit || takeHit)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}

			if (UseTrailingStop && TrailingStopPips > 0m && _entryPrice > 0m && trailingDistance > 0m)
			{
				var profitPips = (_pipSize > 0m) ? (_entryPrice - candle.ClosePrice) / _pipSize : 0m;
				var distancePips = (_pipSize > 0m && _stopPrice > 0m) ? (_stopPrice - candle.ClosePrice) / _pipSize : decimal.MaxValue;

				if (profitPips > TrailingStopPips && distancePips > TrailingStopPips)
				{
					var desiredStop = candle.ClosePrice + trailingDistance;
					if (_stopPrice <= 0m || desiredStop < _stopPrice)
					{
						_stopPrice = desiredStop;
						LogInfo($"Update short trailing stop to {_stopPrice}");
					}
				}
			}
		}

		return false;
	}

	private TrendDirections? GenerateSignal()
	{
		var directions = new List<TrendDirections>();

		if (!TryGetRsiSignal(out var rsiDirection))
			return null;
		if (rsiDirection is TrendDirections rsi)
			directions.Add(rsi);
		else if (UseRsi)
			return null;

		if (!TryGetStochasticSignal(out var stoDirection))
			return null;
		if (stoDirection is TrendDirections sto)
			directions.Add(sto);
		else if (UseStochastic)
			return null;

		if (!TryGetAdxDirection(out var adxDirection))
			return null;
		if (adxDirection is TrendDirections adx)
			directions.Add(adx);

		if (!IsAdxMainSatisfied())
			return null;

		if (!TryGetSarDirection(out var sarDirection))
			return null;
		if (sarDirection is TrendDirections sar)
			directions.Add(sar);
		else if (UseSar)
			return null;

		if (!TryGetMaDirection(out var maDirection))
			return null;
		if (maDirection is TrendDirections ma)
			directions.Add(ma);
		else if (UseMa)
			return null;

		if (directions.Count == 0)
			return null;

		var first = directions[0];
		foreach (var direction in directions)
		{
			if (direction != first)
				return null;
		}

		return first;
	}

	private bool TryGetRsiSignal(out TrendDirections? direction)
	{
		direction = null;

		if (!UseRsi)
			return true;

		var timeframe = ResolveTimeFrame(RsiTimeFrame);
		if (!_rsiBuffers.TryGetValue(timeframe, out var buffer))
			return false;

		var shift = Math.Max(0, Shift);
		if (!buffer.TryGet(shift, out var value))
			return false;

		if (value > RsiUpper)
			direction = TrendDirections.Short;
		else if (value < RsiLower)
			direction = TrendDirections.Long;

		return true;
	}

	private bool TryGetStochasticSignal(out TrendDirections? direction)
	{
		direction = null;

		if (!UseStochastic)
			return true;

		var timeframe = ResolveTimeFrame(StochasticTimeFrame);
		if (!_stochasticBuffers.TryGetValue(timeframe, out var buffer))
			return false;

		var shift = Math.Max(0, Shift);
		if (!buffer.TryGet(shift, out var snapshot))
			return false;

		if (UseStochasticHighLow)
		{
			if (snapshot.Main > snapshot.Signal && snapshot.Main > StochasticHigh)
				direction = TrendDirections.Long;
			else if (snapshot.Main < snapshot.Signal && snapshot.Main < StochasticLow)
				direction = TrendDirections.Short;
		}
		else
		{
			direction = snapshot.Main > snapshot.Signal ? TrendDirections.Long : TrendDirections.Short;
		}

		return true;
	}

	private bool TryGetAdxDirection(out TrendDirections? direction)
	{
		direction = null;

		if (!UseAdx)
			return true;

		var timeframe = ResolveTimeFrame(AdxTimeFrame);
		if (!_adxDirectionalBuffers.TryGetValue(timeframe, out var buffer))
			return false;

		var shift = Math.Max(0, Shift);
		if (!buffer.TryGet(shift, out var snapshot))
			return false;

		direction = snapshot.PlusDi > snapshot.MinusDi ? TrendDirections.Long : TrendDirections.Short;
		return true;
	}

	private bool TryGetSarDirection(out TrendDirections? direction)
	{
		direction = null;

		if (!UseSar)
			return true;

		var timeframe = ResolveTimeFrame(SarTimeFrame);
		if (!_sarBuffers.TryGetValue(timeframe, out var buffer))
			return false;

		var shift = Math.Max(0, Shift);
		if (!buffer.TryGet(shift, out var sarValue))
			return false;

		if (!TryGetClose(shift, out var close))
			return false;

		direction = sarValue > close ? TrendDirections.Long : TrendDirections.Short;
		return true;
	}

	private bool TryGetMaDirection(out TrendDirections? direction)
	{
		direction = null;

		if (!UseMa)
			return true;

		var timeframe = ResolveTimeFrame(MaTimeFrame);
		if (!_maBuffers.TryGetValue(timeframe, out var buffer))
			return false;

		var shift = Math.Max(0, Shift + Math.Max(0, MaShift));
		if (!buffer.TryGet(shift, out var maValue))
			return false;

		if (!TryGetClose(Math.Max(0, Shift), out var close))
			return false;

		direction = maValue < close ? TrendDirections.Long : TrendDirections.Short;
		return true;
	}

	private bool IsAdxMainSatisfied()
	{
		if (!UseAdxMain)
			return true;

		var shift = Math.Max(0, Shift);
		var strengthFrame = ResolveTimeFrame(AdxMainTimeFrame);
		var adxFrame = ResolveTimeFrame(AdxTimeFrame);

		if (UseAdx && strengthFrame == adxFrame)
		{
			if (!_adxDirectionalBuffers.TryGetValue(strengthFrame, out var buffer))
				return false;

			if (!buffer.TryGet(shift, out var snapshot))
				return false;

			if (snapshot.AdxMain is not decimal adxMain)
				return false;

			return adxMain > AdxMainThreshold;
		}

		if (!_adxMainBuffers.TryGetValue(strengthFrame, out var mainBuffer))
			return false;

		if (!mainBuffer.TryGet(shift, out var value))
			return false;

		return value > AdxMainThreshold;
	}

	private bool TryGetClose(int shift, out decimal price)
	{
		return _closeBuffer.TryGet(Math.Max(0, shift), out price);
	}

	private ISubscriptionHandler<ICandleMessage> GetOrCreateSubscription(DataType timeframe)
	{
		if (_subscriptions.TryGetValue(timeframe, out var series))
			return series;

		series = SubscribeCandles(timeframe);
		_subscriptions.Add(timeframe, series);
		return series;
	}

	private ValueBuffer<T> CreateBuffer<T>(Dictionary<DataType, ValueBuffer<T>> storage, DataType timeframe, int capacity)
	{
		if (!storage.TryGetValue(timeframe, out var buffer))
		{
			buffer = new ValueBuffer<T>(capacity);
			storage.Add(timeframe, buffer);
		}

		return buffer;
	}

	private DataType ResolveTimeFrame(int minutes)
	{
		return minutes <= 0 ? CandleType : TimeSpan.FromMinutes(minutes).TimeFrame();
	}

	private LengthIndicator<decimal> CreateMovingAverageIndicator()
	{
		var indicator = MaMethod switch
		{
			MovingAverageMethods.Simple => new SimpleMovingAverage { Length = MaPeriod },
			MovingAverageMethods.Exponential => new ExponentialMovingAverage { Length = MaPeriod },
			MovingAverageMethods.Smoothed => new SmoothedMovingAverage { Length = MaPeriod },
			MovingAverageMethods.LinearWeighted => new WeightedMovingAverage { Length = MaPeriod },
			_ => new SimpleMovingAverage { Length = MaPeriod },
		};

		indicator.CandlePrice = ConvertAppliedPrices(MaAppliedPrices);
		return indicator;
	}

	private CandlePrice ConvertAppliedPrices(AppliedPrices price)
	{
		return price switch
		{
			AppliedPrices.Close => CandlePrice.Close,
			AppliedPrices.Open => CandlePrice.Open,
			AppliedPrices.High => CandlePrice.High,
			AppliedPrices.Low => CandlePrice.Low,
			AppliedPrices.Median => CandlePrice.Median,
			AppliedPrices.Typical => CandlePrice.Typical,
			AppliedPrices.Weighted => CandlePrice.Weighted,
			_ => CandlePrice.Close,
		};
	}

	private int GetRequiredCapacity(int extra = 0)
	{
		var baseShift = Math.Max(0, Shift);
		return Math.Max(2, baseShift + Math.Max(0, extra) + 2);
	}

	private decimal GetPipSize()
	{
		var step = Security.PriceStep ?? 0.0001m;
		var decimals = Security.Decimals ?? 0;

		if (decimals >= 3)
			return step * 10m;

		return step > 0m ? step : 0.0001m;
	}

	private void ResetProtection()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	private enum TrendDirections
	{
		Long,
		Short,
	}

	public enum MovingAverageMethods
	{
		Simple = 0,
		Exponential = 1,
		Smoothed = 2,
		LinearWeighted = 3,
	}

	public enum AppliedPrices
	{
		Close = 0,
		Open = 1,
		High = 2,
		Low = 3,
		Median = 4,
		Typical = 5,
		Weighted = 6,
	}

	private readonly struct StochasticSnapshot
	{
		public StochasticSnapshot(decimal main, decimal signal)
		{
			Main = main;
			Signal = signal;
		}

		public decimal Main { get; }
		public decimal Signal { get; }
	}

	private readonly struct AdxSnapshot
	{
		public AdxSnapshot(decimal plusDi, decimal minusDi, decimal? adxMain)
		{
			PlusDi = plusDi;
			MinusDi = minusDi;
			AdxMain = adxMain;
		}

		public decimal PlusDi { get; }
		public decimal MinusDi { get; }
		public decimal? AdxMain { get; }
	}

	private sealed class ValueBuffer<T>
	{
		private readonly int _capacity;
		private readonly List<T> _values;

		public ValueBuffer(int capacity)
		{
			_capacity = Math.Max(2, capacity);
			_values = new List<T>(_capacity);
		}

		public void Add(T value)
		{
			if (_values.Count == _capacity)
				_values.RemoveAt(_values.Count - 1);

			_values.Insert(0, value);
		}

		public bool TryGet(int shift, out T value)
		{
			if (shift < 0 || shift >= _values.Count)
			{
				value = default!;
				return false;
			}

			value = _values[shift];
			return true;
		}
	}
}