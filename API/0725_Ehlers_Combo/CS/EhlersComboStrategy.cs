using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class EhlersComboStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _rmsLength;
	private readonly StrategyParam<decimal> _snrThreshold;
	private readonly StrategyParam<int> _exitLength;

	private StandardDeviation _stdDev;
	private AverageTrueRange _atr;
	private ExponentialMovingAverage _rmsMa;
	private WeightedMovingAverage _ssWma;

	private decimal _a1;
	private decimal _c1;
	private decimal _c2;
	private decimal _c3;

	private decimal _prevClose;
	private decimal _prevPrevClose;
	private decimal _dec;
	private decimal _iTrendPrev1;
	private decimal _iTrendPrev2;
	private decimal _prevSs;
	private decimal _prevSs2;
	private decimal _prevIFish;
	private decimal _prevSlo;
	private int _prevSig;
	private int _prevDecSig;
	private int _bars;

	private readonly Queue<decimal> _exitQueue = new();

	public EhlersComboStrategy()
	{
		_candleType = Param("Candle Type", TimeSpan.FromMinutes(1).TimeFrame());
		_length = Param("Length", 20);
		_rmsLength = Param("Rms length", 50);
		_snrThreshold = Param("SNR threshold", 0.1m);
		_exitLength = Param("Exit length", 100);
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public int RmsLength { get => _rmsLength.Value; set => _rmsLength.Value = value; }
	public decimal SnrThreshold { get => _snrThreshold.Value; set => _snrThreshold.Value = value; }
	public int ExitLength { get => _exitLength.Value; set => _exitLength.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stdDev = new StandardDeviation { Length = Length };
		_atr = new AverageTrueRange { Length = Length };
		_rmsMa = new ExponentialMovingAverage { Length = RmsLength };
		_ssWma = new WeightedMovingAverage { Length = Length };

		var pi = Math.PI;
		_a1 = (decimal)Math.Exp(-1.414 * pi / Length);
		var b1 = 2m * _a1 * (decimal)Math.Cos(1.414 * pi / Length);
		_c2 = b1;
		_c3 = -_a1 * _a1;
		_c1 = 1m - _c2 - _c3;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_stdDev, _atr, ProcessCandle).Start();
	}
	private void ProcessCandle(ICandleMessage candle, decimal std, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_bars++;

		var close = candle.ClosePrice;
		var snr = atr != 0m ? std / atr : 0m;

		var twoPi = 2m * (decimal)Math.PI / Length;
		var alpha = (decimal)((Math.Cos((double)twoPi) + Math.Sin((double)twoPi) - 1) / Math.Cos((double)twoPi));
		_dec = ((alpha / 2m) * (close + _prevClose)) + ((1m - alpha) * _dec);
		var decSig = close > _dec ? 1 : close < _dec ? -1 : 0;

		var itrend = (alpha - alpha * alpha / 4m) * close + 0.5m * alpha * alpha * _prevClose - (alpha - 0.75m * alpha * alpha) * _prevPrevClose + 2m * (1m - alpha) * _iTrendPrev1 - (1m - alpha) * (1m - alpha) * _iTrendPrev2;
		if (_bars < 7)
			itrend = (close + 2m * _prevClose + _prevPrevClose) / 4m;
		var trigger = 2m * itrend - _iTrendPrev2;

		var deriv = close - _prevPrevClose;
		_rmsMa.Process(deriv * deriv);
		var rms = (decimal)Math.Sqrt((double)_rmsMa.GetCurrentValue<decimal>());
		var nDeriv = rms != 0m ? deriv / rms : 0m;
		var exp = (decimal)Math.Exp(2.0 * (double)nDeriv);
		var iFish = nDeriv != 0m ? (exp - 1m) / (exp + 1m) : 0m;
		var ss = (_c1 * ((iFish + _prevIFish) / 2m)) + (_c2 * _prevSs) + (_c3 * _prevSs2);
		_ssWma.Process(ss);
		var ssSig = _ssWma.GetCurrentValue<decimal>();
		var slo = ss - ssSig;
		var sig = slo > 0m ? (slo > _prevSlo ? 2 : 1) : slo < 0m ? (slo < _prevSlo ? -2 : -1) : 0;

		var spearmanSig = close > _prevClose ? 1 : close < _prevClose ? -1 : 0;

		_exitQueue.Enqueue(close);
		var oldClose = close;
		if (_exitQueue.Count > ExitLength)
			oldClose = _exitQueue.Dequeue();

		var exitLong = oldClose < itrend;
		var exitShort = oldClose > itrend;

		var enterLong = sig > 0 && _prevSig <= 0 && decSig > 0 && _prevDecSig <= 0 && close > _dec && _prevClose <= _dec && close > itrend && _iTrendPrev1 < itrend && spearmanSig > 0 && snr > SnrThreshold;
		var enterShort = sig < 0 && _prevSig >= 0 && decSig < 0 && _prevDecSig >= 0 && close < _dec && _prevClose >= _dec && close < itrend && _iTrendPrev1 > itrend && spearmanSig < 0 && snr > SnrThreshold;

		if (enterLong && Position <= 0)
			BuyMarket();
		else if (enterShort && Position >= 0)
			SellMarket();

		if (exitLong && Position > 0)
			SellMarket(Position);
		else if (exitShort && Position < 0)
			BuyMarket(-Position);

		_prevSs2 = _prevSs;
		_prevSs = ss;
		_prevIFish = iFish;
		_prevSlo = slo;
		_prevSig = sig;
		_prevDecSig = decSig;
		_iTrendPrev2 = _iTrendPrev1;
		_iTrendPrev1 = itrend;
		_prevPrevClose = _prevClose;
		_prevClose = close;
	}
}
