using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX based long-only strategy for BTC.
/// </summary>
public class AdxForBtcStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryLevel;
	private readonly StrategyParam<decimal> _exitLevel;
	private readonly StrategyParam<bool> _smaFilter;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAdx;

	public decimal EntryLevel { get => _entryLevel.Value; set => _entryLevel.Value = value; }
	public decimal ExitLevel { get => _exitLevel.Value; set => _exitLevel.Value = value; }
	public bool SmaFilter { get => _smaFilter.Value; set => _smaFilter.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AdxForBtcStrategy()
	{
		_entryLevel = Param(nameof(EntryLevel), 14m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Level", "ADX threshold for entry", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10m, 30m, 5m);

		_exitLevel = Param(nameof(ExitLevel), 45m)
			.SetGreaterThanZero()
			.SetDisplay("Exit Level", "ADX threshold for exit", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20m, 60m, 5m);

		_smaFilter = Param(nameof(SmaFilter), true)
			.SetDisplay("SMA Filter", "Enable SMA trend filter", "Strategy Parameters");

		_smaLength = Param(nameof(SmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Length for fast SMA", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_prevAdx = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var adx = new AverageDirectionalIndex { Length = 14 };
		var smaFast = new SimpleMovingAverage { Length = SmaLength };
		var smaSlow = new SimpleMovingAverage { Length = SmaLength * 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(adx, smaFast, smaSlow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, smaFast);
			DrawIndicator(area, smaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal adxValue, decimal smaFastValue, decimal smaSlowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevAdx <= EntryLevel && adxValue > EntryLevel)
		{
			var trendOk = !SmaFilter || smaFastValue > smaSlowValue;
			if (trendOk && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0 && _prevAdx >= ExitLevel && adxValue < ExitLevel)
			SellMarket(Math.Abs(Position));

		_prevAdx = adxValue;
	}
}