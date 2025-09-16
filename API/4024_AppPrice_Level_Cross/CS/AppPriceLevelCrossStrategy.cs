using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens trades when the close price crosses a configured level.
/// Converted from the MQL4 expert advisor BT_v4.
/// </summary>
public class AppPriceLevelCrossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _appPrice;
	private readonly StrategyParam<bool> _buyOnly;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableMoneyManagement;
	private readonly StrategyParam<decimal> _lotBalancePercent;
	private readonly StrategyParam<decimal> _minLot;
	private readonly StrategyParam<decimal> _maxLot;
	private readonly StrategyParam<int> _lotPrecision;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousClose;

	/// <summary>
	/// Initializes strategy parameters with defaults mirroring the MQL version.
	/// </summary>
	public AppPriceLevelCrossStrategy()
	{
		_appPrice = Param(nameof(AppPrice), 0m)
			.SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading");

		_buyOnly = Param(nameof(BuyOnly), true)
			.SetDisplay("Buy Only", "Enable to trade only long entries (set to false for sell-only mode)", "Trading");

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Volume", "Lot size used when money management is disabled", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 140)
			.SetDisplay("Stop Loss (points)", "Distance in price points for the protective stop (0 disables)", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 180)
			.SetDisplay("Take Profit (points)", "Distance in price points for the profit target (0 disables)", "Risk");

		_enableMoneyManagement = Param(nameof(EnableMoneyManagement), false)
			.SetDisplay("Enable MM", "Toggle balance-based position sizing", "Risk");

		_lotBalancePercent = Param(nameof(LotBalancePercent), 10m)
			.SetDisplay("Balance %", "Percentage of balance used to compute the lot when MM is enabled", "Risk");

		_minLot = Param(nameof(MinLot), 0.1m)
			.SetDisplay("Minimum Lot", "Lower bound for the calculated lot size", "Risk");

		_maxLot = Param(nameof(MaxLot), 5m)
			.SetDisplay("Maximum Lot", "Upper bound for the calculated lot size", "Risk");

		_lotPrecision = Param(nameof(LotPrecision), 1)
			.SetDisplay("Lot Precision", "Number of decimal places to round the calculated lot size", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signal generation", "General");
	}

	/// <summary>
	/// Target price level for the close-cross rule.
	/// </summary>
	public decimal AppPrice
	{
		get => _appPrice.Value;
		set => _appPrice.Value = value;
	}

	/// <summary>
	/// When true only long trades are allowed; set to false to trade only shorts.
	/// </summary>
	public bool BuyOnly
	{
		get => _buyOnly.Value;
		set => _buyOnly.Value = value;
	}

	/// <summary>
	/// Fixed lot size used when money management is disabled.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables the balance-percentage position sizing block.
	/// </summary>
	public bool EnableMoneyManagement
	{
		get => _enableMoneyManagement.Value;
		set => _enableMoneyManagement.Value = value;
	}

	/// <summary>
	/// Percentage of account balance used for lot calculation when MM is active.
	/// </summary>
	public decimal LotBalancePercent
	{
		get => _lotBalancePercent.Value;
		set => _lotBalancePercent.Value = value;
	}

	/// <summary>
	/// Minimum allowed lot for the calculated value.
	/// </summary>
	public decimal MinLot
	{
		get => _minLot.Value;
		set => _minLot.Value = value;
	}

	/// <summary>
	/// Maximum allowed lot for the calculated value.
	/// </summary>
	public decimal MaxLot
	{
		get => _maxLot.Value;
		set => _maxLot.Value = value;
	}

	/// <summary>
	/// Decimal precision applied to the calculated lot size.
	/// </summary>
	public int LotPrecision
	{
		get => _lotPrecision.Value;
		set => _lotPrecision.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the cross conditions.
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

		// Reset stored close value so the next formed candle rebuilds the history.
		_previousClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		var step = Security?.PriceStep ?? 1m;
		if (TakeProfitPoints > 0 || StopLossPoints > 0)
		{
			var takeDistance = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : new Unit(0m);
			var stopDistance = StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Point) : new Unit(0m);

			StartProtection(takeProfit: takeDistance, stopLoss: stopDistance);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previousClose = _previousClose;
		_previousClose = candle.ClosePrice;

		// Need at least one completed candle to compare against the configured level.
		if (previousClose is null)
			return;

		// Update history even when trading is paused, but only place orders when trading is allowed.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossedAbove = candle.ClosePrice > AppPrice && previousClose <= AppPrice;
		var crossedBelow = candle.ClosePrice < AppPrice && previousClose >= AppPrice;

		if (crossedAbove && BuyOnly)
		{
			ExecuteBuy();
		}
		else if (crossedBelow && !BuyOnly)
		{
			ExecuteSell();
		}
	}

	private void ExecuteBuy()
	{
		// Skip if we already hold a long position.
		if (Position > 0)
			return;

		var baseVolume = CalculateBaseVolume();
		if (baseVolume <= 0m)
			return;

		var volume = baseVolume;
		if (Position < 0)
			volume += Math.Abs(Position);

		volume = AlignVolume(volume);
		if (volume <= 0m)
			return;

		CancelActiveOrders();
		BuyMarket(volume);
	}

	private void ExecuteSell()
	{
		// Skip if we already hold a short position.
		if (Position < 0)
			return;

		var baseVolume = CalculateBaseVolume();
		if (baseVolume <= 0m)
			return;

		var volume = baseVolume;
		if (Position > 0)
			volume += Math.Abs(Position);

		volume = AlignVolume(volume);
		if (volume <= 0m)
			return;

		CancelActiveOrders();
		SellMarket(volume);
	}

	private decimal CalculateBaseVolume()
	{
		if (!EnableMoneyManagement)
			return FixedVolume;

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
		if (balance is null || balance <= 0m)
			return FixedVolume;

		var divisor = LotPrecision == 2 ? 100m : 1000m;
		if (LotPrecision <= 0)
			divisor = 1m;

		var precision = LotPrecision;
		if (precision < 0)
			precision = 0;

		var volume = LotBalancePercent / 100m * balance.Value / divisor;
		volume = Math.Round(volume, precision, MidpointRounding.AwayFromZero);

		if (volume < MinLot)
			volume = MinLot;
		if (volume > MaxLot)
			volume = MaxLot;

		return volume;
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var minVolume = security.VolumeMin ?? 0m;
		var maxVolume = security.VolumeMax ?? decimal.MaxValue;
		var step = security.VolumeStep ?? 0m;

		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		if (step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			volume = steps * step;
		}

		return volume;
	}
}
