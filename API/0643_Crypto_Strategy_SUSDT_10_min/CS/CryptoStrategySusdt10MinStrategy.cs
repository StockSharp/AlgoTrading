using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crypto Strategy SUSDT 10 min - EMA based long/short strategy with fixed risk parameters.
/// </summary>
public class CryptoStrategySusdt10MinStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _orderPercent;

	private ExponentialMovingAverage _ema;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
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
	/// Initializes a new instance of <see cref="CryptoStrategySusdt10MinStrategy"/>.
	/// </summary>
	public CryptoStrategySusdt10MinStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_emaLength = Param(nameof(EmaLength), 24)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicator")
			.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetRange(0.1m, 20m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk Management")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.1m, 20m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
			.SetCanOptimize(true);

		_orderPercent = Param(nameof(OrderPercent), 30m)
			.SetRange(1m, 100m)
			.SetDisplay("Order Size %", "Percent of equity per trade", "Risk Management");
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

		_ema = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		if (close > emaValue && open < emaValue && Position <= 0)
		{
			var volume = CalculateVolume(close);
			BuyMarket(volume);
		}
		else if (close < emaValue && open > emaValue && Position >= 0)
		{
			var volume = CalculateVolume(close);
			SellMarket(volume);
		}
	}

	private decimal CalculateVolume(decimal price)
	{
		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var size = portfolioValue * (OrderPercent / 100m) / price;
		return size > 0 ? size : Volume;
	}
}
