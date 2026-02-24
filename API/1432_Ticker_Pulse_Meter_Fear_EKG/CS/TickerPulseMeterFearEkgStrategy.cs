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
/// Ticker Pulse Meter + Fear EKG strategy.
/// Uses short and long Highest/Lowest lookbacks to find oversold zones.
/// </summary>
public class TickerPulseMeterFearEkgStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackShort;
	private readonly StrategyParam<int> _lookbackLong;
	private readonly StrategyParam<int> _profitTake;
	private readonly StrategyParam<int> _entryThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevPctCombo;

	public int LookbackShort { get => _lookbackShort.Value; set => _lookbackShort.Value = value; }
	public int LookbackLong { get => _lookbackLong.Value; set => _lookbackLong.Value = value; }
	public int ProfitTake { get => _profitTake.Value; set => _profitTake.Value = value; }
	public int EntryThreshold { get => _entryThreshold.Value; set => _entryThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TickerPulseMeterFearEkgStrategy()
	{
		_lookbackShort = Param(nameof(LookbackShort), 50)
			.SetGreaterThanZero();
		_lookbackLong = Param(nameof(LookbackLong), 200)
			.SetGreaterThanZero();
		_profitTake = Param(nameof(ProfitTake), 90)
			.SetGreaterThanZero();
		_entryThreshold = Param(nameof(EntryThreshold), 20)
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevPctCombo = 50m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var shortHigh = new Highest { Length = LookbackShort };
		var shortLow = new Lowest { Length = LookbackShort };
		var longHigh = new Highest { Length = LookbackLong };
		var longLow = new Lowest { Length = LookbackLong };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortHigh, shortLow, longHigh, longLow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortH, decimal shortL, decimal longH, decimal longL)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var denomShort = shortH - shortL;
		var denomLong = longH - longL;
		if (denomShort <= 0 || denomLong <= 0)
			return;

		var pctAboveShort = (candle.ClosePrice - shortL) / denomShort;
		var pctAboveLong = (candle.ClosePrice - longL) / denomLong;

		var pctCombo = pctAboveLong * pctAboveShort * 100m;

		// Check exits first
		if (Position > 0 && _prevPctCombo >= ProfitTake && pctCombo < ProfitTake)
		{
			SellMarket();
			_prevPctCombo = pctCombo;
			return;
		}

		// Entry: oversold bounce -- combo crosses up through threshold
		if (Position == 0 && _prevPctCombo <= EntryThreshold && pctCombo > EntryThreshold)
		{
			BuyMarket();
		}
		// Also sell short when overbought breaks down
		else if (Position == 0 && _prevPctCombo >= (100 - EntryThreshold) && pctCombo < (100 - EntryThreshold))
		{
			SellMarket();
		}
		// Exit short
		else if (Position < 0 && pctCombo <= EntryThreshold)
		{
			BuyMarket();
		}

		_prevPctCombo = pctCombo;
	}
}
