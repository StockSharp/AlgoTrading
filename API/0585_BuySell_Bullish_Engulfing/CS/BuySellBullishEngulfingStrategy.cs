using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades bullish engulfing pattern with optional trend filter.
/// </summary>
public class BuySellBullishEngulfingStrategy : Strategy
{
	private const int BodyEmaLength = 14;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _orderPercent;
	private readonly StrategyParam<TrendMode> _trendMode;

	private ICandleMessage _previousCandle;
	private decimal _bodyEma;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Order size as percent of equity.
	/// </summary>
	public decimal OrderPercent
	{
		get => _orderPercent.Value;
		set => _orderPercent.Value = value;
	}

	/// <summary>
	/// Trend detection rule.
	/// </summary>
	public TrendMode TrendMode
	{
		get => _trendMode.Value;
		set => _trendMode.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BuySellBullishEngulfingStrategy"/>.
	/// </summary>
	public BuySellBullishEngulfingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk Management")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
			.SetCanOptimize(true);

		_orderPercent = Param(nameof(OrderPercent), 30m)
			.SetRange(1m, 100m)
			.SetDisplay("Order Size %", "Percent of equity per trade", "Risk Management");

		_trendMode = Param(nameof(TrendMode), TrendMode.Sma50)
			.SetDisplay("Trend Rule", "How to detect trend", "Pattern");
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
		_bodyEma = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma50 = new SMA { Length = 50 };
		var sma200 = new SMA { Length = 200 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma50, sma200, ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma50);
			DrawIndicator(area, sma200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma50, decimal sma200)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		if (_bodyEma == 0m)
			_bodyEma = body;
		else
			_bodyEma += (body - _bodyEma) * 2m / (BodyEmaLength + 1m);

		var longBody = body > _bodyEma;

		if (_previousCandle != null && Position == 0)
		{
			var prevBody = Math.Abs(_previousCandle.ClosePrice - _previousCandle.OpenPrice);
			var prevSmall = prevBody < _bodyEma;
			var prevBear = _previousCandle.ClosePrice < _previousCandle.OpenPrice;
			var currBull = candle.ClosePrice > candle.OpenPrice;

			var downTrend = TrendMode switch
			{
				TrendMode.Sma50 => candle.ClosePrice < sma50,
				TrendMode.Sma50And200 => candle.ClosePrice < sma50 && sma50 < sma200,
				_ => true
			};

			var engulf = downTrend && currBull && longBody && prevBear && prevSmall &&
				candle.ClosePrice >= _previousCandle.OpenPrice &&
				candle.OpenPrice <= _previousCandle.ClosePrice &&
				(candle.ClosePrice > _previousCandle.OpenPrice || candle.OpenPrice < _previousCandle.ClosePrice);

			if (engulf)
			{
				var volume = CalculateVolume(candle.ClosePrice);
				BuyMarket(volume);
			}
		}

		_previousCandle = candle;
	}

	private decimal CalculateVolume(decimal price)
	{
		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var size = portfolioValue * (OrderPercent / 100m) / price;
		return size > 0 ? size : Volume;
	}
}

/// <summary>
/// Trend detection modes.
/// </summary>
public enum TrendMode
{
	/// <summary>
	/// Price below SMA50.
	/// </summary>
	Sma50,

	/// <summary>
	/// Price below SMA50 and SMA50 below SMA200.
	/// </summary>
	Sma50And200,

	/// <summary>
	/// No trend check.
	/// </summary>
	None
}
