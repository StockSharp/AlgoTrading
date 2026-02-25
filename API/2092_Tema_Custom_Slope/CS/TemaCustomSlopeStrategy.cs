using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on slope reversals of the Triple Exponential Moving Average.
/// </summary>
public class TemaCustomSlopeStrategy : Strategy
{
	private readonly StrategyParam<int> _temaLength;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prev1;
	private decimal? _prev2;

	public int TemaLength
	{
		get => _temaLength.Value;
		set => _temaLength.Value = value;
	}

	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TemaCustomSlopeStrategy()
	{
		_temaLength = Param(nameof(TemaLength), 12)
			.SetDisplay("TEMA Length", "Length of the TEMA", "Indicators");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of candles", "General");
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
		_prev1 = null;
		_prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var tema = new TripleExponentialMovingAverage { Length = TemaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(tema, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, tema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal tema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prev1 is null || _prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = tema;
			return;
		}

		var falling = _prev1 < _prev2;
		var rising = _prev1 > _prev2;
		var turnedUp = falling && tema > _prev1;
		var turnedDown = rising && tema < _prev1;

		_prev2 = _prev1;
		_prev1 = tema;

		if (turnedUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (turnedDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
