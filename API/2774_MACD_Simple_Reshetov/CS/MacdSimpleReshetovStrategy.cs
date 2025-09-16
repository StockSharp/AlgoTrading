using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD-based strategy adapted from Yury Reshetov's MACDSimple expert advisor.
/// </summary>
public class MacdSimpleReshetovStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _df;
	private readonly StrategyParam<int> _ds;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private MACD _macd;

	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public int Df { get => _df.Value; set => _df.Value = value; }
	public int Ds { get => _ds.Value; set => _ds.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdSimpleReshetovStrategy()
	{
		_volume = Param(nameof(Volume), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_df = Param(nameof(Df), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("DF", "Offset for the fast EMA", "Indicators");

		_ds = Param(nameof(Ds), 2)
			.SetGreaterOrEqualZero()
			.SetDisplay("DS", "Offset for the slow EMA", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// The MQL version derives MACD periods from the signal period with DF and DS offsets.
		var fastPeriod = SignalPeriod + Df;
		var slowPeriod = SignalPeriod + Ds + Df;

		_macd = new MACD
		{
			ShortPeriod = fastPeriod,
			LongPeriod = slowPeriod,
			SignalPeriod = SignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_macd, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal signalLine)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed)
			return;

		// Manage existing positions before evaluating new signals.
		if (Position > 0)
		{
			if (macdLine < 0m)
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if (macdLine > 0m)
				BuyMarket(-Position);

			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Enter only when MACD and signal lines share the same sign.
		if (macdLine * signalLine <= 0m)
			return;

		if (macdLine > 0m && macdLine > signalLine)
		{
			BuyMarket(Volume);
		}
		else if (macdLine < 0m && macdLine < signalLine)
		{
			SellMarket(Volume);
		}
	}
}
