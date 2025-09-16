using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the Exp_TrailingStop MQL expert by managing trailing stops for existing positions.
/// The strategy does not generate entries and is intended to be attached to positions opened by other logic.
/// </summary>
public class TrailingStopManagerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStartPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _priceDeviationPoints;

	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _lastAsk;
	private decimal? _lastBid;
	private decimal _previousPosition;

	/// <summary>
	/// Distance between market price and the stop in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit distance in price steps required before trailing starts.
	/// </summary>
	public decimal TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Minimum improvement in price steps required before moving the stop again.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Reserved parameter kept for compatibility with the original MQL expert.
	/// </summary>
	public decimal PriceDeviationPoints
	{
		get => _priceDeviationPoints.Value;
		set => _priceDeviationPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrailingStopManagerStrategy"/>.
	/// </summary>
	public TrailingStopManagerStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-loss distance", "Distance from market price to the stop in price steps.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(500m, 2000m, 100m);

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 1000m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing activation", "Profit distance in price steps required to enable trailing.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(500m, 3000m, 100m);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 200m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing step", "Minimum improvement in price steps before moving the stop again.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100m, 500m, 50m);

		_priceDeviationPoints = Param(nameof(PriceDeviationPoints), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Price deviation", "Reserved parameter preserved for compatibility with the source expert.", "Execution")
			.SetCanOptimize(true)
			.SetOptimize(0m, 50m, 5m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, DataType.Level1)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetTrailing();
		_lastAsk = null;
		_lastBid = null;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not specified.");

		var step = Security.PriceStep;
		if (step == null || step <= 0m)
			throw new InvalidOperationException("Security price step must be defined and positive.");

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetTrailing();
		}
		else if (_previousPosition <= 0m && Position > 0m)
		{
			// Direction changed to long - reset short trailing state.
			_shortStop = null;
			_longStop = null;
		}
		else if (_previousPosition >= 0m && Position < 0m)
		{
			// Direction changed to short - reset long trailing state.
			_longStop = null;
			_shortStop = null;
		}

		_previousPosition = Position;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj != null)
			_lastAsk = (decimal)askObj;

		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj != null)
			_lastBid = (decimal)bidObj;

		UpdateTrailingStops();
	}

	private void UpdateTrailingStops()
	{
		if (!IsFormedAndOnline())
			return;

		if (Security?.PriceStep is not decimal step || step <= 0m)
			return;

		var startDistance = TrailingStartPoints * step;
		var stepDistance = TrailingStepPoints * step;

		if (Position > 0m)
		{
			if (_lastAsk is not decimal ask)
				return;

			var entryPrice = PositionPrice;
			if (entryPrice <= 0m)
				return;

			var profitDistance = ask - entryPrice;
			if (profitDistance > startDistance)
			{
				var newStop = ask - StopLossPoints * step;

				if (!_longStop.HasValue || newStop - _longStop.Value > stepDistance)
				{
					_longStop = newStop;
					LogInfo($"Long trailing stop updated to {_longStop:F5}.");
				}
			}

			if (_longStop.HasValue && _lastBid is decimal bid && bid <= _longStop.Value)
			{
				var volume = Position;
				if (volume > 0m)
				{
					SellMarket(volume);
					LogInfo($"Long trailing stop triggered at {bid:F5}.");
				}

				ResetTrailing();
			}
		}
		else if (Position < 0m)
		{
			if (_lastBid is not decimal bid)
				return;

			var entryPrice = PositionPrice;
			if (entryPrice <= 0m)
				return;

			var profitDistance = entryPrice - bid;
			if (profitDistance > startDistance)
			{
				var newStop = bid + StopLossPoints * step;

				if (!_shortStop.HasValue || _shortStop.Value - newStop > stepDistance)
				{
					_shortStop = newStop;
					LogInfo($"Short trailing stop updated to {_shortStop:F5}.");
				}
			}

			if (_shortStop.HasValue && _lastAsk is decimal ask && ask >= _shortStop.Value)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
				{
					BuyMarket(volume);
					LogInfo($"Short trailing stop triggered at {ask:F5}.");
				}

				ResetTrailing();
			}
		}
		else
		{
			ResetTrailing();
		}
	}

	private void ResetTrailing()
	{
		_longStop = null;
		_shortStop = null;
	}
}
