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
/// Simple grid trading strategy with optional martingale sizing.
/// </summary>
public class GridStrategy : Strategy
{
	private readonly StrategyParam<int> _gridStep;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastGridLevel;
	private decimal _entryPrice;

	/// <summary>
	/// Grid step in price points.
	/// </summary>
	public int GridStep { get => _gridStep.Value; set => _gridStep.Value = value; }

	/// <summary>
	/// Base volume for initial orders.
	/// </summary>
	public decimal BaseVolume { get => _baseVolume.Value; set => _baseVolume.Value = value; }

	/// <summary>
	/// Profit threshold to reset the grid.
	/// </summary>
	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }

	/// <summary>
	/// Enable martingale position sizing.
	/// </summary>
	public bool UseMartingale { get => _useMartingale.Value; set => _useMartingale.Value = value; }

	/// <summary>
	/// Candle type used for price updates.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public GridStrategy()
	{
		_gridStep = Param(nameof(GridStep), 10)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step", "Step size in price points", "General");

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Initial order volume", "General");

		_profitTarget = Param(nameof(ProfitTarget), 1m)
			.SetDisplay("Profit Target", "Total profit to reset grid", "General");

		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Use Martingale", "Increase volume after losses", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for price feed", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = (Security.PriceStep ?? 1m) * GridStep;
		var currentLevel = Math.Floor(candle.ClosePrice / step) * step;

		if (_lastGridLevel == 0)
		{
			_lastGridLevel = currentLevel;
			return;
		}

		if (currentLevel > _lastGridLevel)
		{
			// Price moved up - buy
			var vol = UseMartingale && Position < 0 ? Math.Abs(Position) + BaseVolume : BaseVolume;
			BuyMarket();
			if (Position == 0) _entryPrice = candle.ClosePrice;
		}
		else if (currentLevel < _lastGridLevel)
		{
			// Price moved down - sell
			var vol = UseMartingale && Position > 0 ? Position + BaseVolume : BaseVolume;
			SellMarket();
			if (Position == 0) _entryPrice = candle.ClosePrice;
		}

		_lastGridLevel = currentLevel;

		// Check profit target
		if (Position != 0 && _entryPrice > 0)
		{
			var unrealized = Position * (candle.ClosePrice - _entryPrice);
			if (unrealized >= ProfitTarget)
			{
				if (Position > 0) SellMarket(); else BuyMarket();
				_entryPrice = 0;
			}
		}
	}
}
