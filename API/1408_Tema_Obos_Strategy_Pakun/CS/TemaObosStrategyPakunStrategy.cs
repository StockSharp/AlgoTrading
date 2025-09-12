using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TEMA OBOS strategy with ATR based targets.
/// Uses EMA cross filtered by OBOS oscillator.
/// </summary>
public class TemaObosStrategyPakunStrategy : Strategy
{
	private readonly StrategyParam<int> _temaLength;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<int> _obosLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly EMA _ema1 = new();
	private readonly EMA _ema2 = new();
	private readonly EMA _ema3 = new();
	private readonly EMA _ema4 = new();
	private readonly EMA _rk3 = new();
	private readonly StandardDeviation _rk4 = new();
	private readonly EMA _rk6 = new();
	private readonly EMA _up = new();
	private readonly EMA _down = new();
	private readonly ATR _atr = new() { Length = 14 };

	private decimal _prevEma3;
	private decimal _prevEma4;
	private decimal? _stop;
	private decimal? _tp;

	public int TemaLength { get => _temaLength.Value; set => _temaLength.Value = value; }
	public decimal TpMultiplier { get => _tpMultiplier.Value; set => _tpMultiplier.Value = value; }
	public decimal SlMultiplier { get => _slMultiplier.Value; set => _slMultiplier.Value = value; }
	public int ObosLength { get => _obosLength.Value; set => _obosLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TemaObosStrategyPakunStrategy()
	{
		_temaLength = Param(nameof(TemaLength), 25);
		_tpMultiplier = Param(nameof(TpMultiplier), 5m);
		_slMultiplier = Param(nameof(SlMultiplier), 2m);
		_obosLength = Param(nameof(ObosLength), 85);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma3 = 0m;
		_prevEma4 = 0m;
		_stop = null;
		_tp = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema1.Length = TemaLength;
		_ema2.Length = TemaLength;
		_ema3.Length = TemaLength;
		_ema4.Length = TemaLength * 2;
		_rk3.Length = ObosLength;
		_rk4.Length = ObosLength;
		_rk6.Length = ObosLength;
		_up.Length = ObosLength;
		_down.Length = ObosLength;

		var sub = SubscribeCandles(CandleType);
		sub.Bind(Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, _ema3);
			DrawIndicator(area, _ema4);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ema1Val = _ema1.Process(candle.ClosePrice);
		var ema2Val = _ema2.Process(ema1Val);
		var ema3Val = _ema3.Process(ema2Val);
		var ema4Val = _ema4.Process(ema2Val);

		var ys1 = (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m;
		var rk3Val = _rk3.Process(ys1);
		var rk4Val = _rk4.Process(ys1);
		var rk5Val = rk4Val == 0m ? 0m : (ys1 - rk3Val) * 100m / rk4Val;
		var rk6Val = _rk6.Process(rk5Val);
		var upVal = _up.Process(rk6Val);
		var downVal = _down.Process(upVal);

		var atrVal = _atr.Process(candle);

		var longCond = _prevEma3 <= _prevEma4 && ema3Val > ema4Val && upVal > downVal;
		var shortCond = _prevEma3 >= _prevEma4 && ema3Val < ema4Val && upVal < downVal;

		if (longCond && Position <= 0)
		{
			BuyMarket();
			_stop = candle.ClosePrice - atrVal * SlMultiplier;
			_tp = candle.ClosePrice + atrVal * TpMultiplier;
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket();
			_stop = candle.ClosePrice + atrVal * SlMultiplier;
			_tp = candle.ClosePrice - atrVal * TpMultiplier;
		}

		if (Position > 0)
		{
			if (_stop.HasValue && candle.LowPrice <= _stop)
			{
				SellMarket(Math.Abs(Position));
				_stop = null;
				_tp = null;
			}
			else if (_tp.HasValue && candle.HighPrice >= _tp)
			{
				SellMarket(Math.Abs(Position));
				_stop = null;
				_tp = null;
			}
		}
		else if (Position < 0)
		{
			if (_stop.HasValue && candle.HighPrice >= _stop)
			{
				BuyMarket(Math.Abs(Position));
				_stop = null;
				_tp = null;
			}
			else if (_tp.HasValue && candle.LowPrice <= _tp)
			{
				BuyMarket(Math.Abs(Position));
				_stop = null;
				_tp = null;
			}
		}

		_prevEma3 = ema3Val;
		_prevEma4 = ema4Val;
	}
}
