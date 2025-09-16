namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;

/// <summary>
/// Strategy that extrapolates future prices using the Burg autoregressive model and opens trades when forecasted swings exceed thresholds.
/// Converted from the MetaTrader Burg Extrapolator expert.
/// </summary>
public class BurgExtrapolatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _minProfitPips;
	private readonly StrategyParam<decimal> _maxLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _pastBars;
	private readonly StrategyParam<decimal> _modelOrderFraction;
	private readonly StrategyParam<bool> _useMomentum;
	private readonly StrategyParam<bool> _useRateOfChange;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _openHistory = Array.Empty<decimal>();
	private decimal[] _inputSeries = Array.Empty<decimal>();
	private double[] _inputBuffer = Array.Empty<double>();
	private double[] _coefficients = Array.Empty<double>();
	private double[] _predictions = Array.Empty<double>();
	private double[] _forwardErrors = Array.Empty<double>();
	private double[] _backwardErrors = Array.Empty<double>();
	private decimal[] _priceForecast = Array.Empty<decimal>();

	private int _historyCapacity;
	private int _openCount;
	private int _modelOrder;
	private int _forecastSteps;
	private int _effectivePastBars;
	private decimal _pipSize;

	/// <summary>
	/// Risk percent of equity per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous positions in the same direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Minimum predicted profit in pips required to open a position.
	/// </summary>
	public decimal MinProfitPips
	{
		get => _minProfitPips.Value;
		set => _minProfitPips.Value = value;
	}

	/// <summary>
	/// Maximum tolerated loss in pips that triggers position close.
	/// </summary>
	public decimal MaxLossPips
	{
		get => _maxLossPips.Value;
		set => _maxLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
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
	/// Number of past bars used for the Burg model input.
	/// </summary>
	public int PastBars
	{
		get => _pastBars.Value;
		set => _pastBars.Value = value;
	}

	/// <summary>
	/// Fraction of past bars that determines the autoregressive order.
	/// </summary>
	public decimal ModelOrderFraction
	{
		get => _modelOrderFraction.Value;
		set => _modelOrderFraction.Value = value;
	}

	/// <summary>
	/// Enables logarithmic momentum input instead of raw prices.
	/// </summary>
	public bool UseMomentum
	{
		get => _useMomentum.Value;
		set => _useMomentum.Value = value;
	}

	/// <summary>
	/// Enables rate of change input when momentum is disabled.
	/// </summary>
	public bool UseRateOfChange
	{
		get => _useRateOfChange.Value;
		set => _useRateOfChange.Value = value;
	}

	/// <summary>
	/// Base order volume when risk calculation is not available.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="BurgExtrapolatorStrategy"/>.
	/// </summary>
	public BurgExtrapolatorStrategy()
	{
		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetDisplay("Risk %", "Risk percent per trade", "Money")
		.SetGreaterThanOrEqual(0m);

		_maxPositions = Param(nameof(MaxPositions), 1)
		.SetDisplay("Max Positions", "Maximum simultaneous trades", "Risk")
		.SetGreaterThanZero();

		_minProfitPips = Param(nameof(MinProfitPips), 160m)
		.SetDisplay("Min Profit", "Minimum predicted profit (pips)", "Signals")
		.SetGreaterThanOrEqual(0m);

		_maxLossPips = Param(nameof(MaxLossPips), 130m)
		.SetDisplay("Max Loss", "Maximum tolerated loss (pips)", "Risk")
		.SetGreaterThanOrEqual(0m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
		.SetDisplay("Take Profit", "Take profit distance (pips)", "Risk")
		.SetGreaterThanOrEqual(0m);

		_stopLossPips = Param(nameof(StopLossPips), 180m)
		.SetDisplay("Stop Loss", "Stop loss distance (pips)", "Risk")
		.SetGreaterThanOrEqual(0m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
		.SetDisplay("Trailing Stop", "Trailing stop distance (pips)", "Risk")
		.SetGreaterThanOrEqual(0m);

		_pastBars = Param(nameof(PastBars), 200)
		.SetDisplay("Past Bars", "Bars used for Burg model", "Model")
		.SetGreaterThanZero();

		_modelOrderFraction = Param(nameof(ModelOrderFraction), 0.37m)
		.SetDisplay("Model Order", "Fraction of bars used for AR order", "Model")
		.SetRange(0.1m, 0.9m);

		_useMomentum = Param(nameof(UseMomentum), true)
		.SetDisplay("Use Momentum", "Use logarithmic momentum input", "Model");

		_useRateOfChange = Param(nameof(UseRateOfChange), false)
		.SetDisplay("Use ROC", "Use rate of change input when momentum is off", "Model");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Fallback order volume", "Money")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		ResetBuffers();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		if (decimals is 3 or 5)
		_pipSize *= 10m;

		EnsureCapacity();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
		takeProfit: TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null,
		stopLoss: StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null,
		isStopTrailing: TrailingStopPips > 0m,
		trailingStop: TrailingStopPips > 0m ? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute) : null);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		EnsureCapacity();

		PushOpen(candle.OpenPrice);

		if (_openCount < _historyCapacity)
		return;

		var currentOpen = _openHistory[_openCount - 1];
		if (!TryBuildInputSeries(out var average))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!TryCalculateSignals(average, currentOpen, out var openSignal, out var closeSignal))
		return;

		var hasPosition = Position != 0m;
		if (hasPosition)
		{
			if (Position > 0m && (closeSignal == -1 || openSignal == -1))
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (Position < 0m && (closeSignal == 1 || openSignal == 1))
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}

		if (openSignal == 0)
		return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		return;

		var maxExposure = MaxPositions * volume;

		if (openSignal > 0)
		{
			if (Position < maxExposure)
			{
				var remaining = maxExposure - Math.Max(Position, 0m);
				var tradeVolume = Math.Min(volume, remaining);
				if (tradeVolume > 0m)
				BuyMarket(tradeVolume);
			}
		}
		else if (openSignal < 0)
		{
			var shortExposure = Math.Abs(Math.Min(Position, 0m));
			if (shortExposure < maxExposure)
			{
				var remaining = maxExposure - shortExposure;
				var tradeVolume = Math.Min(volume, remaining);
				if (tradeVolume > 0m)
				SellMarket(tradeVolume);
			}
		}
	}

	private void ResetBuffers()
	{
		_openHistory = Array.Empty<decimal>();
		_inputSeries = Array.Empty<decimal>();
		_inputBuffer = Array.Empty<double>();
		_coefficients = Array.Empty<double>();
		_predictions = Array.Empty<double>();
		_forwardErrors = Array.Empty<double>();
		_backwardErrors = Array.Empty<double>();
		_priceForecast = Array.Empty<decimal>();
		_historyCapacity = 0;
		_openCount = 0;
		_modelOrder = 1;
		_forecastSteps = 1;
		_effectivePastBars = 0;
	}

	private void EnsureCapacity()
	{
		var bars = Math.Max(PastBars, 3);
		var momentumEnabled = UseMomentum;
		var rocEnabled = !momentumEnabled && UseRateOfChange;
		var requiredHistory = momentumEnabled || rocEnabled ? bars + 1 : bars;

		if (_effectivePastBars != bars)
		{
			_effectivePastBars = bars;
			_inputSeries = new decimal[bars];
			_inputBuffer = new double[bars];
			_forwardErrors = new double[bars];
			_backwardErrors = new double[bars];
			_openHistory = new decimal[requiredHistory];
			_historyCapacity = requiredHistory;
			_openCount = 0;
		}
		else if (_historyCapacity != requiredHistory)
		{
			_openHistory = new decimal[requiredHistory];
			_historyCapacity = requiredHistory;
			_openCount = 0;
		}

		var order = (int)Math.Floor((double)(ModelOrderFraction * bars));
		if (order < 1)
		order = 1;
		if (order >= bars)
		order = bars - 1;
		if (order < 1)
		order = 1;

		var nf = bars - order - 1;
		if (nf < 1)
		nf = 1;

		if (_coefficients.Length != order + 1)
		_coefficients = new double[order + 1];

		if (_predictions.Length != nf + 1)
		_predictions = new double[nf + 1];

		if (_priceForecast.Length != nf + 1)
		_priceForecast = new decimal[nf + 1];

		_modelOrder = order;
		_forecastSteps = nf;
	}

	private void PushOpen(decimal open)
	{
		if (_historyCapacity == 0)
		return;

		if (_openCount < _historyCapacity)
		{
			_openHistory[_openCount++] = open;
		}
		else
		{
			Array.Copy(_openHistory, 1, _openHistory, 0, _historyCapacity - 1);
			_openHistory[_historyCapacity - 1] = open;
		}
	}

	private bool TryBuildInputSeries(out decimal average)
	{
		average = 0m;
		var bars = _effectivePastBars;
		if (bars == 0 || _openCount < _historyCapacity)
		return false;

		var momentumEnabled = UseMomentum;
		var rocEnabled = !momentumEnabled && UseRateOfChange;

		if (momentumEnabled)
		{
			for (var i = 0; i < bars; i++)
			{
				var prev = _openHistory[i];
				var next = _openHistory[i + 1];
				if (prev <= 0m || next <= 0m)
				{
					_inputSeries[i] = 0m;
				}
				else
				{
					var ratio = next / prev;
					_inputSeries[i] = (decimal)Math.Log((double)ratio);
				}
			}
		}
		else if (rocEnabled)
		{
			for (var i = 0; i < bars; i++)
			{
				var prev = _openHistory[i];
				var next = _openHistory[i + 1];
				if (prev == 0m)
				{
					_inputSeries[i] = 0m;
				}
				else
				{
					_inputSeries[i] = next / prev - 1m;
				}
			}
		}
		else
		{
			for (var i = 0; i < bars; i++)
			average += _openHistory[i];
			average /= bars;

			for (var i = 0; i < bars; i++)
			_inputSeries[i] = _openHistory[i] - average;
		}

		for (var i = 0; i < bars; i++)
		_inputBuffer[i] = (double)_inputSeries[i];

		return true;
	}

	private bool TryCalculateSignals(decimal average, decimal currentOpen, out int openSignal, out int closeSignal)
	{
		openSignal = 0;
		closeSignal = 0;

		var bars = _effectivePastBars;
		if (bars == 0 || _modelOrder < 1 || _forecastSteps < 1)
		return false;

		Array.Clear(_coefficients, 0, _coefficients.Length);
		Array.Clear(_predictions, 0, _predictions.Length);
		Array.Copy(_inputBuffer, _forwardErrors, bars);
		Array.Copy(_inputBuffer, _backwardErrors, bars);

		ComputeBurgCoefficients(bars);
		ForecastSeries(bars);

		var momentumEnabled = UseMomentum;
		var rocEnabled = !momentumEnabled && UseRateOfChange;

		if (momentumEnabled)
		{
			_priceForecast[0] = currentOpen;
			for (var i = 1; i <= _forecastSteps; i++)
			{
				var prev = _priceForecast[i - 1];
				var next = prev * (decimal)Math.Exp(_predictions[i]);
				_priceForecast[i] = next;
			}
		}
		else if (rocEnabled)
		{
			_priceForecast[0] = currentOpen;
			for (var i = 1; i <= _forecastSteps; i++)
			{
				var prev = _priceForecast[i - 1];
				_priceForecast[i] = prev * (1m + (decimal)_predictions[i]);
			}
		}
		else
		{
			for (var i = 0; i <= _forecastSteps; i++)
			_priceForecast[i] = (decimal)_predictions[i] + average;
		}

		var minProfit = MinProfitPips * _pipSize;
		var maxLoss = MaxLossPips * _pipSize;
		var ymax = _priceForecast[0];
		var ymin = _priceForecast[0];
		var imax = 0;
		var imin = 0;

		for (var i = 1; i < _forecastSteps; i++)
		{
			var value = _priceForecast[i];

			if (value > ymax && openSignal == 0)
			{
				ymax = value;
				imax = i;

				if (imin == 0 && ymax - ymin >= maxLoss)
				closeSignal = 1;

				if (imin == 0 && ymax - ymin >= minProfit)
				openSignal = 1;
			}

			if (value < ymin && openSignal == 0)
			{
				ymin = value;
				imin = i;

				if (imax == 0 && ymax - ymin >= maxLoss)
				closeSignal = -1;

				if (imax == 0 && ymax - ymin >= minProfit)
				openSignal = -1;
			}
		}

		return true;
	}

	private void ComputeBurgCoefficients(int bars)
	{
		var den = 0.0;
		for (var i = 0; i < bars; i++)
		{
			den += _inputBuffer[i] * _inputBuffer[i];
		}
		den *= 2.0;

		var reflection = 0.0;

		for (var k = 1; k <= _modelOrder; k++)
		{
			double num = 0.0;
			for (var i = k; i < bars; i++)
			{
				num += _forwardErrors[i] * _backwardErrors[i - 1];
			}

			var left = _forwardErrors[k - 1];
			var right = _backwardErrors[bars - 1];
			var denom = (1.0 - reflection * reflection) * den - left * left - right * right;
			reflection = Math.Abs(denom) > double.Epsilon ? -2.0 * num / denom : 0.0;

			_coefficients[k] = reflection;
			var half = k / 2;
			for (var i = 1; i <= half; i++)
			{
				var ki = k - i;
				var temp = _coefficients[i];
				_coefficients[i] += reflection * _coefficients[ki];
				if (i != ki)
				{
					_coefficients[ki] += reflection * temp;
				}
			}

			if (k < _modelOrder)
			{
				for (var i = bars - 1; i >= k; i--)
				{
					var temp = _forwardErrors[i];
					_forwardErrors[i] += reflection * _backwardErrors[i - 1];
					_backwardErrors[i] = _backwardErrors[i - 1] + reflection * temp;
				}
			}
		}
	}

	private void ForecastSeries(int bars)
	{
		for (var n = bars - 1; n < bars + _forecastSteps; n++)
		{
			double sum = 0.0;
			for (var i = 1; i <= _modelOrder; i++)
			{
				var index = n - i;
				if (index < bars)
				{
					sum -= _coefficients[i] * _inputBuffer[index];
				}
				else
				{
					var pfIndex = index - bars + 1;
					if (pfIndex >= 0 && pfIndex < _predictions.Length)
					{
						sum -= _coefficients[i] * _predictions[pfIndex];
					}
				}
			}

			var targetIndex = n - bars + 1;
			if (targetIndex >= 0 && targetIndex < _predictions.Length)
			{
				_predictions[targetIndex] = sum;
			}
		}
	}

	private decimal CalculateOrderVolume()
	{
		if (StopLossPips <= 0m || RiskPercent <= 0m)
		{
			return OrderVolume;
		}

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
		{
			return OrderVolume;
		}

		var riskAmount = equity * RiskPercent / 100m;
		var stopDistance = StopLossPips * _pipSize;
		if (stopDistance <= 0m)
		{
			return OrderVolume;
		}

		var volume = riskAmount / stopDistance;
		return volume > 0m ? volume : OrderVolume;
	}
}
