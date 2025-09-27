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

using StockSharp.Algo;

/// <summary>
/// ComFracti strategy converted from the original MT4 expert advisor.
/// Combines fractals, RSI, stochastic oscillator and multiple optional filters.
/// </summary>
public class ComFractiStrategy : Strategy
{
	private readonly StrategyParam<int> _historyCapacity;
	private readonly StrategyParam<decimal> _psarMax;

	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<bool> _profitTrailing;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _accountMicro;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<bool> _useFractals;
	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<bool> _useStochastic;
	private readonly StrategyParam<bool> _closeOnOppositeSignal;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<int> _fractalShiftBuyCurrent;
	private readonly StrategyParam<int> _fractalShiftBuyHigher;
	private readonly StrategyParam<int> _rsiLevelBuy;
	private readonly StrategyParam<int> _stochasticPeriodBuy;
	private readonly StrategyParam<int> _stochasticLevelBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<int> _fractalShiftSellCurrent;
	private readonly StrategyParam<int> _fractalShiftSellHigher;
	private readonly StrategyParam<int> _rsiLevelSell;
	private readonly StrategyParam<int> _stochasticPeriodSell;
	private readonly StrategyParam<int> _stochasticLevelSell;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<bool> _usePsarFilter;
	private readonly StrategyParam<bool> _useChannelFilter;
	private readonly StrategyParam<bool> _usePerceptronFilter;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _psarStep;
	private readonly StrategyParam<int> _channelLookback;
	private readonly StrategyParam<decimal> _channelK;
	private readonly StrategyParam<int> _perceptronV1;
	private readonly StrategyParam<int> _perceptronV2;
	private readonly StrategyParam<int> _perceptronV3;
	private readonly StrategyParam<int> _perceptronV4;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherFractalType;
	private readonly StrategyParam<DataType> _rsiType;
	private readonly StrategyParam<DataType> _filterType;

	private readonly FractalSignalTracker _mainFractals = new();
	private readonly FractalSignalTracker _higherFractals = new();
	private readonly StochasticOscillator _stochasticBuy = new();
	private readonly StochasticOscillator _stochasticSell = new();
	private readonly RelativeStrengthIndex _rsi = new();
	private ExponentialMovingAverage _maFilter = null!;
	private ParabolicSar _psarFilter = null!;

	private decimal? _lastRsiValue;
	private decimal? _lastStochBuy;
	private decimal? _lastStochSell;
	private decimal? _maPrevValue;
	private decimal? _maSlope;
	private decimal? _lastPsarValue;
	private decimal? _longEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortEntryPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private decimal[] _highHistory;
	private decimal[] _lowHistory;
	private decimal[] _closeHistory;
	private decimal[] _openHistory;
	private int _historyCount;

