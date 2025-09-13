using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid averaging strategy based on the Ilan 1.6 Dynamic expert advisor.
/// Adds positions when price moves against the current one and closes the
/// whole basket on a take profit or trailing stop.
/// </summary>
public class Ilan16DynamicStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<decimal> _pipStep;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _startLong;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailStart;
	private readonly StrategyParam<decimal> _trailStop;

	private int _tradeCount;
	private decimal _lastPrice;
	private decimal _avgPrice;
	private decimal _totalVolume;
	private bool _isLong;
	private decimal? _trailingStop;

	/// <summary>
	/// Volume of the first order.
	/// </summary>
	public decimal InitialVolume { get => _initialVolume.Value; set => _initialVolume.Value = value; }

	/// <summary>
	/// Multiplier for subsequent order size.
	/// </summary>
	public decimal LotExponent { get => _lotExponent.Value; set => _lotExponent.Value = value; }

	/// <summary>
	/// Distance in points between grid levels.
	/// </summary>
	public decimal PipStep { get => _pipStep.Value; set => _pipStep.Value = value; }

	/// <summary>
	/// Profit target from average price in points.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Maximum number of averaging entries.
	/// </summary>
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Open first trade as long if true.
	/// </summary>
	public bool StartLong { get => _startLong.Value; set => _startLong.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }

	/// <summary>
	/// Profit in points to start trailing.
	/// </summary>
	public decimal TrailStart { get => _trailStart.Value; set => _trailStart.Value = value; }

	/// <summary>
	/// Trailing distance in points.
	/// </summary>
	public decimal TrailStop { get => _trailStop.Value; set => _trailStop.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public Ilan16DynamicStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Volume of the first order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_lotExponent = Param(nameof(LotExponent), 1.6m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Exponent", "Multiplier for subsequent order size", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1.1m, 2m, 0.1m);

		_pipStep = Param(nameof(PipStep), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Step", "Distance in points between grid levels", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_takeProfit = Param(nameof(TakeProfit), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target from average price", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of averaging entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_startLong = Param(nameof(StartLong), true)
			.SetDisplay("Start Long", "Open first trade as long", "General");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailStart = Param(nameof(TrailStart), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Start", "Profit to start trailing", "Risk");

		_trailStop = Param(nameof(TrailStop), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Stop", "Trailing distance", "Risk");

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

		_tradeCount = 0;
		_lastPrice = 0m;
		_avgPrice = 0m;
		_totalVolume = 0m;
		_isLong = StartLong;
		_trailingStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_isLong = StartLong;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.PriceStep ?? 1m;
		var price = candle.ClosePrice;

		if (Position == 0)
		{
			var volume = InitialVolume;
			if (_isLong)
				BuyMarket(volume);
			else
				SellMarket(volume);

			_tradeCount = 1;
			_lastPrice = price;
			_avgPrice = price;
			_totalVolume = volume;
			_trailingStop = null;
			return;
		}

		if (UseTrailingStop)
		{
			if (_isLong)
			{
				if (_trailingStop == null && price - _avgPrice >= TrailStart * step)
					_trailingStop = price - TrailStop * step;
				else if (_trailingStop != null && price - TrailStop * step > _trailingStop)
					_trailingStop = price - TrailStop * step;

				if (_trailingStop != null && price <= _trailingStop)
				{
					SellMarket(Position);
					ResetState();
					return;
				}
			}
			else
			{
				if (_trailingStop == null && _avgPrice - price >= TrailStart * step)
					_trailingStop = price + TrailStop * step;
				else if (_trailingStop != null && price + TrailStop * step < _trailingStop)
					_trailingStop = price + TrailStop * step;

				if (_trailingStop != null && price >= _trailingStop)
				{
					BuyMarket(-Position);
					ResetState();
					return;
				}
			}
		}

		if (_isLong && _tradeCount < MaxTrades && _lastPrice - price >= PipStep * step)
		{
			var volume = InitialVolume * (decimal)Math.Pow((double)LotExponent, _tradeCount);
			BuyMarket(volume);
			_tradeCount++;
			_lastPrice = price;
			UpdateAverage(price, volume);
		}
		else if (!_isLong && _tradeCount < MaxTrades && price - _lastPrice >= PipStep * step)
		{
			var volume = InitialVolume * (decimal)Math.Pow((double)LotExponent, _tradeCount);
			SellMarket(volume);
			_tradeCount++;
			_lastPrice = price;
			UpdateAverage(price, volume);
		}

		if (_isLong && price >= _avgPrice + TakeProfit * step)
		{
			SellMarket(Position);
			ResetState();
		}
		else if (!_isLong && price <= _avgPrice - TakeProfit * step)
		{
			BuyMarket(-Position);
			ResetState();
		}
	}

	private void UpdateAverage(decimal price, decimal volume)
	{
		var totalValue = _avgPrice * _totalVolume + price * volume;
		_totalVolume += volume;
		_avgPrice = totalValue / _totalVolume;
	}

	private void ResetState()
	{
		_tradeCount = 0;
		_lastPrice = 0m;
		_avgPrice = 0m;
		_totalVolume = 0m;
		_isLong = StartLong;
		_trailingStop = null;
	}
}
