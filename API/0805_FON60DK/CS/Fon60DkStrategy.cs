using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class Fon60DkStrategy : Strategy
{
	private readonly StrategyParam<int> _t3Length;
	private readonly StrategyParam<decimal> _t3Opt;
	private readonly StrategyParam<int> _tottLength;
	private readonly StrategyParam<decimal> _tottOpt;
	private readonly StrategyParam<decimal> _tottCoeff;

	private readonly StrategyParam<int> _t3LengthSat;
	private readonly StrategyParam<decimal> _t3OptSat;
	private readonly StrategyParam<int> _tottLengthSat;
	private readonly StrategyParam<decimal> _tottOptSat;
	private readonly StrategyParam<decimal> _tottCoeffSat;

	private readonly StrategyParam<int> _williamsLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema1;
	private ExponentialMovingAverage _ema2;
	private ExponentialMovingAverage _ema3;
	private ExponentialMovingAverage _ema4;

	private ExponentialMovingAverage _ema1Sat;
	private ExponentialMovingAverage _ema2Sat;
	private ExponentialMovingAverage _ema3Sat;
	private ExponentialMovingAverage _ema4Sat;

	private ChandeMomentumOscillator _cmo;
	private ChandeMomentumOscillator _cmoSat;

	private decimal? _var;
	private decimal? _varSat;

	private decimal? _longStopPrev;
	private decimal? _shortStopPrev;
	private int _dir = 1;

	private decimal? _longStopPrevS;
	private decimal? _shortStopPrevS;
	private int _dirS = 1;

	public int T3Length { get => _t3Length.Value; set => _t3Length.Value = value; }
	public decimal T3Opt { get => _t3Opt.Value; set => _t3Opt.Value = value; }
	public int TottLength { get => _tottLength.Value; set => _tottLength.Value = value; }
	public decimal TottOpt { get => _tottOpt.Value; set => _tottOpt.Value = value; }
	public decimal TottCoeff { get => _tottCoeff.Value; set => _tottCoeff.Value = value; }

	public int T3LengthSat { get => _t3LengthSat.Value; set => _t3LengthSat.Value = value; }
	public decimal T3OptSat { get => _t3OptSat.Value; set => _t3OptSat.Value = value; }
	public int TottLengthSat { get => _tottLengthSat.Value; set => _tottLengthSat.Value = value; }
	public decimal TottOptSat { get => _tottOptSat.Value; set => _tottOptSat.Value = value; }
	public decimal TottCoeffSat { get => _tottCoeffSat.Value; set => _tottCoeffSat.Value = value; }

	public int WilliamsLength { get => _williamsLength.Value; set => _williamsLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Fon60DkStrategy()
	{
		_t3Length = Param(nameof(T3Length), 5).SetGreaterThanZero().SetDisplay("Tillson Period AL", "Tillson T3 period for entry", "Indicators");
		_t3Opt = Param(nameof(T3Opt), 0.1m).SetDisplay("Tillson Opt AL", "Tillson T3 factor for entry", "Indicators");
		_tottLength = Param(nameof(TottLength), 5).SetGreaterThanZero().SetDisplay("TOTT Period AL", "OTT period for entry", "Indicators");
		_tottOpt = Param(nameof(TottOpt), 0.1m).SetDisplay("TOTT Opt AL", "OTT optimization for entry", "Indicators");
		_tottCoeff = Param(nameof(TottCoeff), 0.006m).SetDisplay("TOTT Coeff AL", "OTT coefficient for entry", "Indicators");

		_t3LengthSat = Param(nameof(T3LengthSat), 5).SetGreaterThanZero().SetDisplay("Tillson Period SAT", "Tillson T3 period for exit", "Indicators");
		_t3OptSat = Param(nameof(T3OptSat), 0.1m).SetDisplay("Tillson Opt SAT", "Tillson T3 factor for exit", "Indicators");
		_tottLengthSat = Param(nameof(TottLengthSat), 5).SetGreaterThanZero().SetDisplay("TOTT Period SAT", "OTT period for exit", "Indicators");
		_tottOptSat = Param(nameof(TottOptSat), 0.1m).SetDisplay("TOTT Opt SAT", "OTT optimization for exit", "Indicators");
		_tottCoeffSat = Param(nameof(TottCoeffSat), 0.006m).SetDisplay("TOTT Coeff SAT", "OTT coefficient for exit", "Indicators");

		_williamsLength = Param(nameof(WilliamsLength), 3).SetGreaterThanZero().SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema1 = new() { Length = T3Length };
		_ema2 = new() { Length = T3Length };
		_ema3 = new() { Length = T3Length };
		_ema4 = new() { Length = T3Length };

		_ema1Sat = new() { Length = T3LengthSat };
		_ema2Sat = new() { Length = T3LengthSat };
		_ema3Sat = new() { Length = T3LengthSat };
		_ema4Sat = new() { Length = T3LengthSat };

		_cmo = new() { Length = 9 };
		_cmoSat = new() { Length = 9 };

		var williams = new WilliamsR { Length = WilliamsLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(williams, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue williamsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var close = candle.ClosePrice;

		var t3 = CalcT3(_ema1, _ema2, _ema3, _ema4, T3Opt, close, time);
		var varValue = CalcVar(close, _cmo, TottLength, ref _var, time);
		var (ottUp, _) = CalcOtt(varValue, TottOpt, TottCoeff, ref _longStopPrev, ref _shortStopPrev, ref _dir);

		var t3Sat = CalcT3(_ema1Sat, _ema2Sat, _ema3Sat, _ema4Sat, T3OptSat, close, time);
		var varValueSat = CalcVar(close, _cmoSat, TottLengthSat, ref _varSat, time);
		var (_, ottDnS) = CalcOtt(varValueSat, TottOptSat, TottCoeffSat, ref _longStopPrevS, ref _shortStopPrevS, ref _dirS);

		var williams = williamsValue.ToDecimal();

		var longCondition = t3 > ottUp && williams > -20m;
		var shortCondition = t3Sat < ottDnS && williams < -70m;

		if (longCondition && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCondition && Position > 0)
			SellMarket(Position);
	}

	private static decimal CalcT3(ExponentialMovingAverage ema1, ExponentialMovingAverage ema2, ExponentialMovingAverage ema3, ExponentialMovingAverage ema4, decimal opt, decimal price, DateTimeOffset time)
	{
		var e1 = ema1.Process(price, time, true).ToDecimal();
		var e2 = ema2.Process(e1, time, true).ToDecimal();
		var e3 = ema3.Process(e2, time, true).ToDecimal();
		var e4 = ema4.Process(e3, time, true).ToDecimal();

		var c1 = -opt * opt * opt;
		var c2 = 3m * opt * opt + 3m * opt * opt * opt;
		var c3 = -6m * opt * opt - 3m * opt - 3m * opt * opt * opt;
		var c4 = 1m + 3m * opt + opt * opt * opt + 3m * opt * opt;

		return c1 * e4 + c2 * e3 + c3 * e2 + c4 * e1;
	}

	private static decimal CalcVar(decimal price, ChandeMomentumOscillator cmo, int length, ref decimal? prev, DateTimeOffset time)
	{
		var cmoVal = cmo.Process(price, time, true).ToDecimal();
		var valpha = 2m / (length + 1m);
		var absCmo = Math.Abs(cmoVal / 100m);
		var previous = prev ?? price;
		var result = valpha * absCmo * price + (1m - valpha * absCmo) * previous;
		prev = result;
		return result;
	}

	private static (decimal ottUp, decimal ottDn) CalcOtt(decimal mAvg, decimal opt, decimal coeff, ref decimal? longStopPrev, ref decimal? shortStopPrev, ref int dir)
	{
		var fark = mAvg * opt * 0.01m;

		var longStop = mAvg - fark;
		var lsPrev = longStopPrev ?? longStop;
		longStop = mAvg > lsPrev ? Math.Max(longStop, lsPrev) : longStop;
		longStopPrev = longStop;

		var shortStop = mAvg + fark;
		var ssPrev = shortStopPrev ?? shortStop;
		shortStop = mAvg < ssPrev ? Math.Min(shortStop, ssPrev) : shortStop;
		shortStopPrev = shortStop;

		dir = dir == -1 && mAvg > ssPrev ? 1 : dir == 1 && mAvg < lsPrev ? -1 : dir;

		var mt = dir == 1 ? longStop : shortStop;
		var ott = mAvg > mt ? mt * (200m + opt) / 200m : mt * (200m - opt) / 200m;
		var ottUp = ott * (1m + coeff);
		var ottDn = ott * (1m - coeff);
		return (ottUp, ottDn);
	}
}
