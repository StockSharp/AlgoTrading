using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining RSI with moving average trend filter.
/// Buys when RSI is below the buy level and fast MA is above slow MA.
/// Sells when RSI is above the sell level and fast MA is below slow MA.
/// </summary>
public class RsiMaTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level to trigger buy.
	/// </summary>
	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// RSI level to trigger sell.
	/// </summary>
	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RsiMaTrendStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of RSI indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 30m)
			.SetDisplay("RSI Buy Level", "Value below which long is opened", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_rsiSellLevel = Param(nameof(RsiSellLevel), 70m)
			.SetDisplay("RSI Sell Level", "Value above which short is opened", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Length of fast moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 10);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Length of slow moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 20);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var fastMa = new SMA { Length = FastMaPeriod };
		var slowMa = new SMA { Length = SlowMaPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal fastMaValue, decimal slowMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isUpTrend = fastMaValue > slowMaValue;

		if (rsiValue < RsiBuyLevel && isUpTrend && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (rsiValue > RsiSellLevel && !isUpTrend && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
