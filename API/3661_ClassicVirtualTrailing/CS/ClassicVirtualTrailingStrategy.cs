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

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop manager that replicates the behaviour of the "Classic &amp; Virtual Trailing" MetaTrader expert.
/// The strategy itself does not open positions and simply manages the active one by shifting a trailing stop level
/// according to the selected mode (classic stop order updates or virtual closing logic).
/// </summary>
public class ClassicVirtualTrailingStrategy : Strategy
{
	private static readonly Level1Fields? StopLevelField = TryResolveField("StopLevel")
	?? TryResolveField("MinStopPrice")
	?? TryResolveField("StopPrice")
	?? TryResolveField("StopDistance");

	private readonly StrategyParam<TrailingManagementModes> _trailingMode;
	private readonly StrategyParam<decimal> _trailingStartPips;
	private readonly StrategyParam<decimal> _trailingGapPips;

	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal? _longTrailingPrice;
	private decimal? _shortTrailingPrice;
	private decimal? _stopLevelPoints;

	/// <summary>
	/// Specifies whether classic or virtual trailing management is applied.
	/// </summary>
	public TrailingManagementModes TrailingMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	/// <summary>
	/// Profit (in pips) that has to be accumulated before the trailing logic activates.
	/// </summary>
	public decimal TrailingStartPips
	{
		get => _trailingStartPips.Value;
		set => _trailingStartPips.Value = value;
	}

	/// <summary>
	/// Distance (in pips) maintained between price and the trailing stop.
	/// </summary>
	public decimal TrailingGapPips
	{
		get => _trailingGapPips.Value;
		set => _trailingGapPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ClassicVirtualTrailingStrategy"/> class.
	/// </summary>
	public ClassicVirtualTrailingStrategy()
	{
		_trailingMode = Param(nameof(TrailingMode), TrailingManagementModes.Virtual)
		.SetDisplay("Trailing Mode", "Classic updates stop orders, Virtual closes at the trail", "Risk Management");

		_trailingStartPips = Param(nameof(TrailingStartPips), 30m)
		.SetNotNegative()
		.SetDisplay("Trailing Start (pips)", "Profit in pips required before trailing activates", "Risk Management")
		.SetCanOptimize(true);

		_trailingGapPips = Param(nameof(TrailingGapPips), 30m)
		.SetNotNegative()
		.SetDisplay("Trailing Gap (pips)", "Distance between price and the trailing level", "Risk Management")
		.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetTrailing();
		_currentBid = null;
		_currentAsk = null;
		_stopLevelPoints = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		{
			throw new InvalidOperationException("Security is not specified.");
		}

		if (Portfolio == null)
		{
			throw new InvalidOperationException("Portfolio is not specified.");
		}

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			ResetTrailing();
		}
		else if (Position > 0m)
		{
			_shortTrailingPrice = null;
		}
		else
		{
			_longTrailingPrice = null;
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
		_currentBid = bid;

		if (level1.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
		_currentAsk = ask;

		if (StopLevelField is Level1Fields stopField && level1.Changes.TryGetValue(stopField, out var stopRaw))
		_stopLevelPoints = ToDecimal(stopRaw);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var pipSize = GetPipSize();

		UpdateLongTrailing(pipSize);
		UpdateShortTrailing(pipSize);
	}

	private void UpdateLongTrailing(decimal pipSize)
	{
		if (Position <= 0m)
		{
			_longTrailingPrice = null;
			return;
		}

		if (_currentBid is not decimal bid || bid <= 0m)
		return;

		var entryPrice = Position.AveragePrice;
		if (entryPrice <= 0m)
		return;

		var startDistance = Math.Max(0m, TrailingStartPips) * pipSize;
		var gapDistance = Math.Max(0m, TrailingGapPips) * pipSize;

		if (TrailingMode == TrailingManagementModes.Classic)
		{
			var minimalGap = GetMinimalClassicGap(pipSize);
			if (gapDistance < minimalGap)
			gapDistance = minimalGap;
		}

		var activationPrice = entryPrice + startDistance + gapDistance;
		if (bid >= activationPrice)
		{
			var newStop = bid - gapDistance;
			if (!_longTrailingPrice.HasValue || newStop > _longTrailingPrice.Value)
			_longTrailingPrice = newStop;
		}

		if (_longTrailingPrice.HasValue && bid <= _longTrailingPrice.Value && bid > entryPrice)
		{
			SellMarket(Math.Abs(Position));
			_longTrailingPrice = null;
		}
	}

	private void UpdateShortTrailing(decimal pipSize)
	{
		if (Position >= 0m)
		{
			_shortTrailingPrice = null;
			return;
		}

		if (_currentAsk is not decimal ask || ask <= 0m)
		return;

		var entryPrice = Position.AveragePrice;
		if (entryPrice <= 0m)
		return;

		var startDistance = Math.Max(0m, TrailingStartPips) * pipSize;
		var gapDistance = Math.Max(0m, TrailingGapPips) * pipSize;

		if (TrailingMode == TrailingManagementModes.Classic)
		{
			var minimalGap = GetMinimalClassicGap(pipSize);
			if (gapDistance < minimalGap)
			gapDistance = minimalGap;
		}

		var activationPrice = entryPrice - startDistance - gapDistance;
		if (ask <= activationPrice)
		{
			var newStop = ask + gapDistance;
			if (!_shortTrailingPrice.HasValue || newStop < _shortTrailingPrice.Value)
			_shortTrailingPrice = newStop;
		}

		if (_shortTrailingPrice.HasValue && ask >= _shortTrailingPrice.Value && entryPrice > ask)
		{
			BuyMarket(Math.Abs(Position));
			_shortTrailingPrice = null;
		}
	}

	private decimal GetMinimalClassicGap(decimal pipSize)
	{
		if (_stopLevelPoints is decimal stopPoints && stopPoints > 0m)
		{
			return stopPoints * pipSize;
		}

		return TrailingGapPips * pipSize;
	}

	private void ResetTrailing()
	{
		_longTrailingPrice = null;
		_shortTrailingPrice = null;
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 1m;

		var decimals = Security?.Decimals ?? 0;
		var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;
		return priceStep * multiplier;
	}

	private static Level1Fields? TryResolveField(string name)
	{
		return Enum.TryParse(name, out Level1Fields field)
		? field
		: null;
	}

	private static decimal? ToDecimal(object value)
	{
		return value switch
		{
			null => null,
			decimal dec => dec,
			double dbl => (decimal)dbl,
			float flt => (decimal)flt,
			int i => i,
			long l => l,
			short s => s,
			string str when decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) => result,
			_ => null
		};
	}

	public enum TrailingManagementModes
	{
		Classic,
		Virtual
	}
}
