using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on price momentum percentage change.
/// Uses percentage RateOfChange with an SMA trend filter and actual-fill percent protection.
/// Trades signed percentage threshold crossings, completely reversing on a counter signal.
/// </summary>
public class MomentumPercentageStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _thresholdPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private decimal _prevMom;
	private bool _hasPrevValues;
	private Order _pendingOrder;

	/// <summary>
	/// Momentum period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal ThresholdPercent { get => _thresholdPercent.Value; set => _thresholdPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MomentumPercentageStrategy"/>.
	/// </summary>
	public MomentumPercentageStrategy()
	{
		_momentumPeriod = Param(nameof(MomentumPeriod), 10)
			.SetDisplay("Momentum Period", "Period for momentum calculation", "Indicators")
			.SetOptimize(8, 20, 4);

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetDisplay("SMA Period", "Period for SMA trend filter", "Indicators")
			.SetOptimize(15, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_thresholdPercent = Param(nameof(ThresholdPercent), 5m).SetGreaterThanZero()
			.SetDisplay("Threshold (%)", "Symmetric percentage return breakout level.", "Signal");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMom = default;
		_hasPrevValues = default;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var momentum = new RateOfChange { Length = MomentumPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(momentum, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, momentum);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before the callback, including between bars.
	}

	private void ProcessCandle(ICandleMessage candle, decimal momValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (smaValue == 0)
			return;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevMom = momValue;
			return;
		}

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
		{
			_prevMom = momValue;
			return;
		}

		var price = candle.ClosePrice;

		// Cross the configured positive percent return with an SMA confirmation.
		if (_prevMom <= ThresholdPercent && momValue > ThresholdPercent && price > smaValue && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		// A confirmed opposite threshold reverses the complete position, not just its default volume.
		else if (_prevMom >= -ThresholdPercent && momValue < -ThresholdPercent && price < smaValue && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevMom = momValue;
	}
}
