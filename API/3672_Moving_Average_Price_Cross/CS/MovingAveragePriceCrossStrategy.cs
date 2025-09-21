namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Simple moving average price crossover strategy converted from the MT5 "MovingAverage" expert advisor.
/// Enters long positions when the candle close moves above the moving average and short positions when it falls below.
/// Uses MetaTrader-style point distances for stop-loss and take-profit levels.
/// </summary>
public class MovingAveragePriceCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SMA? _sma;
	private decimal? _previousClose;
	private decimal? _previousMa;
	private decimal? _currentClose;
	private decimal? _currentMa;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAveragePriceCrossStrategy"/> class.
	/// </summary>
	public MovingAveragePriceCrossStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA period", "Length of the simple moving average used for entries.", "Indicator")
			.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Volume submitted with each market order.", "Trading")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150)
			.SetNotNegative()
			.SetDisplay("Take profit (points)", "Distance to the profit target expressed in MetaTrader points.", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 150)
			.SetNotNegative()
			.SetDisplay("Stop loss (points)", "Distance to the protective stop expressed in MetaTrader points.", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General");
	}

	/// <summary>
	/// Moving average period used for generating signals.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Volume submitted with each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Profit target distance expressed in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type used to read market data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_sma = null;
		_previousClose = null;
		_previousMa = null;
		_currentClose = null;
		_currentMa = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		// Align the default strategy volume to the instrument settings.
		Volume = NormalizeVolume(OrderVolume);

		// Configure automatic protective orders using MetaTrader point distances.
		StartProtection(
			stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints * _pipSize, UnitTypes.Absolute) : null,
			takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * _pipSize, UnitTypes.Absolute) : null);

		_sma = new SMA { Length = MaPeriod };

		// Subscribe to candle updates and bind the moving average indicator.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		// Operate only on completed candles to mirror the MT5 implementation.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is fully initialized and allowed to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Cache the latest candle data for crossover checks.
		if (_currentClose is null)
		{
			_currentClose = candle.ClosePrice;
			_currentMa = maValue;
			return;
		}

		if (_previousClose is null)
		{
			_previousClose = _currentClose;
			_previousMa = _currentMa;
			_currentClose = candle.ClosePrice;
			_currentMa = maValue;
			return;
		}

		if (_sma?.IsFormed != true)
		{
			_previousClose = _currentClose;
			_previousMa = _currentMa;
			_currentClose = candle.ClosePrice;
			_currentMa = maValue;
			return;
		}

		var previousClose = _previousClose.Value;
		var previousMa = _previousMa!.Value;
		var currentClose = _currentClose.Value;
		var currentMa = _currentMa!.Value;

		var crossedBelowPrice = previousMa < previousClose && currentMa > currentClose;
		var crossedAbovePrice = previousMa > previousClose && currentMa < currentClose;

		if (Position == 0m)
		{
			var volume = NormalizeVolume(OrderVolume);

			// Open a short position when the moving average crosses above price.
			if (crossedBelowPrice && volume > 0m)
			{
				SellMarket(volume);
			}
			// Open a long position when the moving average crosses below price.
			else if (crossedAbovePrice && volume > 0m)
			{
				BuyMarket(volume);
			}
		}

		// Shift cached values to include the new candle for the next iteration.
		_previousClose = currentClose;
		_previousMa = currentMa;
		_currentClose = candle.ClosePrice;
		_currentMa = maValue;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var minVolume = Security?.MinVolume ?? step;
		if (volume < minVolume)
			volume = minVolume;

		var multiplier = volume / step;
		var rounded = Math.Round(multiplier, MidpointRounding.AwayFromZero) * step;

		if (rounded < minVolume)
			rounded = minVolume;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume is decimal max && rounded > max)
			rounded = max;

		return Math.Max(rounded, step);
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
			step *= 10m;

		return step;
	}
}
