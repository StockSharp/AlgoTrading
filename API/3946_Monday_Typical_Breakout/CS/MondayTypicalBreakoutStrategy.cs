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
/// Port of the MetaTrader strategy yi1ywioff50qr6 (ID 8187).
/// Buys on Monday when the hourly open breaks above the prior bar's typical price.
/// Applies equity-based position sizing when the fixed lot is disabled.
/// </summary>
public class MondayTypicalBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<int> _openHour;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _initialEquity;
	private readonly StrategyParam<decimal> _equityStep;
	private readonly StrategyParam<decimal> _initialStepVolume;
	private readonly StrategyParam<decimal> _volumeStep;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _previousCandle;
	private DateTimeOffset? _lastSignalTime;
	private decimal _priceStep;

	/// <summary>
	/// Initializes parameters to mirror the MQL expert defaults.
	/// </summary>
	public MondayTypicalBreakoutStrategy()
	{
		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
		.SetNotNegative()
		.SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk");

		_openHour = Param(nameof(OpenHour), 9)
		.SetRange(0, 23)
		.SetDisplay("Open Hour", "Hour of the session to evaluate Monday breakout entries", "Session");

		_stopLossPoints = Param(nameof(StopLossPoints), 50)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20)
		.SetNotNegative()
		.SetDisplay("Take Profit (points)", "Profit target distance expressed in price points", "Risk");

		_initialEquity = Param(nameof(InitialEquity), 600m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Equity", "Account equity threshold that triggers the first scaling tier", "Money Management");

		_equityStep = Param(nameof(EquityStep), 300m)
		.SetGreaterThanZero()
		.SetDisplay("Equity Step", "Incremental equity required to raise the position size", "Money Management");

		_initialStepVolume = Param(nameof(InitialStepVolume), 0.4m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Step Volume", "Lot size used once the equity threshold is met", "Money Management");

		_volumeStep = Param(nameof(VolumeStep), 0.2m)
		.SetNotNegative()
		.SetDisplay("Volume Step", "Additional lot size added for each equity step", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for detecting the Monday breakout", "General");
	}

	/// <summary>
	/// Fixed lot size used for entries (set to zero to enable scaling).
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Hour (0-23) when Monday entries are evaluated.
	/// </summary>
	public int OpenHour
	{
		get => _openHour.Value;
		set => _openHour.Value = value;
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
	/// Minimum equity required before the scaling table becomes active.
	/// </summary>
	public decimal InitialEquity
	{
		get => _initialEquity.Value;
		set => _initialEquity.Value = value;
	}

	/// <summary>
	/// Equity increment that increases the trade size by <see cref="VolumeStep"/>.
	/// </summary>
	public decimal EquityStep
	{
		get => _equityStep.Value;
		set => _equityStep.Value = value;
	}

	/// <summary>
	/// Volume applied when the first equity threshold is met.
	/// </summary>
	public decimal InitialStepVolume
	{
		get => _initialStepVolume.Value;
		set => _initialStepVolume.Value = value;
	}

	/// <summary>
	/// Additional volume added for each equity tier.
	/// </summary>
	public decimal VolumeStep
	{
		get => _volumeStep.Value;
		set => _volumeStep.Value = value;
	}

	/// <summary>
	/// Candle type used for detecting breakout conditions.
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

		_previousCandle = null;
		_lastSignalTime = null;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		_priceStep = 0.0001m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		if (TakeProfitPoints > 0 || StopLossPoints > 0)
		{
			var takeDistance = TakeProfitPoints > 0
			? new Unit(TakeProfitPoints * _priceStep, UnitTypes.Point)
			: new Unit(0m);
			var stopDistance = StopLossPoints > 0
			? new Unit(StopLossPoints * _priceStep, UnitTypes.Point)
			: new Unit(0m);

			StartProtection(takeProfit: takeDistance, stopLoss: stopDistance);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var previous = _previousCandle;
		_previousCandle = candle;

		if (previous is null)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0m || ActiveOrders.Count > 0)
		return;

		var candleTime = candle.OpenTime.ToLocalTime();
		if (candleTime.DayOfWeek != DayOfWeek.Monday)
		return;

		if (candleTime.Hour != OpenHour)
		return;

		if (_lastSignalTime is DateTimeOffset last && last == candle.OpenTime)
		return;

		var typicalPrice = (previous.HighPrice + previous.LowPrice + previous.ClosePrice) / 3m;
		if (candle.OpenPrice <= typicalPrice)
		return;

		var volume = CalculateOrderVolume();
		volume = AlignVolume(volume);

		if (volume <= 0m)
		return;

		CancelActiveOrders();
		BuyMarket(volume);

		_lastSignalTime = candle.OpenTime;
	}

	private decimal CalculateOrderVolume()
	{
		var fixedVolume = FixedVolume;
		if (fixedVolume > 0m)
		return fixedVolume;

		var security = Security;
		var portfolio = Portfolio;

		var minVolume = security?.VolumeMin ?? security?.MinVolume ?? 0m;
		if (minVolume <= 0m)
		minVolume = 0.01m;

		var equity = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
		return minVolume;

		if (equity < InitialEquity)
		return minVolume;

		if (EquityStep <= 0m)
		return InitialStepVolume;

		var stepsDecimal = (equity - InitialEquity) / EquityStep;
		if (stepsDecimal < 0m)
		stepsDecimal = 0m;

		var steps = (int)Math.Floor(stepsDecimal);
		var dynamicVolume = InitialStepVolume + VolumeStep * steps;

		if (dynamicVolume < minVolume)
		dynamicVolume = minVolume;

		return dynamicVolume;
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var minVolume = security.VolumeMin ?? security.MinVolume ?? 0m;
		var maxVolume = security.VolumeMax ?? 0m;
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
