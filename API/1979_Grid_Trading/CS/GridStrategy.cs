using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<DataType> _candleType;

	private Order _buyStop;
	private Order _sellStop;
	private decimal _nextBuyVolume;
	private decimal _nextSellVolume;

	/// <summary>
	/// Grid step in price points.
	/// </summary>
	public int GridStep { get => _gridStep.Value; set => _gridStep.Value = value; }

	/// <summary>
	/// Base volume for initial orders.
	/// </summary>
	public decimal BaseVolume { get => _baseVolume.Value; set => _baseVolume.Value = value; }

	/// <summary>
	/// Take profit distance for each trade in price points.
	/// </summary>
	public int TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

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

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Points", "Take profit distance in points", "General");

		_profitTarget = Param(nameof(ProfitTarget), 1m)
			.SetDisplay("Profit Target", "Total profit to reset grid", "General");

		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Use Martingale", "Increase volume after losses", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for price feed", "General");

		_nextBuyVolume = BaseVolume;
		_nextSellVolume = BaseVolume;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(new Unit(TakeProfitPoints * (Security.PriceStep ?? 1m), UnitTypes.Price));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = (Security.PriceStep ?? 1m) * GridStep;
		var sellPrice = Math.Floor(candle.ClosePrice / step) * step;
		var buyPrice = sellPrice + step;

		if (_buyStop?.State == OrderStates.Active && _buyStop.Price != buyPrice)
			Cancel(_buyStop);

		if (_sellStop?.State == OrderStates.Active && _sellStop.Price != sellPrice)
			Cancel(_sellStop);

		if (_buyStop == null || _buyStop.State != OrderStates.Active)
			_buyStop = BuyStop(_nextBuyVolume, buyPrice);

		if (_sellStop == null || _sellStop.State != OrderStates.Active)
			_sellStop = SellStop(_nextSellVolume, sellPrice);

		if (UseMartingale)
		{
			var longVolume = Position > 0 ? Position : 0m;
			var shortVolume = Position < 0 ? -Position : 0m;
			_nextBuyVolume = shortVolume + BaseVolume;
			_nextSellVolume = longVolume + BaseVolume;
		}
		else
		{
			_nextBuyVolume = BaseVolume;
			_nextSellVolume = BaseVolume;
		}

		var unrealized = Position * (candle.ClosePrice - PositionPrice);
		var totalProfit = PnL + unrealized;

		if (totalProfit >= ProfitTarget)
		{
			CloseAll();
			_nextBuyVolume = BaseVolume;
			_nextSellVolume = BaseVolume;
		}
	}
}
