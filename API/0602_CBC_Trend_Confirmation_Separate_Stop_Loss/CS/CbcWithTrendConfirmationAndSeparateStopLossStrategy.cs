using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CBC strategy with trend confirmation and separate stop loss.
/// Flips state on break of previous candle and confirms trend using EMA and VWAP.
/// Uses ATR-based profit target and previous candle levels for stop loss.
/// </summary>
public class CbcWithTrendConfirmationAndSeparateStopLossStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _profitTargetMultiplier;
	private readonly StrategyParam<bool> _strongFlipsOnly;
	private readonly StrategyParam<int> _entryStartHour;
	private readonly StrategyParam<int> _entryEndHour;
	private readonly StrategyParam<DataType> _candleType;

	private bool _cbc;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;
	private decimal _takeProfit;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Profit target multiplier.
	/// </summary>
	public decimal ProfitTargetMultiplier { get => _profitTargetMultiplier.Value; set => _profitTargetMultiplier.Value = value; }

	/// <summary>
	/// Require strong flips only.
	/// </summary>
	public bool StrongFlipsOnly { get => _strongFlipsOnly.Value; set => _strongFlipsOnly.Value = value; }

	/// <summary>
	/// Entry start hour.
	/// </summary>
	public int EntryStartHour { get => _entryStartHour.Value; set => _entryStartHour.Value = value; }

	/// <summary>
	/// Entry end hour.
	/// </summary>
	public int EntryEndHour { get => _entryEndHour.Value; set => _entryEndHour.Value = value; }

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CbcWithTrendConfirmationAndSeparateStopLossStrategy"/> class.
	/// </summary>
	public CbcWithTrendConfirmationAndSeparateStopLossStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR calculation period", "Indicators")
			.SetCanOptimize(true);

		_profitTargetMultiplier = Param(nameof(ProfitTargetMultiplier), 1m)
			.SetDisplay("Profit Target Multiplier", "ATR multiplier for take profit", "Risk Management")
			.SetCanOptimize(true);

		_strongFlipsOnly = Param(nameof(StrongFlipsOnly), true)
			.SetDisplay("Strong Flips Only", "Enable strong flips filter", "General");

		_entryStartHour = Param(nameof(EntryStartHour), 10)
			.SetDisplay("Entry Start Hour", "Start hour for entries", "Time")
			.SetRange(0, 23);

		_entryEndHour = Param(nameof(EntryEndHour), 15)
			.SetDisplay("Entry End Hour", "No entries after this hour", "Time")
			.SetRange(0, 23);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_cbc = false;
		_prevHigh = 0m;
		_prevLow = 0m;
		_hasPrev = false;
		_takeProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new ExponentialMovingAverage { Length = 10 };
		var slowEma = new ExponentialMovingAverage { Length = 20 };
		var vwap = new VolumeWeightedMovingAverage { Length = 20 };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, vwap, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal vwap, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_hasPrev = true;
			return;
		}

		var prevCbc = _cbc;

		if (_cbc && candle.ClosePrice < _prevLow)
			_cbc = false;
		else if (!_cbc && candle.ClosePrice > _prevHigh)
			_cbc = true;

		var flippedToLong = _cbc && !prevCbc;
		var flippedToShort = !_cbc && prevCbc;
		var hour = candle.OpenTime.Hour;
		var inTimeRange = hour >= EntryStartHour && hour < EntryEndHour;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			var longCondition = inTimeRange && slowEma >= vwap && flippedToLong && (!StrongFlipsOnly || candle.LowPrice < _prevLow);
			var shortCondition = inTimeRange && slowEma < vwap && flippedToShort && (!StrongFlipsOnly || candle.HighPrice > _prevHigh);

			if (longCondition && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_takeProfit = candle.ClosePrice + atr * ProfitTargetMultiplier;
			}
			else if (shortCondition && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_takeProfit = candle.ClosePrice - atr * ProfitTargetMultiplier;
			}

			if (Position > 0)
			{
				if (candle.ClosePrice <= _prevLow || (_takeProfit != 0m && candle.HighPrice >= _takeProfit))
				{
					ClosePosition();
					_takeProfit = 0m;
				}
			}
			else if (Position < 0)
			{
				if (candle.ClosePrice >= _prevHigh || (_takeProfit != 0m && candle.LowPrice <= _takeProfit))
				{
					ClosePosition();
					_takeProfit = 0m;
				}
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
