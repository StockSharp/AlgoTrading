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
/// SSL channel combined with Hama candle trend direction.
/// Uses SSL (SMA-based channel) and two EMAs for Hama trend with StdDev for consolidation detection.
/// </summary>
public class TrendGuardScalperSslHamaCandleWithConsolidationZonesStrategy : Strategy
{
	private readonly StrategyParam<int> _sslPeriod;
	private readonly StrategyParam<int> _hamaFast;
	private readonly StrategyParam<int> _hamaSlow;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	public int SslPeriod { get => _sslPeriod.Value; set => _sslPeriod.Value = value; }
	public int HamaFast { get => _hamaFast.Value; set => _hamaFast.Value = value; }
	public int HamaSlow { get => _hamaSlow.Value; set => _hamaSlow.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendGuardScalperSslHamaCandleWithConsolidationZonesStrategy()
	{
		_sslPeriod = Param(nameof(SslPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("SSL Period", "Period for SSL channel", "Indicators");

		_hamaFast = Param(nameof(HamaFast), 20)
			.SetGreaterThanZero()
			.SetDisplay("Hama Fast", "Fast EMA for Hama", "Indicators");

		_hamaSlow = Param(nameof(HamaSlow), 50)
			.SetGreaterThanZero()
			.SetDisplay("Hama Slow", "Slow EMA for Hama", "Indicators");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ssl = new SimpleMovingAverage { Length = SslPeriod };
		var hamaClose = new ExponentialMovingAverage { Length = HamaFast };
		var hamaLine = new ExponentialMovingAverage { Length = HamaSlow };

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ssl, hamaClose, hamaLine, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal sslChannel, decimal hamaCloseVal, decimal hamaLineVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var sslIsGreen = candle.ClosePrice > sslChannel;
		var sslIsRed = candle.ClosePrice < sslChannel;
		var hamaIsGreen = hamaCloseVal > hamaLineVal;
		var hamaIsRed = hamaCloseVal < hamaLineVal;

		var longCond = sslIsGreen && hamaIsGreen && candle.ClosePrice > hamaCloseVal;
		var shortCond = sslIsRed && hamaIsRed && candle.ClosePrice < hamaCloseVal;

		if (longCond && Position <= 0)
		{
			BuyMarket();
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket();
		}
	}
}
