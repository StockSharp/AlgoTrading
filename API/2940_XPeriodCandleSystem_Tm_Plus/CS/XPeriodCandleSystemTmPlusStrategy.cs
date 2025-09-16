using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Translated XPeriod Candle System strategy with time filter and Bollinger band breakout logic.
/// </summary>
public class XPeriodCandleSystemTmPlusStrategy : Strategy
{
	private const int MaxColorHistory = 16;

	private readonly int[] _colorHistory = new int[MaxColorHistory];
	private int _historyCount;

	private ExponentialMovingAverage _openMa = null!;
	private ExponentialMovingAverage _highMa = null!;
	private ExponentialMovingAverage _lowMa = null!;
	private ExponentialMovingAverage _closeMa = null!;
	private BollingerBands _bollinger = null!;

	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<bool> _timeTrade;
	private readonly StrategyParam<int> _holdingMinutes;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _deviation;

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	public bool TimeTrade
	{
		get => _timeTrade.Value;
		set => _timeTrade.Value = value;
	}

	public int HoldingMinutes
	{
		get => _holdingMinutes.Value;
		set => _holdingMinutes.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	public decimal BandsDeviation
	{
		get => _bandsDeviation.Value;
		set => _bandsDeviation.Value = value;
	}

	public AppliedPrice AppliedPriceMode
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal Deviation
	{
		get => _deviation.Value;
		set => _deviation.Value = value;
	}

	public XPeriodCandleSystemTmPlusStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Base order size", "Trading")
		.SetCanOptimize();

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Allow Buy Open", "Enable long entries", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Allow Sell Open", "Enable short entries", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Allow Buy Close", "Allow long exits", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Allow Sell Close", "Allow short exits", "Trading");

		_timeTrade = Param(nameof(TimeTrade), true)
		.SetDisplay("Enable Time Filter", "Close positions after a fixed holding time", "Risk");

		_holdingMinutes = Param(nameof(HoldingMinutes), 960)
		.SetRange(1, 2000)
		.SetDisplay("Holding Minutes", "Maximum holding time in minutes", "Risk")
		.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for signal detection", "Market")
		.SetCanOptimize();

		_period = Param(nameof(Period), 5)
		.SetRange(2, 100)
		.SetDisplay("Smoothing Length", "Length for candle smoothing", "Indicators")
		.SetCanOptimize();

		_bollingerLength = Param(nameof(BollingerLength), 20)
		.SetRange(2, 200)
		.SetDisplay("Bollinger Length", "Number of bars for Bollinger Bands", "Indicators")
		.SetCanOptimize();

		_bandsDeviation = Param(nameof(BandsDeviation), 1.001m)
		.SetGreaterThanZero()
		.SetDisplay("Bands Deviation", "Multiplier for Bollinger Bands width", "Indicators")
		.SetCanOptimize();

		_appliedPrice = Param(nameof(AppliedPriceMode), AppliedPrice.Close)
		.SetDisplay("Applied Price", "Price source for the band calculation", "Indicators");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetRange(1, MaxColorHistory - 2)
		.SetDisplay("Signal Bar", "Index of the candle used for signals", "Trading");

