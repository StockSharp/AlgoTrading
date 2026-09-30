using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades when price deviates significantly from its moving average.
/// Opens positions when price deviates by a specified percentage from MA
/// and closes when price returns to MA.
/// </summary>
public class MADeviationStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _deviationPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskPercent;

	private Order _pendingOrder;
	private Unit _stopDistance;
	private bool _protectionStarted;

	/// <summary>
	/// Period for Moving Average calculation.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Deviation percentage from MA required for entry.
	/// </summary>
	public decimal DeviationPercent
	{
		get => _deviationPercent.Value;
		set => _deviationPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the MA Deviation strategy.
	/// </summary>
	public MADeviationStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 5);

		_deviationPercent = Param(nameof(DeviationPercent), 5m).SetGreaterThanZero()
			.SetDisplay("Deviation %", "Deviation percentage from MA required for entry", "Entry")
			.SetOptimize(1m, 10m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR lookback for sizing and protection.", "Protection");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Multiplier", "Frozen entry ATR distance; zero disables stop and ATR sizing.", "Protection");
		_riskPercent = Param(nameof(RiskPercent), 1m).SetGreaterThanZero()
			.SetDisplay("Risk (%)", "Portfolio value percentage budgeted against the entry ATR stop.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pendingOrder = null;
		_stopDistance = null;
		_protectionStarted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, atr, ProcessCandle, false)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before the callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFormed || !atrValue.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		var mean = maValue.GetValue<decimal>();
		if (mean <= 0m)
			return;
		var deviation = 100m * (candle.ClosePrice - mean) / mean;
		if (Position > 0m && candle.ClosePrice >= mean)
			SellMarket(Position);
		else if (Position < 0m && candle.ClosePrice <= mean)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && Math.Abs(deviation) > DeviationPercent)
			Enter(deviation < 0m ? Sides.Buy : Sides.Sell, candle.ClosePrice, atrValue.GetValue<decimal>());
	}

	private void Enter(Sides side, decimal close, decimal atr)
	{
		var distance = atr * AtrMultiplier;
		var volume = CalculateVolume(close, distance);
		if (volume <= 0m)
			return;
		_stopDistance ??= new Unit(distance);
		_stopDistance.Value = distance;
		if (!_protectionStarted && distance > 0m)
		{
			StartProtection(new Unit(), _stopDistance, useMarketOrders: true, isLocalStop: true);
			_protectionStarted = true;
		}
		RegisterOrder(new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = volume,
			Comment = "MA deviation entry",
		});
	}

	private decimal CalculateVolume(decimal close, decimal distance)
	{
		if (AtrMultiplier == 0m)
			return NormalizeVolume(Volume);
		var balance = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
		var stepPrice = Security.StepPrice ?? 0m;
		var priceStep = Security.PriceStep ?? 0m;
		var moneyFactor = stepPrice > 0m && priceStep > 0m ? stepPrice / priceStep : Security.Multiplier ?? 1m;
		if (balance <= 0m || distance <= 0m || close <= 0m || moneyFactor <= 0m)
			return 0m;
		var budget = balance * RiskPercent / 100m;
		var riskVolume = budget / (distance * moneyFactor);
		var cashVolume = balance / (close * moneyFactor);
		return NormalizeVolume(Math.Min(riskVolume, cashVolume));
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var minimum = Math.Max(0m, Security.MinVolume ?? 0m);
		var maximum = Security.MaxVolume is decimal max && max > 0m ? max : decimal.MaxValue;
		volume = Math.Min(volume, maximum);
		if (Security.VolumeStep is decimal step && step > 0m)
		{
			minimum = Math.Ceiling(minimum / step) * step;
			if (maximum != decimal.MaxValue) maximum = Math.Floor(maximum / step) * step;
			volume = Math.Floor(volume / step) * step;
		}
		// Never round UP through the risk budget to force a minimum-size trade.
		return minimum <= maximum && volume >= minimum ? volume : 0m;
	}
}
