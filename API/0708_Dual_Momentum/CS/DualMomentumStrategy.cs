using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Rotates between a risky and a safe asset based on dual momentum.
/// </summary>
public class DualMomentumStrategy : Strategy
{
	private readonly StrategyParam<Security> _riskyAsset;
	private readonly StrategyParam<Security> _safeAsset;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private RateOfChange _rocRisky;
	private RateOfChange _rocSafe;
	private decimal _momRisky;
	private decimal _momSafe;
	private decimal _priceRisky;
	private decimal _priceSafe;
	private DateTime _lastMonth;

	/// <summary>
	/// Initializes a new instance of <see cref="DualMomentumStrategy"/>.
	/// </summary>
	public DualMomentumStrategy()
	{
		_riskyAsset = Param<Security>(nameof(RiskyAsset))
			.SetDisplay("Risky Asset", "Security considered risky", "General");

		_safeAsset = Param<Security>(nameof(SafeAsset))
			.SetDisplay("Safe Asset", "Security considered safe", "General");

		_period = Param(nameof(Period), 12)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Lookback in months", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for momentum", "General");
	}

	/// <summary>
	/// Risky asset security.
	/// </summary>
	public Security RiskyAsset
	{
		get => _riskyAsset.Value;
		set => _riskyAsset.Value = value;
	}

	/// <summary>
	/// Safe asset security.
	/// </summary>
	public Security SafeAsset
	{
		get => _safeAsset.Value;
		set => _safeAsset.Value = value;
	}

	/// <summary>
	/// Momentum lookback period in months.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(RiskyAsset, CandleType), (SafeAsset, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_momRisky = _momSafe = 0m;
		_priceRisky = _priceSafe = 0m;
		_lastMonth = default;
		_rocRisky = null;
		_rocSafe = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		if (RiskyAsset == null || SafeAsset == null)
			throw new InvalidOperationException("Assets must be specified.");

		base.OnStarted(time);

		_rocRisky = new RateOfChange { Length = Period };
		_rocSafe = new RateOfChange { Length = Period };

		SubscribeCandles(CandleType, true, RiskyAsset)
			.Bind(_rocRisky, ProcessRisky)
			.Start();

		SubscribeCandles(CandleType, true, SafeAsset)
			.Bind(_rocSafe, ProcessSafe)
			.Start();
	}

	private void ProcessRisky(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_priceRisky = candle.ClosePrice;
		_momRisky = momentum;

		TryEvaluate(candle.OpenTime);
	}

	private void ProcessSafe(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_priceSafe = candle.ClosePrice;
		_momSafe = momentum;
	}

	private void TryEvaluate(DateTimeOffset time)
	{
		if (!_rocRisky.IsFormed || !_rocSafe.IsFormed)
			return;

		var month = new DateTime(time.Year, time.Month, 1);
		if (month == _lastMonth)
			return;
		_lastMonth = month;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var equity = Portfolio.CurrentValue ?? 0m;
		if (equity <= 0)
			return;

		if (_momRisky > 0 && _momRisky > _momSafe && _priceRisky > 0)
		{
			var qty = equity / _priceRisky;
			Move(RiskyAsset, qty);
			Move(SafeAsset, 0);
		}
		else if (_priceSafe > 0)
		{
			var qty = equity / _priceSafe;
			Move(SafeAsset, qty);
			Move(RiskyAsset, 0);
		}
	}

	private void Move(Security s, decimal target)
	{
		var diff = target - PositionBy(s);
		if (diff == 0)
			return;

		RegisterOrder(new Order
		{
			Security = s,
			Portfolio = Portfolio,
			Side = diff > 0 ? Sides.Buy : Sides.Sell,
			Volume = Math.Abs(diff),
			Type = OrderTypes.Market,
			Comment = "DualMomentum"
		});
	}

	private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0m;
}
