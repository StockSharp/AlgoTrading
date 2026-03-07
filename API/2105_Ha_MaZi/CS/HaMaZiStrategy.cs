using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heikin Ashi with moving average and ZigZag-style pivot confirmation.
/// </summary>
public class HaMaZiStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _zigzagLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;

	private Highest _highest;
	private Lowest _lowest;
	private decimal _haOpenPrev;
	private decimal _haClosePrev;
	private decimal _lastZigzag;
	private decimal _lastZigzagHigh;
	private decimal _lastZigzagLow;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int ZigzagLength { get => _zigzagLength.Value; set => _zigzagLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }

	public HaMaZiStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 40)
			.SetDisplay("MA Period", "EMA period", "General");
		_zigzagLength = Param(nameof(ZigzagLength), 13)
			.SetDisplay("ZigZag Length", "Lookback for pivot search", "ZigZag");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");
		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highest = default;
		_lowest = default;
		_haOpenPrev = 0; _haClosePrev = 0;
		_lastZigzag = 0; _lastZigzagHigh = 0; _lastZigzagLow = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		_highest = new Highest { Length = ZigzagLength };
		_lowest = new Lowest { Length = ZigzagLength };

		Indicators.Add(_highest);
		Indicators.Add(_lowest);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, (candle, emaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var highResult = _highest.Process(candle);
				var lowResult = _lowest.Process(candle);
				if (!highResult.IsFormed || !lowResult.IsFormed)
					return;

				var highest = highResult.ToDecimal();
				var lowest = lowResult.ToDecimal();

				var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
				var haOpen = (_haOpenPrev == 0 && _haClosePrev == 0)
					? (candle.OpenPrice + candle.ClosePrice) / 2m
					: (_haOpenPrev + _haClosePrev) / 2m;
				var haBull = haClose > haOpen;
				var haBear = haClose < haOpen;

				if (candle.HighPrice >= highest && _lastZigzag != candle.HighPrice)
				{
					_lastZigzag = candle.HighPrice;
					_lastZigzagHigh = candle.HighPrice;
					_lastZigzagLow = 0;
				}
				else if (candle.LowPrice <= lowest && _lastZigzag != candle.LowPrice)
				{
					_lastZigzag = candle.LowPrice;
					_lastZigzagLow = candle.LowPrice;
					_lastZigzagHigh = 0;
				}

				if (_lastZigzag == _lastZigzagLow && _lastZigzagLow > 0 && haBull && candle.ClosePrice > emaVal && Position <= 0)
				{
					if (Position < 0) BuyMarket();
					BuyMarket();
				}
				else if (_lastZigzag == _lastZigzagHigh && _lastZigzagHigh > 0 && haBear && candle.ClosePrice < emaVal && Position >= 0)
				{
					if (Position > 0) SellMarket();
					SellMarket();
				}

				_haOpenPrev = haOpen;
				_haClosePrev = haClose;
			})
			.Start();

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
}