		_stopLoss = Param(nameof(StopLoss), 1000m)
		.SetRange(0m, 100000m)
		.SetDisplay("Stop Loss", "Protective stop distance in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
		.SetRange(0m, 100000m)
		.SetDisplay("Take Profit", "Protective profit target in price units", "Risk");

		_deviation = Param(nameof(Deviation), 10m)
		.SetRange(0m, 10000m)
		.SetDisplay("Breakout Offset", "Additional price offset applied to breakouts", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_colorHistory, 0, _colorHistory.Length);
		_historyCount = 0;
		_longEntryTime = null;
		_shortEntryTime = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_openMa = new ExponentialMovingAverage { Length = Period };
		_highMa = new ExponentialMovingAverage { Length = Period };
		_lowMa = new ExponentialMovingAverage { Length = Period };
		_closeMa = new ExponentialMovingAverage { Length = Period };

		_bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BandsDeviation
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(ProcessCandle)
		.Start();

		var take = TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Absolute) : new Unit(0);
		var stop = StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Absolute) : new Unit(0);

		StartProtection(takeProfit: take, stopLoss: stop);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var openValue = _openMa.Process(new DecimalIndicatorValue(_openMa, candle.OpenPrice, candle.OpenTime));
		var highValue = _highMa.Process(new DecimalIndicatorValue(_highMa, candle.HighPrice, candle.OpenTime));
		var lowValue = _lowMa.Process(new DecimalIndicatorValue(_lowMa, candle.LowPrice, candle.OpenTime));
		var closeValue = _closeMa.Process(new DecimalIndicatorValue(_closeMa, candle.ClosePrice, candle.OpenTime));

		if (!openValue.IsFinal || !highValue.IsFinal || !lowValue.IsFinal || !closeValue.IsFinal)
		return;

		var smoothedOpen = openValue.GetValue<decimal>();
		var smoothedHigh = highValue.GetValue<decimal>();
		var smoothedLow = lowValue.GetValue<decimal>();
		var smoothedClose = closeValue.GetValue<decimal>();

		var price = GetAppliedPrice(AppliedPriceMode, smoothedOpen, smoothedHigh, smoothedLow, smoothedClose);
		var bbValue = (BollingerBandsValue)_bollinger.Process(new DecimalIndicatorValue(_bollinger, price, candle.OpenTime));

		if (bbValue.UpBand is not decimal upperBand || bbValue.LowBand is not decimal lowerBand)
		return;

		var color = CalculateColor(smoothedOpen, smoothedClose, upperBand, lowerBand, GetBreakoutOffset());
		AddColor(color);

		if (_historyCount <= SignalBar)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (TimeTrade)
		CheckTimeExit(candle);

		var signalIndex = SignalBar - 1;
		var previousIndex = SignalBar;

		var currentColor = _colorHistory[signalIndex];
		var previousColor = _colorHistory[previousIndex];

		if (BuyPosClose && previousColor > 2)
		CloseLongPosition();

		if (SellPosClose && previousColor < 2)
		CloseShortPosition();

		if (BuyPosOpen && previousColor == 0 && currentColor != 0 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_longEntryTime = candle.CloseTime;
			_shortEntryTime = null;
		}

		if (SellPosOpen && previousColor == 4 && currentColor != 4 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_shortEntryTime = candle.CloseTime;
			_longEntryTime = null;
		}
	}

	private void CheckTimeExit(ICandleMessage candle)
	{
		var holding = TimeSpan.FromMinutes(HoldingMinutes);

		if (Position > 0 && _longEntryTime is DateTimeOffset longTime && candle.CloseTime - longTime >= holding)
		CloseLongPosition();
		else if (Position < 0 && _shortEntryTime is DateTimeOffset shortTime && candle.CloseTime - shortTime >= holding)
		CloseShortPosition();
	}

	private void CloseLongPosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
			_longEntryTime = null;
		}
	}

	private void CloseShortPosition()
	{
		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_shortEntryTime = null;
		}
	}

	private decimal GetBreakoutOffset()
	{
		var step = Security?.PriceStep ?? 0m;
		var offset = Deviation;

		if (step > 0m)
		offset *= step;

		return Math.Abs(offset);
	}

	private static int CalculateColor(decimal open, decimal close, decimal upperBand, decimal lowerBand, decimal offset)
	{
		var isBullish = close >= open;

		if (isBullish && close > upperBand + offset)
		return 0;

		if (!isBullish && close < lowerBand - offset)
		return 4;

		return isBullish ? 1 : 3;
	}

	private void AddColor(int color)
	{
		for (var i = MaxColorHistory - 1; i > 0; i--)
		_colorHistory[i] = _colorHistory[i - 1];

		_colorHistory[0] = color;

		if (_historyCount < MaxColorHistory)
		_historyCount++;
	}

	private static decimal GetAppliedPrice(AppliedPrice mode, decimal open, decimal high, decimal low, decimal close)
	{
		return mode switch
		{
			AppliedPrice.Close => close,
			AppliedPrice.Open => open,
			AppliedPrice.High => high,
			AppliedPrice.Low => low,
			AppliedPrice.Median => (high + low) / 2m,
			AppliedPrice.Typical => (close + high + low) / 3m,
			AppliedPrice.Weighted => (2m * close + high + low) / 4m,
			AppliedPrice.Simpl => (open + close) / 2m,
			AppliedPrice.Quarter => (open + close + high + low) / 4m,
			AppliedPrice.TrendFollow0 => close > open ? high : close < open ? low : close,
			AppliedPrice.TrendFollow1 => close > open ? (high + close) / 2m : close < open ? (low + close) / 2m : close,
			AppliedPrice.Demark => CalculateDemark(open, high, low, close),
			_ => close,
		};
	}

	private static decimal CalculateDemark(decimal open, decimal high, decimal low, decimal close)
	{
		var res = high + low + close;

		if (close < open)
		res = (res + low) / 2m;
		else if (close > open)
		res = (res + high) / 2m;
		else
		res = (res + close) / 2m;

		return ((res - low) + (res - high)) / 2m;
	}

	public enum AppliedPrice
	{
		Close = 1,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simpl,
		Quarter,
		TrendFollow0,
		TrendFollow1,
		Demark
	}
}
