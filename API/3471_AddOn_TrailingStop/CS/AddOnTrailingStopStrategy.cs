using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop utility that mirrors the AddOn_TrailingStop expert for MetaTrader.
/// It does not generate new entries and only manages stop levels for the current position.
/// </summary>
public class AddOnTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingStartPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _magicNumber;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal _previousPosition;

	/// <summary>
	/// Enables or disables trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit in pips required before the trailing stop activates.
	/// </summary>
	public decimal TrailingStartPips
	{
		get => _trailingStartPips.Value;
		set => _trailingStartPips.Value = value;
	}

	/// <summary>
	/// Additional profit in pips required before the trailing stop moves again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Identifier preserved for compatibility with the original expert advisor.
	/// The value is informational because StockSharp manages only the current strategy orders.
	/// </summary>
	public int MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AddOnTrailingStopStrategy"/>.
	/// </summary>
	public AddOnTrailingStopStrategy()
	{
		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Toggle trailing stop logic.", "Trailing")
			.SetCanOptimize(true);

		_trailingStartPips = Param(nameof(TrailingStartPips), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Start (pips)", "Profit distance in pips required before trailing activates.", "Trailing")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (pips)", "Extra profit in pips required before moving the stop again.", "Trailing")
			.SetCanOptimize(true);

		_magicNumber = Param(nameof(MagicNumber), 0)
			.SetDisplay("Magic Number", "Identifier preserved for compatibility with the MQL version.", "General");
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
		_lastBid = null;
		_lastAsk = null;
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

		if (GetPipSize() <= 0m)
			throw new InvalidOperationException("Unable to determine pip size from security settings.");

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
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
			// Direction changed to long - clear short trailing values.
			_shortStop = null;
			_longStop = null;
		}
		else if (_previousPosition >= 0m && Position < 0m)
		{
			// Direction changed to short - clear long trailing values.
			_longStop = null;
			_shortStop = null;
		}

		_previousPosition = Position;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (!EnableTrailing)
			return;

		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj != null)
			_lastBid = (decimal)bidObj;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj != null)
			_lastAsk = (decimal)askObj;

		UpdateTrailingStops();
	}

	private void UpdateTrailingStops()
	{
		if (!IsFormedAndOnline())
			return;

		if (TrailingStartPips <= 0m)
			return;

		var pip = GetPipSize();
		if (pip <= 0m)
			return;

		var startDistance = TrailingStartPips * pip;
		var stepDistance = TrailingStepPips * pip;

		if (Position > 0m)
		{
			if (_lastBid is not decimal bid)
				return;

			var entryPrice = PositionPrice;
			if (entryPrice <= 0m)
				return;

			if (bid - entryPrice >= startDistance)
			{
				var newStop = bid - startDistance;

				if (!_longStop.HasValue || newStop >= _longStop.Value + stepDistance)
				{
					_longStop = newStop;
					LogInfo($"Long trailing stop updated to {_longStop:F5}.");
				}
			}

			if (_longStop.HasValue && _lastBid is decimal bidToCheck && bidToCheck <= _longStop.Value)
			{
				var volume = Position;
				if (volume > 0m)
				{
					SellMarket(volume);
					LogInfo($"Long trailing stop triggered at {bidToCheck:F5}.");
				}

				ResetTrailing();
			}
		}
		else if (Position < 0m)
		{
			if (_lastAsk is not decimal ask)
				return;

			var entryPrice = PositionPrice;
			if (entryPrice <= 0m)
				return;

			if (entryPrice - ask >= startDistance)
			{
				var newStop = ask + startDistance;

				if (!_shortStop.HasValue || newStop <= _shortStop.Value - stepDistance)
				{
					_shortStop = newStop;
					LogInfo($"Short trailing stop updated to {_shortStop:F5}.");
				}
			}

			if (_shortStop.HasValue && _lastAsk is decimal askToCheck && askToCheck >= _shortStop.Value)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
				{
					BuyMarket(volume);
					LogInfo($"Short trailing stop triggered at {askToCheck:F5}.");
				}

				ResetTrailing();
			}
		}
		else
		{
			ResetTrailing();
		}
	}

	private decimal GetPipSize()
	{
		if (Security == null)
			return 0m;

		if (Security.Decimals is int decimals)
		{
			if (decimals == 2 || decimals == 3)
				return 0.01m;

			if (decimals == 4 || decimals == 5)
				return 0.0001m;
		}

		return Security.PriceStep ?? 0m;
	}

	private void ResetTrailing()
	{
		_longStop = null;
		_shortStop = null;
	}
}
