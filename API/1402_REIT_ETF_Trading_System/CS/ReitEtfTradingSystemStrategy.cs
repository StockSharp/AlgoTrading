using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ReitEtfTradingSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _tnxLookback;
	private readonly StrategyParam<decimal> _tnxMinChange;
	private readonly StrategyParam<int> _donchianLength;
	private readonly StrategyParam<decimal> _maxCorrelation;
	private readonly StrategyParam<decimal> _minYield;
	private readonly StrategyParam<decimal> _atrStop;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<Security> _spySecurity;
	private readonly StrategyParam<Security> _tnxSecurity;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _spySma20;
	private Lowest _tnxLowest;
	private Highest _tnxHighest;
	private Highest _highestDc;
	private Lowest _lowestLow;
	private Highest _highestHigh3;
	private Highest _highestStoch3;
	private Highest _highestClose2;
	private Highest _highestClose5;

	private decimal _tnxClose;
	private decimal _spyClose;
	private decimal _spySma20Value;
	private decimal _tnxLl;
	private decimal _tnxHh;
	private int _tnxBarsSinceHigh;
	private decimal _prevClose;
	private decimal _prevSpyClose;
	private decimal _dcUpperPrev;
	private int _barIndex;

	private decimal[] _tnxHighWindow = Array.Empty<decimal>();
	private int _tnxHighIndex;
	private int _tnxHighCount;

	private readonly decimal[] _corrMain = new decimal[20];
	private readonly decimal[] _corrTnx = new decimal[20];
	private readonly decimal[] _corrSpy = new decimal[20];
	private int _corrCount;

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Lookback period for TNX index.
	/// </summary>
	public int TnxLookbackPeriod
	{
		get => _tnxLookback.Value;
		set => _tnxLookback.Value = value;
	}

	/// <summary>
	/// Minimum TNX change percent.
	/// </summary>
	public decimal TnxMinChangePercent
	{
		get => _tnxMinChange.Value;
		set => _tnxMinChange.Value = value;
	}

	/// <summary>
	/// Donchian Channel length.
	/// </summary>
	public int DonchianChannelLength
	{
		get => _donchianLength.Value;
		set => _donchianLength.Value = value;
	}

	/// <summary>
	/// Maximum correlation for buy signal.
	/// </summary>
	public decimal MaxCorrelationForBuy
	{
		get => _maxCorrelation.Value;
		set => _maxCorrelation.Value = value;
	}

	/// <summary>
	/// Minimum yield value.
	/// </summary>
	public decimal MinYield
	{
		get => _minYield.Value;
		set => _minYield.Value = value;
	}

	/// <summary>
	/// ATR stop multiplier.
	/// </summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStop.Value;
		set => _atrStop.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Reference SPY security.
	/// </summary>
	public Security SpySecurity
	{
		get => _spySecurity.Value;
		set => _spySecurity.Value = value;
	}

	/// <summary>
	/// TNX index security.
	/// </summary>
	public Security TnxSecurity
	{
		get => _tnxSecurity.Value;
		set => _tnxSecurity.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ReitEtfTradingSystemStrategy"/>.
	/// </summary>
	public ReitEtfTradingSystemStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 15)
			.SetDisplay("BB Length", "Bollinger Bands length", "Indicators");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetDisplay("BB Multiplier", "Bollinger Bands multiplier", "Indicators");

		_tnxLookback = Param(nameof(TnxLookbackPeriod), 25)
			.SetDisplay("TNX Lookback", "Lookback period for TNX index", "Parameters");

		_tnxMinChange = Param(nameof(TnxMinChangePercent), 15m)
			.SetDisplay("TNX Min Change %", "Minimum TNX change percent", "Signals");

		_donchianLength = Param(nameof(DonchianChannelLength), 30)
			.SetDisplay("Donchian Length", "Donchian Channel length", "Indicators");

		_maxCorrelation = Param(nameof(MaxCorrelationForBuy), 0.3m)
			.SetDisplay("Max Correlation", "Maximum correlation for buy signal", "Signals");

		_minYield = Param(nameof(MinYield), 2m)
			.SetDisplay("Min Yield", "Minimum yield", "Signals");

		_atrStop = Param(nameof(AtrStopMultiplier), 1.5m)
			.SetDisplay("ATR Stop", "ATR stop multiplier", "Protection");

		_stopLoss = Param(nameof(StopLossPercent), 8m)
			.SetDisplay("Stop Loss %", "Maximum loss percent", "Protection");

		_spySecurity = Param<Security>(nameof(SpySecurity))
			.SetDisplay("SPY Security", "Reference SPY security", "Data")
			.SetRequired();

		_tnxSecurity = Param<Security>(nameof(TnxSecurity))
			.SetDisplay("TNX Security", "TNX index security", "Data")
			.SetRequired();

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(7).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (SpySecurity, CandleType);
		yield return (TnxSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_tnxClose = 0m;
		_spyClose = 0m;
		_spySma20Value = 0m;
		_tnxLl = 0m;
		_tnxHh = 0m;
		_tnxBarsSinceHigh = 0;
		_prevClose = 0m;
		_prevSpyClose = 0m;
		_dcUpperPrev = 0m;
		_barIndex = 0;
		_corrCount = 0;
		Array.Clear(_corrMain);
		Array.Clear(_corrTnx);
		Array.Clear(_corrSpy);
		_tnxHighWindow = new decimal[DonchianChannelLength];
		_tnxHighIndex = 0;
		_tnxHighCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_tnxLowest = new Lowest { Length = TnxLookbackPeriod };
		_tnxHighest = new Highest { Length = TnxLookbackPeriod };
		_highestDc = new Highest { Length = DonchianChannelLength };
		_lowestLow = new Lowest { Length = 100 };
		_highestHigh3 = new Highest { Length = 3 };
		_highestStoch3 = new Highest { Length = 3 };

		var bb = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };
		var atr = new AverageTrueRange { Length = 15 };
		var stoch = new StochasticOscillator { Length = 10, KPeriod = 3, DPeriod = 1 };
		var sma30 = new SimpleMovingAverage { Length = 30 };
		_highestClose2 = new Highest { Length = 2 };
		_highestClose5 = new Highest { Length = 5 };

		var mainSub = SubscribeCandles(CandleType);
		mainSub
			.BindEx(bb, stoch, atr, sma30, _highestClose2, _highestClose5, ProcessMainCandle)
			.Start();

		_spySma20 = new SimpleMovingAverage { Length = 20 };
		var spySub = SubscribeCandles(CandleType, security: SpySecurity);
		spySub
			.Bind(_spySma20, ProcessSpyCandle)
			.Start();

		var tnxSub = SubscribeCandles(CandleType, security: TnxSecurity);
		tnxSub
			.Bind(ProcessTnxCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawIndicator(area, bb);
			DrawIndicator(area, sma30);
			DrawOwnTrades(area);
		}

		base.OnStarted(time);
	}

	private void ProcessSpyCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevSpyClose = _spyClose;
		_spyClose = candle.ClosePrice;
		_spySma20Value = sma;
	}

	private void ProcessTnxCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_tnxClose = candle.ClosePrice;

		_tnxLl = _tnxLowest.Process(_tnxClose, candle.OpenTime, true).ToDecimal();
		_tnxHh = _tnxHighest.Process(_tnxClose, candle.OpenTime, true).ToDecimal();

		_tnxHighWindow[_tnxHighIndex] = _tnxClose;
		_tnxHighIndex = (_tnxHighIndex + 1) % DonchianChannelLength;
		if (_tnxHighCount < DonchianChannelLength)
			_tnxHighCount++;

		var max = _tnxHighWindow[0];
		var bars = 0;
		for (var i = 0; i < _tnxHighCount; i++)
		{
			var idx = (_tnxHighIndex - 1 - i);
			if (idx < 0)
				idx += DonchianChannelLength;
			var val = _tnxHighWindow[idx];
			if (val >= max)
			{
				max = val;
				bars = i;
			}
		}
		_tnxBarsSinceHigh = bars;
	}

	private void ProcessMainCandle(ICandleMessage candle, IIndicatorValue bbVal, IIndicatorValue stochVal, IIndicatorValue atrVal, IIndicatorValue sma30Val, IIndicatorValue high2Val, IIndicatorValue high5Val)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var bb = (BollingerBandsValue)bbVal;
		if (bb.LowBand is not decimal bbLower)
			return;

		if (stochVal is not StochasticOscillatorValue stochValue || stochValue.K is not decimal stochK)
			return;

		var stochHigh3 = _highestStoch3.Process(stochK, candle.OpenTime, true).ToDecimal();
		var atr = atrVal.ToDecimal();
		var sma30 = sma30Val.ToDecimal();
		var highestClose2 = high2Val.ToDecimal();
		var highestClose5 = high5Val.ToDecimal();

		var ll = _lowestLow.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();
		var high3 = _highestHigh3.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();

		var dcUpper = _dcUpperPrev;
		_dcUpperPrev = _highestDc.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

		Shift(_corrMain, candle.ClosePrice);
		Shift(_corrTnx, _tnxClose);
		Shift(_corrSpy, _spyClose);
		if (_corrCount < 20)
			_corrCount++;
		var corT = _corrCount < 20 ? 0m : CalculateCorrelation(_corrMain, _corrTnx);
		var corS = _corrCount < 20 ? 0m : CalculateCorrelation(_corrMain, _corrSpy);

		var tnxUp = _tnxLl == 0m ? 0m : (_tnxClose - _tnxLl) / (_tnxLl + 0.001m) * 100m;
		var tnxDn = _tnxHh == 0m ? 0m : (_tnxClose - _tnxHh) / (_tnxHh + 0.001m) * 100m;

		var llPercent = ll == 0m ? 0m : (candle.ClosePrice - ll) / (ll + 0.001m) * 100m;
		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;

		var isStop = candle.ClosePrice < sma30 &&
			NotAtLoss(candle.ClosePrice, highestClose2) &&
			(NotAtLoss(_spyClose, _prevSpyClose) || corS < 0m);

		var buyBb = candle.LowPrice < bbLower &&
			candle.ClosePrice > bbLower + 0.2m * atr &&
			((tnxDn < -TnxMinChangePercent && corT < MaxCorrelationForBuy) || _tnxClose < MinYield) &&
			candle.ClosePrice > hl2 &&
			!isStop;

		var buyTrend = candle.ClosePrice > dcUpper &&
			_spyClose > _spySma20Value &&
			(_tnxBarsSinceHigh > 10 || corT > MaxCorrelationForBuy || _tnxClose < MinYield);

		var sell1 = (tnxUp > TnxMinChangePercent || (_tnxClose > 4m && tnxUp > 0m)) &&
			candle.ClosePrice < highestClose5 - AtrStopMultiplier * atr &&
			!buyBb;

		var stop0 = candle.ClosePrice < sma30 &&
			NotAtLoss(candle.ClosePrice, _prevClose) &&
			(NotAtLoss(_spyClose, _prevSpyClose) || corS < 0m);

		var sellOb = llPercent > 50m &&
			candle.ClosePrice < high3 - AtrStopMultiplier * atr &&
			stochHigh3 > 90m;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (buyBb && Position <= 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (buyTrend && Position <= 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && (sell1 || sellOb || stop0))
		{
			SellMarket(Position);
		}

		_prevClose = candle.ClosePrice;
	}

	private bool NotAtLoss(decimal src, decimal limit)
	{
		return limit != 0m && src < (1m - StopLossPercent / 100m) * limit;
	}

	private static void Shift(decimal[] array, decimal value)
	{
		for (var i = 0; i < array.Length - 1; i++)
			array[i] = array[i + 1];
		array[^1] = value;
	}

	private static decimal CalculateCorrelation(decimal[] x, decimal[] y)
	{
		var n = x.Length;
		decimal sumX = 0m, sumY = 0m, sumX2 = 0m, sumY2 = 0m, sumXY = 0m;
		for (var i = 0; i < n; i++)
		{
			var xi = x[i];
			var yi = y[i];
			sumX += xi;
			sumY += yi;
			sumX2 += xi * xi;
			sumY2 += yi * yi;
			sumXY += xi * yi;
		}
		var denom = (decimal)Math.Sqrt((double)(n * sumX2 - sumX * sumX) * (double)(n * sumY2 - sumY * sumY));
		return denom == 0m ? 0m : (n * sumXY - sumX * sumY) / denom;
	}
}