	/// <summary>
	/// Initialize <see cref="ComFractiStrategy"/>.
	/// </summary>
	public ComFractiStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400m)
		.SetDisplay("Take Profit", "Take profit distance in points", "Orders");

		_stopLossPoints = Param(nameof(StopLossPoints), 800m)
		.SetDisplay("Stop Loss", "Stop-loss distance in points", "Orders");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
		.SetDisplay("Use Trailing", "Enable trailing stop management", "Risk");

		_profitTrailing = Param(nameof(ProfitTrailing), true)
		.SetDisplay("Require Profit", "Start trailing only after profit", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 300m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
		.SetDisplay("Base Volume", "Default trading volume", "Volume");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Money Management", "Enable risk based position sizing", "Volume");

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetDisplay("Risk Percent", "Risk per trade in percent", "Volume");

		_accountMicro = Param(nameof(AccountMicro), false)
		.SetDisplay("Micro Account", "Divide volume by ten for micro accounts", "Volume");

		_minimumVolume = Param(nameof(MinimumVolume), 0.1m)
		.SetDisplay("Minimum Volume", "Lower bound for calculated volume", "Volume");

		_historyCapacity = Param(nameof(HistoryCapacity), 256)
		.SetGreaterThanZero()
		.SetDisplay("History Capacity", "Number of bars cached for indicator emulation", "Advanced");

		_useFractals = Param(nameof(UseFractals), true)
		.SetDisplay("Use Fractals", "Enable fractal confirmation", "Signals");

		_useRsi = Param(nameof(UseRsi), true)
		.SetDisplay("Use RSI", "Enable RSI confirmation", "Signals");

		_useStochastic = Param(nameof(UseStochastic), false)
		.SetDisplay("Use Stochastic", "Enable stochastic confirmation", "Signals");

		_closeOnOppositeSignal = Param(nameof(CloseOnOppositeSignal), false)
		.SetDisplay("Close On Opposite", "Exit when opposite signal appears", "Risk");

		_allowBuy = Param(nameof(AllowBuy), true)
		.SetDisplay("Allow Buy", "Enable long entries", "Signals");

		_fractalShiftBuyCurrent = Param(nameof(FractalShiftBuyCurrent), 3)
		.SetDisplay("Fractal Shift Buy", "Shift for current timeframe fractal", "Signals");

		_fractalShiftBuyHigher = Param(nameof(FractalShiftBuyHigher), 3)
		.SetDisplay("Fractal Shift Buy Higher", "Shift for higher timeframe fractal", "Signals");

		_rsiLevelBuy = Param(nameof(RsiLevelBuy), 3)
		.SetDisplay("RSI Buy Offset", "Points below 50 for buy RSI", "Signals");

		_stochasticPeriodBuy = Param(nameof(StochasticPeriodBuy), 5)
		.SetDisplay("Stochastic Buy Period", "K period for buy stochastic", "Signals");

		_stochasticLevelBuy = Param(nameof(StochasticLevelBuy), 20)
		.SetDisplay("Stochastic Buy Offset", "Points below 50 for buy stochastic", "Signals");

		_allowSell = Param(nameof(AllowSell), true)
		.SetDisplay("Allow Sell", "Enable short entries", "Signals");

		_fractalShiftSellCurrent = Param(nameof(FractalShiftSellCurrent), 3)
		.SetDisplay("Fractal Shift Sell", "Shift for current timeframe fractal", "Signals");

		_fractalShiftSellHigher = Param(nameof(FractalShiftSellHigher), 3)
		.SetDisplay("Fractal Shift Sell Higher", "Shift for higher timeframe fractal", "Signals");

		_rsiLevelSell = Param(nameof(RsiLevelSell), 3)
		.SetDisplay("RSI Sell Offset", "Points above 50 for sell RSI", "Signals");

		_stochasticPeriodSell = Param(nameof(StochasticPeriodSell), 5)
		.SetDisplay("Stochastic Sell Period", "K period for sell stochastic", "Signals");

		_stochasticLevelSell = Param(nameof(StochasticLevelSell), 20)
		.SetDisplay("Stochastic Sell Offset", "Points above 50 for sell stochastic", "Signals");

		_useMaFilter = Param(nameof(UseMaFilter), false)
		.SetDisplay("MA Filter", "Enable EMA slope filter", "Filters");

		_usePsarFilter = Param(nameof(UsePsarFilter), false)
		.SetDisplay("PSAR Filter", "Enable Parabolic SAR filter", "Filters");

		_useChannelFilter = Param(nameof(UseChannelFilter), false)
		.SetDisplay("Channel Filter", "Enable channel breakout filter", "Filters");

		_usePerceptronFilter = Param(nameof(UsePerceptronFilter), false)
		.SetDisplay("Perceptron Filter", "Enable weighted high/low perceptron filter", "Filters");

		_maPeriod = Param(nameof(MaPeriod), 26)
		.SetDisplay("MA Period", "EMA period for filter", "Filters");

		_psarStep = Param(nameof(PsarStep), 0.02m)
		.SetDisplay("PSAR Step", "Parabolic SAR acceleration step", "Filters");

		_psarMax = Param(nameof(PsarMax), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("PSAR Max", "Maximum acceleration factor applied by the Parabolic SAR filter", "Filters");

		_channelLookback = Param(nameof(ChannelLookback), 45)
		.SetDisplay("Channel Bars", "Bars used for channel calculation", "Filters");

		_channelK = Param(nameof(ChannelK), 0.1m)
		.SetDisplay("Channel Factor", "Channel offset multiplier", "Filters");

		_perceptronV1 = Param(nameof(PerceptronV1), 55)
		.SetDisplay("Perceptron V1", "Weight applied to high(1) - high(7)", "Filters");

		_perceptronV2 = Param(nameof(PerceptronV2), 55)
		.SetDisplay("Perceptron V2", "Weight applied to high(4) - high(11)", "Filters");

		_perceptronV3 = Param(nameof(PerceptronV3), 55)
		.SetDisplay("Perceptron V3", "Weight applied to low(1) - low(7)", "Filters");

		_perceptronV4 = Param(nameof(PerceptronV4), 55)
		.SetDisplay("Perceptron V4", "Weight applied to low(4) - low(11)", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Main Candles", "Primary timeframe", "Data");

		_higherFractalType = Param(nameof(HigherFractalType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Higher Fractals", "Higher timeframe for fractals", "Data");

		_rsiType = Param(nameof(RsiType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("RSI Candles", "Timeframe used for RSI", "Data");

		_filterType = Param(nameof(FilterType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Filter Candles", "Timeframe for MA/PSAR filters", "Data");

		InitializeHistoryBuffers(HistoryCapacity);

		Volume = _baseVolume.Value;
	}

	private void InitializeHistoryBuffers(int capacity)
	{
		capacity = Math.Max(1, capacity);

		_highHistory = new decimal[capacity];
		_lowHistory = new decimal[capacity];
		_closeHistory = new decimal[capacity];
		_openHistory = new decimal[capacity];
		_historyCount = 0;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Use trailing stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Require price to move into profit before trailing.
	/// </summary>
	public bool ProfitTrailing
	{
		get => _profitTrailing.Value;
		set => _profitTrailing.Value = value;
	}

	/// <summary>
	/// Trailing distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Base trading volume used when money management is disabled.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Enable risk based position sizing.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk percentage used when money management is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Interpret volume as micro lot sizing (divide by ten).
	/// </summary>
	public bool AccountMicro
	{
		get => _accountMicro.Value;
		set => _accountMicro.Value = value;
	}

	/// <summary>
	/// Minimum trade volume allowed after calculations.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minimumVolume.Value;
		set => _minimumVolume.Value = value;
	}

	/// <summary>
	/// Number of bars cached for indicator emulation.
	/// </summary>
	public int HistoryCapacity
	{
		get => _historyCapacity.Value;
		set
		{
			_historyCapacity.Value = value;
			InitializeHistoryBuffers(value);
		}
	}

	/// <summary>
	/// Main candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used to confirm fractals.
	/// </summary>
	public DataType HigherFractalType
	{
		get => _higherFractalType.Value;
		set => _higherFractalType.Value = value;
	}

	/// <summary>
	/// Timeframe used for RSI calculation.
	/// </summary>
	public DataType RsiType
	{
		get => _rsiType.Value;
		set => _rsiType.Value = value;
	}

	/// <summary>
	/// Timeframe used for EMA and PSAR filters.
	/// </summary>
	public DataType FilterType
	{
		get => _filterType.Value;
		set => _filterType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, CandleType),
		(Security, HigherFractalType),
		(Security, RsiType),
		(Security, FilterType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_mainFractals.Reset();
		_higherFractals.Reset();
		_rsi.Reset();
		_stochasticBuy.Reset();
		_stochasticSell.Reset();
		_maFilter.Reset();
		_psarFilter.Reset();

		_lastRsiValue = null;
		_lastStochBuy = null;
		_lastStochSell = null;
		_maPrevValue = null;
		_maSlope = null;
		_lastPsarValue = null;

		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		Array.Clear(_highHistory);
		Array.Clear(_lowHistory);
		Array.Clear(_closeHistory);
		Array.Clear(_openHistory);
		_historyCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeHistoryBuffers(HistoryCapacity);

		Volume = BaseVolume;

		_stochasticBuy.Length = Math.Max(1, StochasticPeriodBuy);
		_stochasticBuy.K.Length = 3;
		_stochasticBuy.D.Length = 3;

		_stochasticSell.Length = Math.Max(1, StochasticPeriodSell);
		_stochasticSell.K.Length = 3;
		_stochasticSell.D.Length = 3;

		_rsi.Length = 3;

		_maFilter = new ExponentialMovingAverage { Length = Math.Max(1, MaPeriod) };
		_psarFilter = new ParabolicSar
		{
			Acceleration = PsarStep,
			AccelerationMax = PsarMax
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.BindEx(_stochasticBuy, ProcessStochasticBuy)
		.BindEx(_stochasticSell, ProcessStochasticSell)
		.Bind(ProcessMainCandle)
		.Start();

		SubscribeCandles(HigherFractalType)
		.Bind(ProcessHigherFractal)
		.Start();

		SubscribeCandles(RsiType)
		.Bind(ProcessRsi)
		.Start();

		SubscribeCandles(FilterType)
		.Bind(_maFilter, _psarFilter, ProcessFilterCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
			{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _maFilter);
			DrawIndicator(area, _psarFilter);
			DrawIndicator(area, _stochasticBuy);
			DrawIndicator(area, _stochasticSell);
			DrawOwnTrades(area);
		}
	}

	private void ProcessStochasticBuy(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)value;
		if (stoch.K is not decimal k)
			return;

		_lastStochBuy = k;
	}

	private void ProcessStochasticSell(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)value;
		if (stoch.K is not decimal k)
			return;

		_lastStochSell = k;
	}

	private void ProcessHigherFractal(ICandleMessage candle)
	{
		_higherFractals.Update(candle);
	}

	private void ProcessRsi(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = _rsi.Process(new DecimalIndicatorValue(_rsi, candle.OpenPrice, candle.OpenTime));
		if (!value.IsFinal)
			return;

		_lastRsiValue = value.ToDecimal();
	}

	private void ProcessFilterCandle(ICandleMessage candle, decimal maValue, decimal psarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_maPrevValue is decimal previous)
			_maSlope = maValue - previous;
		else
			_maSlope = 0m;

		_maPrevValue = maValue;
		_lastPsarValue = psarValue;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_mainFractals.Update(candle);
		UpdateHistory(candle);

		ManageOpenPositions(candle);

		var signal = CalculatePrimarySignal();

		if (CloseOnOppositeSignal)
			{
			if (signal < 0 && Position > 0)
				{
				SellMarket(Position);
				ResetLongState();
			}
			else if (signal > 0 && Position < 0)
				{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
			}
		}

		if (Position != 0)
			return;

		if (signal > 0 && AreFiltersSatisfied(candle, 1))
			{
			var volume = GetVolume(candle.ClosePrice);
			if (volume > 0m)
				{
				InitializeLongLevels(candle.ClosePrice);
				BuyMarket(volume);
			}
		}
		else if (signal < 0 && AreFiltersSatisfied(candle, -1))
			{
			var volume = GetVolume(candle.ClosePrice);
			if (volume > 0m)
				{
				InitializeShortLevels(candle.ClosePrice);
				SellMarket(volume);
			}
		}
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (Position > 0)
			{
			ManageLongPosition(candle);
		}
		else if (Position < 0)
			{
			ManageShortPosition(candle);
		}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (_longEntryPrice is not decimal entryPrice)
			return;

		var step = GetPoint();
		var trailingDistance = TrailingStopPoints * step;

		if (UseTrailingStop && trailingDistance > 0m)
			{
			if (!ProfitTrailing || candle.ClosePrice - entryPrice > trailingDistance)
				{
				var candidate = candle.ClosePrice - trailingDistance;
				if (_longStopPrice is decimal currentStop)
					{
					if (candidate > currentStop + step)
						_longStopPrice = candidate;
				}
				else
					{
					_longStopPrice = candidate;
				}
			}
		}

		if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
			SellMarket(Position);
			ResetLongState();
			return;
		}

		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
			SellMarket(Position);
			ResetLongState();
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (_shortEntryPrice is not decimal entryPrice)
			return;

		var step = GetPoint();
		var trailingDistance = TrailingStopPoints * step;

		if (UseTrailingStop && trailingDistance > 0m)
			{
			if (!ProfitTrailing || entryPrice - candle.ClosePrice > trailingDistance)
				{
				var candidate = candle.ClosePrice + trailingDistance;
				if (_shortStopPrice is decimal currentStop)
					{
					if (candidate < currentStop - step)
						_shortStopPrice = candidate;
				}
				else
					{
					_shortStopPrice = candidate;
				}
			}
		}

		if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return;
		}

		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
		}
	}

	private int CalculatePrimarySignal()
	{
		var buyAllowed = AllowBuy;
		var sellAllowed = AllowSell;

		if (!buyAllowed && !sellAllowed)
			return 0;

		var buyFractals = true;
		var sellFractals = true;

		if (UseFractals)
			{
			var currentBuy = _mainFractals.GetSignal(Math.Max(0, FractalShiftBuyCurrent));
			var higherBuy = _higherFractals.GetSignal(Math.Max(0, FractalShiftBuyHigher));
			var currentSell = _mainFractals.GetSignal(Math.Max(0, FractalShiftSellCurrent));
			var higherSell = _higherFractals.GetSignal(Math.Max(0, FractalShiftSellHigher));

			buyFractals = currentBuy > 0 && higherBuy > 0;
			sellFractals = currentSell < 0 && higherSell < 0;
		}

		decimal rsiValue = 50m;
		if (UseRsi)
			{
			if (_lastRsiValue is not decimal value)
				return 0;

			rsiValue = value;
		}

		decimal stochBuy = 50m;
		decimal stochSell = 50m;

		if (UseStochastic)
			{
			if (_lastStochBuy is not decimal buyValue || _lastStochSell is not decimal sellValue)
				return 0;

			stochBuy = buyValue;
			stochSell = sellValue;
		}

		var buyRsi = !UseRsi || rsiValue < 50m - RsiLevelBuy;
		var sellRsi = !UseRsi || rsiValue > 50m + RsiLevelSell;

		var buyStoch = !UseStochastic || stochBuy < 50m - StochasticLevelBuy;
		var sellStoch = !UseStochastic || stochSell > 50m + StochasticLevelSell;

		if (buyAllowed && buyFractals && buyRsi && buyStoch)
			return 1;

		if (sellAllowed && sellFractals && sellRsi && sellStoch)
			return -1;

		return 0;
	}

	private bool AreFiltersSatisfied(ICandleMessage candle, int direction)
	{
		if (direction > 0)
			{
			if (UseMaFilter && _maSlope is not decimal slopePositive)
				return false;

			if (UseMaFilter && _maSlope <= 0m)
				return false;
		}
		else if (direction < 0)
			{
			if (UseMaFilter && _maSlope is not decimal slopeNegative)
				return false;

			if (UseMaFilter && _maSlope >= 0m)
				return false;
		}

		if (UsePsarFilter)
			{
			if (_lastPsarValue is not decimal psar)
				return false;

			if (direction > 0 && psar >= candle.OpenPrice)
				return false;

			if (direction < 0 && psar <= candle.OpenPrice)
				return false;
		}

		if (UseChannelFilter)
			{
			var channelSignal = GetChannelSignal();
			if (direction > 0 && channelSignal <= 0)
				return false;

			if (direction < 0 && channelSignal >= 0)
				return false;
		}

		if (UsePerceptronFilter)
			{
			var perceptron = GetPerceptronValue();
			if (direction > 0 && perceptron <= 0m)
				return false;

			if (direction < 0 && perceptron >= 0m)
				return false;
		}

		return true;
	}

	private decimal GetPoint()
	{
		return Security?.PriceStep ?? 0.0001m;
	}

	private decimal GetVolume(decimal price)
	{
		var volume = BaseVolume;

		if (AccountMicro)
			volume /= 10m;

		if (!UseMoneyManagement || price <= 0m || Portfolio is null)
			return Math.Max(volume, MinimumVolume);

		var capital = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
		if (capital <= 0m)
			return Math.Max(volume, MinimumVolume);

		var step = GetPoint();
		var stopDistance = StopLossPoints * step;
		var priceStepValue = Security?.StepPrice ?? step;

		if (stopDistance <= 0m || priceStepValue <= 0m)
			return Math.Max(volume, MinimumVolume);

		var riskAmount = capital * RiskPercent / 100m;
		var lossPerUnit = stopDistance / step * priceStepValue;

		if (lossPerUnit <= 0m)
			return Math.Max(volume, MinimumVolume);

		var managedVolume = riskAmount / lossPerUnit;

		if (AccountMicro)
			managedVolume /= 10m;

		return Math.Max(managedVolume, MinimumVolume);
	}

	private void InitializeLongLevels(decimal entryPrice)
	{
		var step = GetPoint();
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		_longEntryPrice = entryPrice;
		_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;
		_longTakePrice = takeDistance > 0m ? entryPrice + takeDistance : null;

		ResetShortState();
	}

	private void InitializeShortLevels(decimal entryPrice)
	{
		var step = GetPoint();
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		_shortEntryPrice = entryPrice;
		_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;
		_shortTakePrice = takeDistance > 0m ? entryPrice - takeDistance : null;

		ResetLongState();
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		var limit = Math.Min(_historyCount, HistoryCapacity - 1);

		for (var i = limit; i >= 1; i--)
		{
			_highHistory[i] = _highHistory[i - 1];
			_lowHistory[i] = _lowHistory[i - 1];
			_closeHistory[i] = _closeHistory[i - 1];
			_openHistory[i] = _openHistory[i - 1];
		}

		_highHistory[0] = candle.HighPrice;
		_lowHistory[0] = candle.LowPrice;
		_closeHistory[0] = candle.ClosePrice;
		_openHistory[0] = candle.OpenPrice;

		if (_historyCount < HistoryCapacity)
			_historyCount++;
	}

	private int GetChannelSignal()
	{
		var lookback = Math.Max(1, ChannelLookback);
		if (_historyCount <= lookback)
			return 0;

		var max = _highHistory[1];
		for (var i = 2; i <= lookback; i++)
		{
			if (_highHistory[i] > max)
				max = _highHistory[i];
		}

		var min = _lowHistory[1];
		for (var i = 2; i <= lookback; i++)
		{
			if (_lowHistory[i] < min)
				min = _lowHistory[i];
		}

		if (_historyCount <= 4)
			return 0;

		var range = max - min;
		var upper = max - ChannelK * range;
		var lower = min + ChannelK * range;

		var previousLow = _lowHistory[1];
		var previousHigh = _highHistory[1];

		if (previousLow > lower)
			return 1;

		if (previousHigh < upper)
			return -1;

		return 0;
	}

	private decimal GetPerceptronValue()
	{
		if (_historyCount <= 11)
			return 0m;

		var w1 = PerceptronV1 - 50m;
		var w2 = PerceptronV2 - 50m;
		var w3 = PerceptronV3 - 50m;
		var w4 = PerceptronV4 - 50m;

		var a1 = _highHistory[1] - _highHistory[7];
		var a2 = _highHistory[4] - _highHistory[11];
		var a3 = _lowHistory[1] - _lowHistory[7];
		var a4 = _lowHistory[4] - _lowHistory[11];

		return w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4;
	}

	private sealed class FractalSignalTracker
	{
		private readonly decimal[] _highs = new decimal[5];
		private readonly decimal[] _lows = new decimal[5];
		private readonly List<int> _signals = new();
		private int _count;

		public void Reset()
		{
			Array.Clear(_highs);
			Array.Clear(_lows);
			_signals.Clear();
			_count = 0;
		}

		public void Update(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			_signals.Add(0);

			if (_count < 5)
				{
				_highs[_count] = candle.HighPrice;
				_lows[_count] = candle.LowPrice;
				_count++;

				if (_count < 5)
					return;
			}
			else
				{
				for (var i = 0; i < 4; i++)
				{
					_highs[i] = _highs[i + 1];
					_lows[i] = _lows[i + 1];
				}

				_highs[4] = candle.HighPrice;
				_lows[4] = candle.LowPrice;
			}

			var index = _signals.Count - 3;
			if (index < 0 || index >= _signals.Count)
				return;

			var midHigh = _highs[2];
			var hasBearish = midHigh > _highs[0] && midHigh > _highs[1] && midHigh > _highs[3] && midHigh > _highs[4];

			var midLow = _lows[2];
			var hasBullish = midLow < _lows[0] && midLow < _lows[1] && midLow < _lows[3] && midLow < _lows[4];

			if (hasBullish && !hasBearish)
				_signals[index] = 1;
			else if (hasBearish && !hasBullish)
				_signals[index] = -1;
			else
				_signals[index] = 0;

			if (_signals.Count > 512)
				_signals.RemoveRange(0, _signals.Count - 512);
		}

		public int GetSignal(int shift)
		{
			var index = _signals.Count - 1 - shift;
			if (index < 0 || index >= _signals.Count)
				return 0;

			return _signals[index];
		}
	}

	private bool AllowBuy => _allowBuy.Value;
	private bool AllowSell => _allowSell.Value;
	private bool UseFractals => _useFractals.Value;
	private bool UseRsi => _useRsi.Value;
	private bool UseStochastic => _useStochastic.Value;
	private bool CloseOnOppositeSignal => _closeOnOppositeSignal.Value;
	private bool UseMaFilter => _useMaFilter.Value;
	private bool UsePsarFilter => _usePsarFilter.Value;
	private bool UseChannelFilter => _useChannelFilter.Value;
	private bool UsePerceptronFilter => _usePerceptronFilter.Value;
	private int FractalShiftBuyCurrent => _fractalShiftBuyCurrent.Value;
	private int FractalShiftBuyHigher => _fractalShiftBuyHigher.Value;
	private int FractalShiftSellCurrent => _fractalShiftSellCurrent.Value;
	private int FractalShiftSellHigher => _fractalShiftSellHigher.Value;
	private int RsiLevelBuy => _rsiLevelBuy.Value;
	private int RsiLevelSell => _rsiLevelSell.Value;
	private int StochasticPeriodBuy => _stochasticPeriodBuy.Value;
	private int StochasticPeriodSell => _stochasticPeriodSell.Value;
	private int StochasticLevelBuy => _stochasticLevelBuy.Value;
	private int StochasticLevelSell => _stochasticLevelSell.Value;
	private int MaPeriod => _maPeriod.Value;
	private decimal PsarStep => _psarStep.Value;
	private decimal PsarMax => _psarMax.Value;
	private int ChannelLookback => _channelLookback.Value;
	private decimal ChannelK => _channelK.Value;
	private decimal PerceptronV1 => _perceptronV1.Value;
	private decimal PerceptronV2 => _perceptronV2.Value;
	private decimal PerceptronV3 => _perceptronV3.Value;
	private decimal PerceptronV4 => _perceptronV4.Value;
}
