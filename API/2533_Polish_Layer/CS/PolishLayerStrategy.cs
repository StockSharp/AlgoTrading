namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Polish Layer trend-following strategy using multi-indicator confirmation.
/// </summary>
public class PolishLayerStrategy : Strategy
{
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _longEmaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _shortEma = null!;
	private ExponentialMovingAverage _longEma = null!;
	private RelativeStrengthIndex _rsi = null!;
	private StochasticOscillator _stochastic = null!;
	private WilliamsR _williamsR = null!;
	private DeMarker _deMarker = null!;

	private decimal? _prevShortEma;
	private decimal? _prevLongEma;
	private decimal? _prevRsi;
	private decimal? _prevPrevRsi;
	private decimal? _prevStochK;
	private decimal? _prevWilliamsR;
	private decimal? _prevDeMarker;

	private decimal? _currentShortEma;
	private decimal? _currentLongEma;
	private decimal? _currentRsi;
	private decimal? _currentStochK;
	private decimal? _currentWilliamsR;
	private decimal? _currentDeMarker;

	private DateTimeOffset? _lastIndicatorsTime;
	private DateTimeOffset? _lastStochasticTime;
	private DateTimeOffset? _lastProcessedTime;

	/// <summary>
	/// Short exponential moving average period.
	/// </summary>
	public int ShortEmaPeriod
	{
		get => _shortEmaPeriod.Value;
		set => _shortEmaPeriod.Value = value;
	}

	/// <summary>
	/// Long exponential moving average period.
	/// </summary>
	public int LongEmaPeriod
	{
		get => _longEmaPeriod.Value;
		set => _longEmaPeriod.Value = value;
	}

