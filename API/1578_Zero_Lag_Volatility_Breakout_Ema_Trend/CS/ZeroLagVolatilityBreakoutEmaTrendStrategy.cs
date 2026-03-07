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
/// Zero-lag volatility breakout strategy with EMA trend filter.
/// Uses Bollinger Bands on price-EMA divergence to detect breakouts.
/// </summary>
public class ZeroLagVolatilityBreakoutEmaTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _stdMultiplier;
	private readonly StrategyParam<bool> _useBinary;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevDif;
	private bool _hasPrev;

	private readonly List<decimal> _difs = new();

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public decimal StdMultiplier { get => _stdMultiplier.Value; set => _stdMultiplier.Value = value; }
	public bool UseBinary { get => _useBinary.Value; set => _useBinary.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZeroLagVolatilityBreakoutEmaTrendStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 50).SetDisplay("EMA Length", "Base EMA length", "Indicators");
		_stdMultiplier = Param(nameof(StdMultiplier), 2m).SetDisplay("Std Mult", "Standard deviation multiplier", "Indicators");
		_useBinary = Param(nameof(UseBinary), true).SetDisplay("Use Binary", "Hold until opposite signal", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = 0;
		_prevDif = 0;
		_hasPrev = false;
		_difs.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		_prevEma = 0;
		_prevDif = 0;
		_hasPrev = false;
		_difs.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hJumper = Math.Max(candle.ClosePrice, emaValue);
		var lJumper = Math.Min(candle.ClosePrice, emaValue);
		var dif = lJumper == 0 ? 0 : (hJumper / lJumper) - 1m;

		_difs.Add(dif);
		if (_difs.Count > EmaLength + 10)
			_difs.RemoveAt(0);

		if (_difs.Count < 20)
		{
			_prevEma = emaValue;
			_prevDif = dif;
			_hasPrev = true;
			return;
		}

		// Compute Bollinger-like bands on dif values
		var lookback = Math.Min(_difs.Count, EmaLength);
		var recent = _difs.Skip(_difs.Count - lookback).ToList();
		var mean = recent.Average();
		var sumSq = recent.Sum(v => (v - mean) * (v - mean));
		var std = (decimal)Math.Sqrt((double)(sumSq / lookback));
		var bbu = mean + std * StdMultiplier;
		var bbm = mean;

		if (!_hasPrev)
		{
			_prevDif = dif;
			_prevEma = emaValue;
			_hasPrev = true;
			return;
		}

		var sigEnter = _prevDif <= bbu && dif > bbu;
		var sigExit = dif < bbm;
		var enterLong = sigEnter && emaValue > _prevEma;
		var enterShort = sigEnter && emaValue < _prevEma;

		if (enterLong && Position <= 0)
		{
			BuyMarket();
		}
		else if (enterShort && Position >= 0)
		{
			SellMarket();
		}
		else if (!UseBinary && sigExit)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
		}

		_prevDif = dif;
		_prevEma = emaValue;
	}
}
