using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hoffman Heiken Bias strategy combining moving averages and Heikin Ashi net volume.
/// </summary>
public class HoffmanHeikenBiasStrategy : Strategy
{
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _ema20Length;
	private readonly StrategyParam<int> _sma50Length;
	private readonly StrategyParam<int> _sma89Length;
	private readonly StrategyParam<int> _ema144Length;
	private readonly StrategyParam<int> _ema35Length;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _netVolumeLength;

	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastSma;
	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _ema20;
	private SimpleMovingAverage _sma50;
	private SimpleMovingAverage _sma89;
	private ExponentialMovingAverage _ema144;
	private ExponentialMovingAverage _ema35;
	private AverageTrueRange _atr;
	private LinearRegression _netVolumeReg;

	private decimal _haOpenPrev;
	private decimal _haClosePrev;
	private bool _isFirstHa = true;

	public int FastSmaLength
{
		get => _fastSmaLength.Value;
		set => _fastSmaLength.Value = value;
}

	public int FastEmaLength
{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
}

	public int Ema20Length
{
		get => _ema20Length.Value;
		set => _ema20Length.Value = value;
}

	public int Sma50Length
{
		get => _sma50Length.Value;
		set => _sma50Length.Value = value;
}

	public int Sma89Length
{
		get => _sma89Length.Value;
		set => _sma89Length.Value = value;
}

	public int Ema144Length
{
		get => _ema144Length.Value;
		set => _ema144Length.Value = value;
}

	public int Ema35Length
{
		get => _ema35Length.Value;
		set => _ema35Length.Value = value;
}

	public int AtrLength
{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
}

	public int NetVolumeLength
{
		get => _netVolumeLength.Value;
		set => _netVolumeLength.Value = value;
}

	public DataType CandleType
{
		get => _candleType.Value;
		set => _candleType.Value = value;
}

	public HoffmanHeikenBiasStrategy()
{
		_fastSmaLength = Param(nameof(FastSmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length", "Fast SMA lookback", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_fastEmaLength = Param(nameof(FastEmaLength), 18)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Fast EMA lookback", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_ema20Length = Param(nameof(Ema20Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA20 Length", "EMA 20 period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_sma50Length = Param(nameof(Sma50Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA50 Length", "SMA 50 period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(30, 80, 5);

		_sma89Length = Param(nameof(Sma89Length), 89)
			.SetGreaterThanZero()
			.SetDisplay("SMA89 Length", "SMA 89 period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 120, 5);

		_ema144Length = Param(nameof(Ema144Length), 144)
			.SetGreaterThanZero()
			.SetDisplay("EMA144 Length", "EMA 144 period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100, 200, 10);

		_ema35Length = Param(nameof(Ema35Length), 35)
			.SetGreaterThanZero()
			.SetDisplay("EMA35 Length", "EMA 35 period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 5);

		_atrLength = Param(nameof(AtrLength), 35)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR lookback", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_netVolumeLength = Param(nameof(NetVolumeLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Net Volume Length", "Linear regression length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastSma = new SimpleMovingAverage { Length = FastSmaLength };
		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_ema20 = new ExponentialMovingAverage { Length = Ema20Length };
		_sma50 = new SimpleMovingAverage { Length = Sma50Length };
		_sma89 = new SimpleMovingAverage { Length = Sma89Length };
		_ema144 = new ExponentialMovingAverage { Length = Ema144Length };
		_ema35 = new ExponentialMovingAverage { Length = Ema35Length };
		_atr = new AverageTrueRange { Length = AtrLength };
		_netVolumeReg = new LinearRegression { Length = NetVolumeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		var bVal = _fastSma.Process(candle).ToDecimal();
		var cVal = _fastEma.Process(candle).ToDecimal();
		var dVal = _ema20.Process(candle).ToDecimal();
		var eVal = _sma50.Process(candle).ToDecimal();
		var fVal = _sma89.Process(candle).ToDecimal();
		var gVal = _ema144.Process(candle).ToDecimal();
		var kVal = _ema35.Process(candle).ToDecimal();
		var atrVal = _atr.Process(candle).ToDecimal();

		var kuVal = kVal + atrVal * 0.5m;
		var klVal = kVal - atrVal * 0.5m;

		if (!_fastSma.IsFormed || !_fastEma.IsFormed || !_ema20.IsFormed || !_sma50.IsFormed ||
			!_sma89.IsFormed || !_ema144.IsFormed || !_ema35.IsFormed || !_atr.IsFormed)
			return;

		var downtrend = dVal > cVal && eVal > cVal && fVal > cVal && gVal > cVal && kVal > cVal && kuVal > cVal && klVal > cVal;
		var uptrend = dVal < cVal && eVal < cVal && fVal < cVal && gVal < cVal && kVal < cVal && kuVal < cVal && klVal < cVal;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _isFirstHa ? (candle.OpenPrice + candle.ClosePrice) / 2m : (_haOpenPrev + _haClosePrev) / 2m;
		var haHigh = Math.Max(Math.Max(candle.HighPrice, haOpen), haClose);
		var haLow = Math.Min(Math.Min(candle.LowPrice, haOpen), haClose);

		var topWick = haOpen < haClose ? haHigh - haClose : haHigh - haOpen;
		var bottomWick = haOpen < haClose ? haOpen - haLow : haClose - haLow;
		var body = haOpen < haClose ? haClose - haOpen : haOpen - haClose;
		var ohcl4 = (haHigh + haLow + haOpen + haClose) / 4m;
		var denom = 2m * topWick + 2m * bottomWick + 2m * body;
		if (denom == 0)
		{
			_haOpenPrev = haOpen;
			_haClosePrev = haClose;
			_isFirstHa = false;
			return;
		}

		var fractionUp = haOpen < haClose ? (topWick + bottomWick + 2m * body) / denom : (topWick + bottomWick) / denom;
		var fractionDown = haOpen < haClose ? (topWick + bottomWick) / denom : (topWick + bottomWick + 2m * body) / denom;
		var volumeUp = candle.TotalVolume * fractionUp * ohcl4;
		var volumeDown = candle.TotalVolume * fractionDown * ohcl4;
		var netVolume = volumeUp - volumeDown;

		var netRegTyped = (LinearRegressionValue)_netVolumeReg.Process(new DecimalIndicatorValue(_netVolumeReg, netVolume));
		if (netRegTyped.LinearReg is not decimal netPlot || !_netVolumeReg.IsFormed)
		{
			_haOpenPrev = haOpen;
			_haClosePrev = haClose;
			_isFirstHa = false;
			return;
		}

		var longSignal = bVal > cVal && uptrend && netPlot > 0m;
		var shortSignal = bVal < cVal && downtrend && netPlot < 0m;

		if (longSignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Long entry: net volume {netPlot:F2}");
		}
		else if (shortSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Short entry: net volume {netPlot:F2}");
		}

		_haOpenPrev = haOpen;
		_haClosePrev = haClose;
		_isFirstHa = false;
	}
}
