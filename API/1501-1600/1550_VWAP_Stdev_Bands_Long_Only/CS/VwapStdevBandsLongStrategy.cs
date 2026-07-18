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
/// VWAP strategy with standard deviation bands, long only.
/// Calculates intraday VWAP and standard deviation bands.
/// Buys when price crosses below lower band, sells at profit target.
/// </summary>
public class VwapStdevBandsLongStrategy : Strategy
{
	private readonly StrategyParam<decimal> _devDown;
	private readonly StrategyParam<decimal> _profitPct;
	private readonly StrategyParam<int> _gapMinutes;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _sessionDate;
	private decimal _vwapSum;
	private decimal _volSum;
	private decimal _v2Sum;
	private decimal _prevClose;
	private decimal _prevLower;
	private bool _hasPrev;
	private decimal _lastEntryPrice;
	private DateTimeOffset? _lastEntryTime;

	public decimal DevDown { get => _devDown.Value; set => _devDown.Value = value; }
	public decimal ProfitPct { get => _profitPct.Value; set => _profitPct.Value = value; }
	public int GapMinutes { get => _gapMinutes.Value; set => _gapMinutes.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VwapStdevBandsLongStrategy()
	{
		_devDown = Param(nameof(DevDown), 1.28m)
			.SetGreaterThanZero()
			.SetDisplay("Stdev Down", "Std dev below VWAP", "Parameters");

		_profitPct = Param(nameof(ProfitPct), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Profit %", "Profit target percent", "Parameters");

		_gapMinutes = Param(nameof(GapMinutes), 15)
			.SetDisplay("Gap Minutes", "Gap before new order", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_sessionDate = default;
		_vwapSum = 0;
		_volSum = 0;
		_v2Sum = 0;
		_prevClose = 0;
		_prevLower = 0;
		_hasPrev = false;
		_lastEntryPrice = 0;
		_lastEntryTime = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 2 };

		_sessionDate = default;
		_vwapSum = 0;
		_volSum = 0;
		_v2Sum = 0;
		_prevClose = 0;
		_prevLower = 0;
		_hasPrev = false;
		_lastEntryPrice = 0;
		_lastEntryTime = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _dummy)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;
		var volume = candle.TotalVolume;
		var price = (candle.HighPrice + candle.LowPrice) / 2m;

		if (date != _sessionDate)
		{
			_sessionDate = date;
			_vwapSum = price * volume;
			_volSum = volume;
			_v2Sum = volume * price * price;
			_hasPrev = false;
		}
		else
		{
			_vwapSum += price * volume;
			_volSum += volume;
			_v2Sum += volume * price * price;
		}

		if (_volSum == 0)
			return;

		var vwap = _vwapSum / _volSum;
		var variance = _v2Sum / _volSum - vwap * vwap;
		var dev = (decimal)Math.Sqrt((double)Math.Max(variance, 0m));
		var lower = vwap - DevDown * dev;

		var canEnter = !_lastEntryTime.HasValue || candle.OpenTime - _lastEntryTime >= TimeSpan.FromMinutes(GapMinutes);
		var crossedLower = _hasPrev && _prevClose >= _prevLower && candle.ClosePrice < lower;

		if (crossedLower && canEnter && Position <= 0)
		{
			BuyMarket();
			_lastEntryPrice = candle.ClosePrice;
			_lastEntryTime = candle.OpenTime;
		}

		// Profit target exit
		if (Position > 0 && _lastEntryPrice > 0)
		{
			var target = _lastEntryPrice * (1 + ProfitPct / 100m);
			if (candle.ClosePrice >= target)
			{
				SellMarket();
				_lastEntryTime = null;
				_lastEntryPrice = 0;
			}
		}

		_prevClose = candle.ClosePrice;
		_prevLower = lower;
		_hasPrev = true;
	}
}
