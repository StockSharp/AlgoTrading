using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Order escort strategy that trails stop-loss and optionally take-profit in steps.
/// Manages existing positions by advancing protective levels after each finished bar.
/// </summary>
public class OrderEscortStrategy : Strategy
{
	private readonly StrategyParam<decimal> _targetBar;
	private readonly StrategyParam<decimal> _deltaPoints;
	private readonly StrategyParam<TrailingType> _trailingMode;
	private readonly StrategyParam<decimal> _exponent;
	private readonly StrategyParam<decimal> _eBase;
	private readonly StrategyParam<bool> _escortTakeProfit;
	private readonly StrategyParam<int> _closeBar;
	private readonly StrategyParam<DataType> _candleType;

	private int _barCounter;
	private decimal? _referenceStop;
	private decimal? _referenceTake;
	private decimal _currentStop;
	private decimal _currentTake;
	private bool _hasReferenceLevels;
	private decimal _priceStep;
	private decimal _linearCoefficient;
	private decimal _parabolicCoefficient;
	private decimal _exponentialCoefficient;

	/// <summary>
	/// Target number of bars for the full trailing distance.
	/// </summary>
	public decimal TargetBar
	{
		get => _targetBar.Value;
		set => _targetBar.Value = value;
	}

	/// <summary>
	/// Total trailing distance in points that will be reached after TargetBar bars.
	/// </summary>
	public decimal DeltaPoints
	{
		get => _deltaPoints.Value;
		set => _deltaPoints.Value = value;
	}

	/// <summary>
	/// Trailing mode selection.
	/// </summary>
	public TrailingType TrailingMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	/// <summary>
	/// Exponent used for parabolic trailing.
	/// </summary>
	public decimal Exponent
	{
		get => _exponent.Value;
		set => _exponent.Value = value;
	}

	/// <summary>
	/// Base of the exponential function.
	/// </summary>
	public decimal EBase
	{
		get => _eBase.Value;
		set => _eBase.Value = value;
	}

	/// <summary>
	/// Enables escorting of the take-profit together with the stop-loss.
	/// </summary>
	public bool EscortTakeProfit
	{
		get => _escortTakeProfit.Value;
		set => _escortTakeProfit.Value = value;
	}

	/// <summary>
	/// Bars count after which the position is forcefully closed.
	/// </summary>
	public int CloseBar
	{
		get => _closeBar.Value;
		set => _closeBar.Value = value;
	}

	/// <summary>
	/// Candle type for new bar detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public OrderEscortStrategy()
	{
		_targetBar = Param(nameof(TargetBar), 35m)
			.SetGreaterThanZero()
			.SetDisplay("Target Bar", "Bars required to reach full trailing distance", "Trailing")
			.SetCanOptimize(true)
			.SetOptimize(10m, 80m, 5m);

		_deltaPoints = Param(nameof(DeltaPoints), 80m)
			.SetGreaterThanZero()
			.SetDisplay("Delta Points", "Total trailing distance in points", "Trailing")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 10m);

		_trailingMode = Param(nameof(TrailingMode), TrailingType.Linear)
			.SetDisplay("Trailing Mode", "Shape of the trailing curve", "Trailing");

