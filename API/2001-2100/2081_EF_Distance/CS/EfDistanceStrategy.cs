using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EF Distance reversal strategy.
/// Uses a smoothed price series and ATR filter to trade turning points.
/// </summary>
public class EfDistanceStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prev;
	private decimal? _prev2;

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EfDistanceStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Period for the smoothing moving average", "Indicator");

		_atrPeriod = Param(nameof(AtrPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for the volatility filter", "Indicator");

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.5m)
			.SetDisplay("ATR Multiplier", "Minimum ATR relative to price", "Indicator");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 4m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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
		_prev = null;
		_prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = SmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(ema, atr, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaVal, IIndicatorValue atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!emaVal.IsFormed || !atrVal.IsFormed)
			return;

		var smaValue = emaVal.ToDecimal();
		var atrValue = atrVal.ToDecimal();

		if (_prev is null || _prev2 is null)
		{
			_prev2 = _prev;
			_prev = smaValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atrEnough = atrValue >= AtrMultiplier / 100m * candle.ClosePrice;

		if (atrEnough)
		{
			if (_prev < _prev2 && smaValue > _prev && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (_prev > _prev2 && smaValue < _prev && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prev2 = _prev;
		_prev = smaValue;
	}
}