	/// <summary>
	/// Relative Strength Index period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing period.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
	}

	/// <summary>
	/// DeMarker period.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PolishLayerStrategy"/>.
	/// </summary>
	public PolishLayerStrategy()
	{
		_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Fast EMA period", "Trend")
			.SetCanOptimize(true);

		_longEmaPeriod = Param(nameof(LongEmaPeriod), 45)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Slow EMA period", "Trend")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Oscillators")
			.SetCanOptimize(true);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Main stochastic period", "Oscillators")
			.SetCanOptimize(true);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Signal line period", "Oscillators")
			.SetCanOptimize(true);

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Slowing", "Final smoothing", "Oscillators")
			.SetCanOptimize(true);

		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R", "Williams %R lookback", "Oscillators")
			.SetCanOptimize(true);

		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker", "DeMarker lookback", "Oscillators")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 17)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Target distance in points", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 77)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Protective distance in points", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		Volume = 1;
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

		_shortEma = null!;
		_longEma = null!;
		_rsi = null!;
		_stochastic = null!;
		_williamsR = null!;
		_deMarker = null!;

		_prevShortEma = null;
		_prevLongEma = null;
		_prevRsi = null;
		_prevPrevRsi = null;
		_prevStochK = null;
		_prevWilliamsR = null;
		_prevDeMarker = null;

		_currentShortEma = null;
		_currentLongEma = null;
		_currentRsi = null;
		_currentStochK = null;
		_currentWilliamsR = null;
		_currentDeMarker = null;

		_lastIndicatorsTime = null;
		_lastStochasticTime = null;
		_lastProcessedTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize primary trend and oscillator indicators.
		_shortEma = new ExponentialMovingAverage { Length = ShortEmaPeriod };
		_longEma = new ExponentialMovingAverage { Length = LongEmaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_stochastic = new StochasticOscillator
		{
			K = { Length = StochasticKPeriod },
			D = { Length = StochasticDPeriod },
			Slowing = StochasticSlowing,
		};
		_williamsR = new WilliamsR { Length = WilliamsRPeriod };
		_deMarker = new DeMarker { Length = DeMarkerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shortEma, _longEma, _rsi, _williamsR, _deMarker, ProcessMainIndicators)
			.BindEx(_stochastic, ProcessStochastic)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _shortEma);
			DrawIndicator(area, _longEma);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);

			var oscillatorArea = CreateChartArea();
			if (oscillatorArea != null)
			{
				DrawIndicator(oscillatorArea, _stochastic);
				DrawIndicator(oscillatorArea, _williamsR);
				DrawIndicator(oscillatorArea, _deMarker);
			}
		}

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		// Enable automatic stop-loss and take-profit protection.
		StartProtection(
			takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossPoints * step, UnitTypes.Point));
	}

	private void ProcessMainIndicators(
		ICandleMessage candle,
		decimal shortEma,
		decimal longEma,
		decimal rsi,
		decimal williamsR,
		decimal deMarker)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store current indicator values for synchronized processing.
		_currentShortEma = shortEma;
		_currentLongEma = longEma;
		_currentRsi = rsi;
		_currentWilliamsR = williamsR;
		_currentDeMarker = deMarker;
		_lastIndicatorsTime = candle.OpenTime;

		TryProcessSignalAndUpdate(candle);
	}

	private void ProcessStochastic(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochasticValue;
		if (stoch.K is not decimal kValue)
			return;

		_currentStochK = kValue;
		_lastStochasticTime = candle.OpenTime;

		TryProcessSignalAndUpdate(candle);
	}

	private void TryProcessSignalAndUpdate(ICandleMessage candle)
	{
		if (_lastIndicatorsTime != candle.OpenTime || _lastStochasticTime != candle.OpenTime)
			return;

		if (_lastProcessedTime == candle.OpenTime)
			return;

		if (!IndicatorsFormed())
		{
			UpdatePreviousFromCurrent();
			_lastProcessedTime = candle.OpenTime;
			return;
		}

		ExecuteTradingLogic(candle);
		UpdatePreviousFromCurrent();
		_lastProcessedTime = candle.OpenTime;
	}

	private bool IndicatorsFormed()
	{
		return _shortEma.IsFormed &&
			_longEma.IsFormed &&
			_rsi.IsFormed &&
			_stochastic.IsFormed &&
			_williamsR.IsFormed &&
			_deMarker.IsFormed;
	}

	private void ExecuteTradingLogic(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevShortEma is not decimal prevShort ||
			_prevLongEma is not decimal prevLong ||
			_prevRsi is not decimal prevRsi ||
			_prevPrevRsi is not decimal prevPrevRsi ||
			_prevStochK is not decimal prevStoch ||
			_prevWilliamsR is not decimal prevWilliams ||
			_prevDeMarker is not decimal prevDeMarker ||
			_currentStochK is not decimal currentStoch ||
			_currentWilliamsR is not decimal currentWilliams ||
			_currentDeMarker is not decimal currentDeMarker)
		{
			return;
		}

		// Determine directional bias using previous EMA and RSI values.
		var longTrend = prevShort > prevLong && prevRsi > prevPrevRsi;
		var shortTrend = prevShort < prevLong && prevRsi < prevPrevRsi;

		if (!longTrend && !shortTrend)
			return;

		// Confirm entries with oscillator crossovers.
		var stochCrossUp = prevStoch < 19m && currentStoch >= 19m;
		var stochCrossDown = prevStoch > 81m && currentStoch <= 81m;

		var deMarkerCrossUp = prevDeMarker < 0.35m && currentDeMarker >= 0.35m;
		var deMarkerCrossDown = prevDeMarker > 0.63m && currentDeMarker <= 0.63m;

		var williamsCrossUp = prevWilliams < -81m && currentWilliams >= -81m;
		var williamsCrossDown = prevWilliams > -19m && currentWilliams <= -19m;

		if (longTrend && stochCrossUp && deMarkerCrossUp && williamsCrossUp && Position == 0m)
		{
			// Enter long position only when no trades are open.
			BuyMarket(Volume);
		}
		else if (shortTrend && stochCrossDown && deMarkerCrossDown && williamsCrossDown && Position == 0m)
		{
			// Enter short position only when flat to mirror the original EA behaviour.
			SellMarket(Volume);
		}
	}

	private void UpdatePreviousFromCurrent()
	{
		if (_currentShortEma is decimal currentShort)
			_prevShortEma = currentShort;

		if (_currentLongEma is decimal currentLong)
			_prevLongEma = currentLong;

		if (_currentRsi is decimal currentRsi)
		{
			_prevPrevRsi = _prevRsi;
			_prevRsi = currentRsi;
		}

		if (_currentStochK is decimal currentStoch)
			_prevStochK = currentStoch;

		if (_currentWilliamsR is decimal currentWilliams)
			_prevWilliamsR = currentWilliams;

		if (_currentDeMarker is decimal currentDeMarker)
			_prevDeMarker = currentDeMarker;
	}
}
