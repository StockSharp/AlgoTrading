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
/// EMA/WMA crossover strategy converted from MetaTrader 4 expert advisor.
/// Uses exponential and weighted moving averages of candle open prices
/// and applies stop-loss/take-profit management similar to the original robot.
/// </summary>
public class EmaWmaRiskStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema = null!;
	private WeightedMovingAverage _wma = null!;

	private decimal? _previousEma;
	private decimal? _previousWma;

	/// <summary>
/// Initializes a new instance of the <see cref="EmaWmaRiskStrategy"/> class.
/// </summary>
public EmaWmaRiskStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for the exponential moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 2);

		_wmaPeriod = Param(nameof(WmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("WMA Period", "Period for the weighted moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 150m, 10m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Profit target distance expressed in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 150m, 10m);

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetNotNegative()
			.SetDisplay("Risk %", "Capital percentage risked per trade", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_orderVolume = Param(nameof(OrderVolume), 0m)
			.SetNotNegative()
			.SetDisplay("Fixed Volume", "Optional fixed order volume override", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations", "General");
	}

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// WMA length.
	/// </summary>
	public int WmaPeriod
	{
		get => _wmaPeriod.Value;
		set => _wmaPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in instrument steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in instrument steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Risk percentage used for position sizing.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed volume override. When zero the size is computed from <see cref="RiskPercent"/> and the stop distance.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
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

		_previousEma = null;
		_previousWma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_wma = new WeightedMovingAverage { Length = WmaPeriod };

		var stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Step) : null;
		var takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Step) : null;

		// Enable automatic stop-loss and take-profit handling similar to the MT4 implementation.
		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _wma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var emaValue = _ema.Process(new DecimalIndicatorValue(_ema, candle.OpenPrice, candle.OpenTime));
		var wmaValue = _wma.Process(new DecimalIndicatorValue(_wma, candle.OpenPrice, candle.OpenTime));

		if (!emaValue.IsFormed || !wmaValue.IsFormed)
			return;

		var currentEma = emaValue.ToDecimal();
		var currentWma = wmaValue.ToDecimal();

		if (_previousEma is decimal prevEma && _previousWma is decimal prevWma)
		{
			var buySignal = prevEma > prevWma && currentEma < currentWma;
			var sellSignal = prevEma < prevWma && currentEma > currentWma;

			if (buySignal)
				TryEnterLong();
			else if (sellSignal)
				TryEnterShort();
		}

		_previousEma = currentEma;
		_previousWma = currentWma;
	}

	private void TryEnterLong()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		// Close existing short exposure before opening a new long position.
		if (Position < 0m)
			BuyMarket(-Position);

		BuyMarket(volume);
	}

	private void TryEnterShort()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		// Close existing long exposure before opening a new short position.
		if (Position > 0m)
			SellMarket(Position);

		SellMarket(volume);
	}

	private decimal CalculateOrderVolume()
	{
		if (OrderVolume > 0m)
			return OrderVolume;

		if (RiskPercent <= 0m)
			return 0m;

		var stopDistance = GetStopDistance();
		if (stopDistance <= 0m)
			return 0m;

		var portfolio = Portfolio;
		if (portfolio is null)
			return 0m;

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		decimal perUnitRisk;
		if (priceStep > 0m && stepPrice > 0m)
		{
			perUnitRisk = stopDistance / priceStep * stepPrice;
		}
		else
		{
			// Fallback when exchange-specific step information is unavailable.
			perUnitRisk = stopDistance;
		}

		if (perUnitRisk <= 0m)
			return 0m;

		var riskAmount = equity * RiskPercent / 100m;
		if (riskAmount <= 0m)
			return 0m;

		var rawVolume = riskAmount / perUnitRisk;
		var volumeStep = Security?.VolumeStep ?? 0m;

		if (volumeStep > 0m)
		{
			var steps = Math.Max(1m, Math.Floor(rawVolume / volumeStep));
			return steps * volumeStep;
		}

		return Math.Max(rawVolume, 0m);
	}

	private decimal GetStopDistance()
	{
		if (StopLossPoints <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep > 0m)
			return StopLossPoints * priceStep;

		return StopLossPoints;
	}
}
