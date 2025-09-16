using System;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Coin flip strategy with martingale money management and trailing stop handling.
/// </summary>
public class CoinFlipStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _entryPrice;
	private decimal _lastStoppedVolume;
	private decimal _lastTradeVolume;
	private bool _closeRequested;
	private bool _closeByStop;
	private Random _random;

	/// <summary>
	/// Stop loss distance measured in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop trigger distance measured in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing step that controls how often the stop is moved.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Base volume used for the very first trade in the series.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied after a stop loss to implement martingale.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum allowed trade volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for synchronization of trading decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CoinFlipStrategy"/>.
	/// </summary>
	public CoinFlipStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 15m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Trailing Stop", "Trailing stop activation distance", "Risk")
			.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Trailing Step", "Trailing step distance", "Risk")
			.SetCanOptimize(true);

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Initial trade volume", "Money Management")
			.SetCanOptimize(true);

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 1.8m)
			.SetGreaterThanOrEqualTo(1m)
			.SetDisplay("Martingale Mult", "Multiplier applied after stop loss", "Money Management")
			.SetCanOptimize(true);

		_maxVolume = Param(nameof(MaxVolume), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Volume", "Upper limit for trade volume", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stopPrice = 0m;
		_takePrice = 0m;
		_entryPrice = 0m;
		_lastStoppedVolume = 0m;
		_lastTradeVolume = 0m;
		_closeRequested = false;
		_closeByStop = false;
		_random = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_random = new Random(Environment.TickCount ^ GetHashCode());

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateTrailing(candle);

		if (Position == 0m && !HasActiveOrders())
		{
			var volume = GetNextVolume();
			if (volume <= 0m)
			return;

			var coin = _random.Next(0, 32768);

			if ((coin < 8192 || coin > 24575) && AllowLong())
			{
				BuyMarket(volume);
			}
			else if (AllowShort())
			{
				SellMarket(volume);
			}
		}
		else if (Position > 0m)
		{
			if (!_closeRequested && _stopPrice > 0m && candle.ClosePrice <= _stopPrice)
			{
				SellMarket(Position);
				_closeRequested = true;
				_closeByStop = true;
			}
			else if (!_closeRequested && _takePrice > 0m && candle.ClosePrice >= _takePrice)
			{
				SellMarket(Position);
				_closeRequested = true;
				_closeByStop = false;
			}
		}
		else if (Position < 0m)
		{
			var absPosition = Math.Abs(Position);

			if (!_closeRequested && _stopPrice > 0m && candle.ClosePrice >= _stopPrice)
			{
				BuyMarket(absPosition);
				_closeRequested = true;
				_closeByStop = true;
			}
			else if (!_closeRequested && _takePrice > 0m && candle.ClosePrice <= _takePrice)
			{
				BuyMarket(absPosition);
				_closeRequested = true;
				_closeByStop = false;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged()
	{
		base.OnPositionChanged();

		if (Position == 0m)
		{
			if (_closeRequested)
			{
				FinalizeClose();
			}

			_entryPrice = 0m;
			_stopPrice = 0m;
			_takePrice = 0m;
			_lastTradeVolume = 0m;
		}
		else
		{
			_entryPrice = PositionPrice;
			_lastTradeVolume = Math.Abs(Position);
			InitializeTargets();
			_closeRequested = false;
			_closeByStop = false;
		}
	}

	private void InitializeTargets()
	{
		var stopOffset = GetOffset(StopLossPoints);
		var takeOffset = GetOffset(TakeProfitPoints);

		if (Position > 0m)
		{
			_stopPrice = stopOffset > 0m ? _entryPrice - stopOffset : 0m;
			_takePrice = takeOffset > 0m ? _entryPrice + takeOffset : 0m;
		}
		else if (Position < 0m)
		{
			_stopPrice = stopOffset > 0m ? _entryPrice + stopOffset : 0m;
			_takePrice = takeOffset > 0m ? _entryPrice - takeOffset : 0m;
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (Position == 0m)
		return;

		var trailingDistance = GetOffset(TrailingStopPoints);
		var trailingStep = GetOffset(TrailingStepPoints);

		if (trailingDistance <= 0m)
		return;

		if (Position > 0m)
		{
			var profitDistance = candle.ClosePrice - _entryPrice;
			if (profitDistance > trailingDistance + trailingStep)
			{
				var candidate = candle.ClosePrice - trailingDistance;
				if (_stopPrice < candidate)
				_stopPrice = candidate;
			}
		}
		else if (Position < 0m)
		{
			var profitDistance = _entryPrice - candle.ClosePrice;
			if (profitDistance > trailingDistance + trailingStep)
			{
				var candidate = candle.ClosePrice + trailingDistance;
				if (_stopPrice == 0m || _stopPrice > candidate)
				_stopPrice = candidate;
			}
		}
	}

	private void FinalizeClose()
	{
		if (_closeByStop)
		{
			_lastStoppedVolume = _lastTradeVolume;
		}
		else
		{
			_lastStoppedVolume = 0m;
		}

		_closeRequested = false;
		_closeByStop = false;
	}

	private decimal GetNextVolume()
	{
		var volume = _lastStoppedVolume > 0m ? _lastStoppedVolume * MartingaleMultiplier : BaseVolume;

		if (volume > MaxVolume)
		{
			AddWarningLog($"Planned volume {volume} exceeds MaxVolume {MaxVolume}. Trade skipped.");
			return 0m;
		}

		return volume;
	}

	private bool HasActiveOrders()
	{
		return Orders.Any(o => o.State.IsActive());
	}

	private decimal GetOffset(decimal points)
	{
		if (points <= 0m)
		return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return points;

		return step * points;
	}
}
