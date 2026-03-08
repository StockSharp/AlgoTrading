using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MACD histogram direction, WilliamsR, RSI and MA trend.
/// Buys when MACD is rising, MA is trending up, WPR and RSI confirm momentum.
/// </summary>
public class SmartAssTradeV2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd;
	private SimpleMovingAverage _ma;
	private WilliamsR _wpr;
	private RelativeStrengthIndex _rsi;

	private decimal? _prevMacd;
	private decimal? _prevMa;
	private decimal? _prevWpr;
	private decimal? _prevRsi;

	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SmartAssTradeV2Strategy()
	{
		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_prevMacd = null;
		_prevMa = null;
		_prevWpr = null;
		_prevRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd = new MovingAverageConvergenceDivergence();
		_ma = new SimpleMovingAverage { Length = 20 };
		_wpr = new WilliamsR { Length = 26 };
		_rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdResult = _macd.Process(candle.ClosePrice, candle.OpenTime, true);
		var maResult = _ma.Process(candle.ClosePrice, candle.OpenTime, true);
		var wprResult = _wpr.Process(candle);
		var rsiResult = _rsi.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!macdResult.IsFormed || !maResult.IsFormed || !wprResult.IsFormed || !rsiResult.IsFormed)
			return;

		var currMacd = macdResult.ToDecimal();
		var currMa = maResult.ToDecimal();
		var currWpr = wprResult.ToDecimal();
		var currRsi = rsiResult.ToDecimal();

		if (_prevMacd == null || _prevMa == null || _prevWpr == null || _prevRsi == null)
		{
			_prevMacd = currMacd;
			_prevMa = currMa;
			_prevWpr = currWpr;
			_prevRsi = currRsi;
			return;
		}

		var macdRising = currMacd > _prevMacd;
		var macdFalling = currMacd < _prevMacd;
		var maRising = currMa > _prevMa;
		var maFalling = currMa < _prevMa;

		// Buy signal: MACD rising, MA rising, WPR recovering, RSI rising below 70
		var buySignal = macdRising && maRising
			&& currWpr > _prevWpr && currRsi > _prevRsi
			&& currRsi < 70m;

		// Sell signal: MACD falling, MA falling, WPR declining, RSI falling above 30
		var sellSignal = macdFalling && maFalling
			&& currWpr < _prevWpr && currRsi < _prevRsi
			&& currRsi > 30m;

		if (Position <= 0 && buySignal)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (Position >= 0 && sellSignal)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevMacd = currMacd;
		_prevMa = currMa;
		_prevWpr = currWpr;
		_prevRsi = currRsi;
	}
}
