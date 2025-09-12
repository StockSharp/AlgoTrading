using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SSL channel combined with Hama candle trend direction.
/// </summary>
public class TrendGuardScalperSslHamaCandleWithConsolidationZonesStrategy : Strategy
{
	private readonly StrategyParam<int> _sslPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrThreshold;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// SSL period.
	/// </summary>
	public int SslPeriod
	{
		get => _sslPeriod.Value;
		set => _sslPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for consolidation detection.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR to price threshold to mark consolidation.
	/// </summary>
	public decimal AtrThreshold
	{
		get => _atrThreshold.Value;
		set => _atrThreshold.Value = value;
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrendGuardScalperSslHamaCandleWithConsolidationZonesStrategy"/> class.
	/// </summary>
	public TrendGuardScalperSslHamaCandleWithConsolidationZonesStrategy()
	{
		_sslPeriod = Param(nameof(SslPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("SSL Period", "Period for SSL channel", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR period for consolidation", "Indicators");

		_atrThreshold = Param(nameof(AtrThreshold), 0.003m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Threshold", "ATR/price threshold", "Indicators");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		var ssl = new SimpleMovingAverage { Length = SslPeriod };
		var hamaClose = new ExponentialMovingAverage { Length = 20 };
		var hamaLine = new ExponentialMovingAverage { Length = 100 };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		StartProtection(
		new Unit(TakeProfitPercent, UnitTypes.Percent),
		new Unit(StopLossPercent, UnitTypes.Percent),
		useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ssl, hamaClose, hamaLine, atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ssl);
			DrawIndicator(area, hamaClose);
			DrawIndicator(area, hamaLine);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sslChannel, decimal hamaClose, decimal hamaLine, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var sslIsGreen = candle.ClosePrice > sslChannel;
		var sslIsRed = candle.ClosePrice < sslChannel;
		var hamaIsGreen = hamaClose > hamaLine;
		var hamaIsRed = hamaClose < hamaLine;

		var longCond = sslIsGreen && hamaIsGreen && candle.ClosePrice > hamaClose;
		var shortCond = sslIsRed && hamaIsRed && candle.ClosePrice < hamaClose;

		var isConsolidation = candle.ClosePrice != 0m && atrValue / candle.ClosePrice < AtrThreshold;
		// isConsolidation is calculated for analysis purposes only.

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var volume = Volume + Math.Abs(Position);

		if (longCond && Position <= 0)
		{
			BuyMarket(volume);
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket(volume);
		}
	}
}
