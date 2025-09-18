using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI alert strategy converted from the original MetaTrader expert advisor.
/// Buys when RSI drops below the oversold threshold and sells when RSI rises above the overbought threshold.
/// </summary>
public class RsiAlertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex? _rsi;

	/// <summary>
	/// Order volume used for market trades.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			Volume = value; // Keep the base strategy volume aligned with the parameter value.
		}
	}

	/// <summary>
	/// RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level that triggers short trades.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// RSI level that triggers long trades.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Candle type supplying prices to the RSI indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RsiAlertStrategy"/> with default parameters.
	/// </summary>
	public RsiAlertStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Order size used for market trades", "Trading")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Number of bars for RSI calculation", "Indicator")
			.SetCanOptimize(true);

		_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
			.SetDisplay("Overbought Level", "RSI threshold that triggers short signals", "Indicator")
			.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetDisplay("Oversold Level", "RSI threshold that triggers long signals", "Indicator")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles that feed the RSI", "General");

		Volume = OrderVolume;
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

		_rsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume; // Ensure the strategy uses the configured volume on each start.

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsi = _rsi;
		if (rsi is null || !rsi.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var buySignal = rsiValue <= OversoldLevel;
		var sellSignal = rsiValue >= OverboughtLevel;

		if (buySignal && Position <= 0)
		{
			var volume = OrderVolume + (Position < 0 ? Math.Abs(Position) : 0m);

			// Reverse any short position and enter a long trade when RSI is oversold.
			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (sellSignal && Position >= 0)
		{
			var volume = OrderVolume + (Position > 0 ? Position : 0m);

			// Reverse any long position and enter a short trade when RSI is overbought.
			if (volume > 0m)
				SellMarket(volume);
		}
	}
}
