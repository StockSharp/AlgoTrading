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
/// Simple martingale template strategy combining fast and slow SMA cross with breakout confirmation.
/// </summary>
public class SimpleMartingaleTemplateStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;

	private decimal _nextVolume;
	private decimal _lastTradeVolume;
	private decimal? _lastBalance;

	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal? _priorFast;
	private decimal? _priorSlow;
	private decimal? _previousClose;
	private decimal? _previousHigh;
	private decimal? _previousLow;
	private decimal? _priorHigh;
	private decimal? _priorLow;

	private decimal? _stopPrice;
	private decimal? _takePrice;
	private Sides? _activeSide;

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
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
	/// Base trading volume used for the first martingale step.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied after a losing trade.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Period of the fast SMA used for direction detection.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow SMA used for trend confirmation.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Time-frame of candles processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SimpleMartingaleTemplateStrategy"/>.
	/// </summary>
	public SimpleMartingaleTemplateStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 650m)
			.SetRange(10m, 5000m)
			.SetDisplay("Stop-Loss Points", "Distance to stop-loss in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 650m)
			.SetRange(10m, 5000m)
			.SetDisplay("Take-Profit Points", "Distance to take-profit in points", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.01m)
			.SetRange(0.01m, 10m)
			.SetDisplay("Base Volume", "Initial trade volume", "Money Management");

		_multiplier = Param(nameof(Multiplier), 2.5m)
			.SetRange(1m, 5m)
			.SetDisplay("Multiplier", "Martingale multiplier after losses", "Money Management");

		_fastPeriod = Param(nameof(FastPeriod), 50)
			.SetRange(2, 500)
			.SetDisplay("Fast SMA", "Fast SMA length", "Signals");

		_slowPeriod = Param(nameof(SlowPeriod), 200)
			.SetRange(5, 1000)
			.SetDisplay("Slow SMA", "Slow SMA length", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame of processed candles", "General");
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

		_fastSma = null;
		_slowSma = null;
		_nextVolume = 0m;
		_lastTradeVolume = 0m;
		_lastBalance = null;
		_previousFast = null;
		_previousSlow = null;
		_priorFast = null;
		_priorSlow = null;
		_previousClose = null;
		_previousHigh = null;
		_previousLow = null;
		_priorHigh = null;
		_priorLow = null;
		_stopPrice = null;
		_takePrice = null;
		_activeSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastSma = new SimpleMovingAverage { Length = FastPeriod };
		_slowSma = new SimpleMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastSma, _slowSma, ProcessCandle)
			.Start();

		InitializeVolume();
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		// Skip unfinished candles because indicator values are not final.
		if (candle.State != CandleStates.Finished)
			return;

		// Remove outdated stops if the strategy is already flat.
		if (Position == 0m && _activeSide != null)
			ResetProtectionLevels();

		UpdateStops(candle);
		UpdateVolumeAfterTrade();

		if (Position != 0m || HasActiveOrders())
		{
			StoreHistory(fastValue, slowValue, candle);
			return;
		}

		if (_previousFast is decimal prevFast &&
			_previousSlow is decimal prevSlow &&
			_priorFast is decimal priorFast &&
			_priorSlow is decimal priorSlow &&
			_previousClose is decimal prevClose &&
			_priorHigh is decimal priorHigh &&
			_priorLow is decimal priorLow)
		{
			// The pattern mirrors the original MQL logic: detect cross and confirm breakout.
			var bullishCross = prevClose > prevFast && prevFast > prevSlow && priorFast < priorSlow && prevClose > priorHigh;
			var bearishCross = prevClose < prevFast && prevFast < prevSlow && priorFast > priorSlow && prevClose < priorLow;

			if (bullishCross)
			{
				EnterPosition(Sides.Buy, candle);
			}
			else if (bearishCross)
			{
				EnterPosition(Sides.Sell, candle);
			}
		}

		StoreHistory(fastValue, slowValue, candle);
	}

	private void UpdateStops(ICandleMessage candle)
	{
		if (_activeSide == null)
			return;

		if (_activeSide == Sides.Buy && _stopPrice is decimal longStop && _takePrice is decimal longTake)
		{
			// Close long position on protective stop or profit target.
			if (Position > 0m && candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				ResetProtectionLevels();
			}
			else if (Position > 0m && candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				ResetProtectionLevels();
			}
		}
		else if (_activeSide == Sides.Sell && _stopPrice is decimal shortStop && _takePrice is decimal shortTake)
		{
			var shortVolume = Math.Abs(Position);
			if (shortVolume <= 0m)
				return;

			// Close short position on protective stop or profit target.
			if (candle.HighPrice >= shortStop)
			{
				BuyMarket(shortVolume);
				ResetProtectionLevels();
			}
			else if (candle.LowPrice <= shortTake)
			{
				BuyMarket(shortVolume);
				ResetProtectionLevels();
			}
		}
	}

	private void UpdateVolumeAfterTrade()
	{
		// Adjust martingale volume only when the strategy is flat and orders are finished.
		if (Portfolio == null || Position != 0m || HasActiveOrders())
			return;

		var balance = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;

		if (_lastBalance is null)
		{
			_lastBalance = balance;
			return;
		}

		var change = balance - _lastBalance.Value;
		if (change > 0.01m)
		{
			// Reset to the base volume after profitable cycles.
			_nextVolume = AlignVolume(BaseVolume);
			_lastBalance = balance;
		}
		else if (change < -0.01m)
		{
			// Increase exposure after a loss according to the multiplier.
			_nextVolume = AlignVolume(_lastTradeVolume * Multiplier);
			_lastBalance = balance;
		}
	}

	private void InitializeVolume()
	{
		_lastBalance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		_nextVolume = AlignVolume(BaseVolume);
		_lastTradeVolume = _nextVolume;
	}

	private decimal AlignVolume(decimal volume)
	{
		// Align requested volume to exchange constraints.
		var step = Security?.VolumeStep ?? 1m;
		var minVolume = Security?.MinVolume ?? step;
		var maxVolume = Security?.MaxVolume;

		if (step <= 0m)
			step = 1m;

		var normalized = Math.Floor(volume / step) * step;

		if (normalized < minVolume)
			normalized = minVolume;

		if (maxVolume is decimal max && normalized > max)
			normalized = max;

		return normalized > 0m ? normalized : minVolume;
	}

	private void EnterPosition(Sides side, ICandleMessage candle)
	{
		var volume = _nextVolume;
		if (volume <= 0m)
			return;

		_lastTradeVolume = volume;
		_lastBalance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? _lastBalance;

		var step = GetPriceStep();
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		if (stopDistance <= 0m || takeDistance <= 0m)
			return;

		var entryPrice = candle.ClosePrice > 0m ? candle.ClosePrice : candle.OpenPrice;

		// Register market order and remember protection levels for manual tracking.
		if (side == Sides.Buy)
		{
			BuyMarket(volume);
			_stopPrice = entryPrice - stopDistance;
			_takePrice = entryPrice + takeDistance;
			_activeSide = Sides.Buy;
		}
		else
		{
			SellMarket(volume);
			_stopPrice = entryPrice + stopDistance;
			_takePrice = entryPrice - takeDistance;
			_activeSide = Sides.Sell;
		}
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;

		// Fallback to decimal precision if the step is not provided by the adapter.
		if (step <= 0m)
		{
			var decimals = Security?.Decimals ?? 4;
			step = 1m;
			for (var i = 0; i < decimals; i++)
				step /= 10m;
		}

		return step;
	}

	private void StoreHistory(decimal fastValue, decimal slowValue, ICandleMessage candle)
	{
		// Shift buffers so the previous values emulate MQL's shift indexes.
		_priorFast = _previousFast;
		_priorSlow = _previousSlow;
		_priorHigh = _previousHigh;
		_priorLow = _previousLow;

		_previousFast = fastValue;
		_previousSlow = slowValue;
		_previousClose = candle.ClosePrice;
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}

	private void ResetProtectionLevels()
	{
		_stopPrice = null;
		_takePrice = null;
		_activeSide = null;
	}

	private bool HasActiveOrders()
	{
		// Scan all orders to check for still active requests.
		foreach (var order in Orders)
		{
			if (order.State.IsActive())
				return true;
		}

		return false;
	}
}