		_exponent = Param(nameof(Exponent), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Exponent", "Exponent for parabolic mode", "Trailing");

		_eBase = Param(nameof(EBase), 2.718m)
			.SetGreaterThanZero()
			.SetDisplay("E Base", "Base of exponential progression", "Trailing");

		_escortTakeProfit = Param(nameof(EscortTakeProfit), true)
			.SetDisplay("Escort Take Profit", "Move take-profit together with stop-loss", "Risk");

		_closeBar = Param(nameof(CloseBar), 15)
			.SetGreaterThanZero()
			.SetDisplay("Close Bar", "Bars until position is forcefully closed", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used to detect new bars", "General");
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

		_barCounter = 0;
		_referenceStop = null;
		_referenceTake = null;
		_currentStop = 0m;
		_currentTake = 0m;
		_hasReferenceLevels = false;
		_priceStep = 0m;
		_linearCoefficient = 0m;
		_parabolicCoefficient = 0m;
		_exponentialCoefficient = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		RecalculateCoefficients();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCounter++;

		var trailingPoints = CalculateTrailingPoints(_barCounter);

		if (Position != 0)
		{
			if (!_hasReferenceLevels)
			{
				CaptureInitialLevels(candle);
			}
			else
			{
				UpdateTrailingLevels(trailingPoints, candle);
			}
		}
		else
		{
			_hasReferenceLevels = false;
		}

		if (CloseBar > 0 && _barCounter == CloseBar && Position != 0)
		{
			// Close the position when the maximum bar count is reached.
			ClosePosition();
			_hasReferenceLevels = false;
		}
	}

	private void CaptureInitialLevels(ICandleMessage candle)
	{
		// Use the first finished bar after position entry to store baseline levels.
		var referencePrice = candle.ClosePrice;
		var delta = DeltaPoints * _priceStep;

		if (Position > 0)
		{
			_referenceStop = referencePrice - delta;
			_referenceTake = referencePrice + delta;
		}
		else
		{
			_referenceStop = referencePrice + delta;
			_referenceTake = referencePrice - delta;
		}

		_currentStop = _referenceStop ?? referencePrice;
		_currentTake = _referenceTake ?? referencePrice;
		_hasReferenceLevels = true;
	}

	private void UpdateTrailingLevels(decimal trailingPoints, ICandleMessage candle)
	{
		// Convert trailing from points to price units.
		var offset = trailingPoints * _priceStep;

		decimal? newStop = null;
		decimal? newTake = null;

		if (Position > 0)
		{
			if (_referenceStop.HasValue)
				newStop = _referenceStop.Value + offset;

			if (EscortTakeProfit && _referenceTake.HasValue)
				newTake = _referenceTake.Value + offset;
		}
		else if (Position < 0)
		{
			if (_referenceStop.HasValue)
				newStop = _referenceStop.Value - offset;

			if (EscortTakeProfit && _referenceTake.HasValue)
				newTake = _referenceTake.Value - offset;
		}

		if (newStop.HasValue)
		{
			_currentStop = newStop.Value;

			// Close long if price touches the trailed stop or short if price rises above it.
			if ((Position > 0 && candle.LowPrice <= _currentStop) ||
				(Position < 0 && candle.HighPrice >= _currentStop))
			{
				ClosePosition();
				_hasReferenceLevels = false;
				return;
			}
		}

		if (newTake.HasValue)
		{
			_currentTake = newTake.Value;

			// Exit early when the escorted take-profit is reached.
			if ((Position > 0 && candle.HighPrice >= _currentTake) ||
				(Position < 0 && candle.LowPrice <= _currentTake))
			{
				ClosePosition();
				_hasReferenceLevels = false;
			}
		}
	}

	private decimal CalculateTrailingPoints(int barNumber)
	{
		return TrailingMode switch
		{
			TrailingType.Linear => _linearCoefficient * barNumber,
			TrailingType.Parabolic => _parabolicCoefficient * (decimal)Math.Pow(barNumber, (double)Exponent),
			TrailingType.Exponential => _exponentialCoefficient * (decimal)Math.Pow((double)EBase, barNumber),
			_ => 0m,
		};
	}

	private void RecalculateCoefficients()
	{
		var target = TargetBar;
		var delta = DeltaPoints;
		var exponent = (double)Exponent;
		var eBase = (double)EBase;
		var targetDouble = (double)target;

		_linearCoefficient = target > 0m ? delta / target : 0m;
		_parabolicCoefficient = target > 0m
			? delta / (decimal)Math.Pow(targetDouble, exponent)
			: 0m;
		_exponentialCoefficient = delta / (decimal)Math.Pow(eBase, targetDouble);
	}
}

/// <summary>
/// Available trailing curve types.
/// </summary>
public enum TrailingType
{
	/// <summary>
	/// Trailing grows linearly with each bar.
	/// </summary>
	Linear,

	/// <summary>
	/// Trailing grows following a power curve.
	/// </summary>
	Parabolic,

	/// <summary>
	/// Trailing grows exponentially.
	/// </summary>
	Exponential,
}
