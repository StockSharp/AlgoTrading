namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class AwesomeOscTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _closeTrade;
	private readonly StrategyParam<int> _profitTypeClTrd;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerSigma;
	private readonly StrategyParam<decimal> _bollingerSpreadLowerLimit;
	private readonly StrategyParam<decimal> _bollingerSpreadUpperLimit;
	private readonly StrategyParam<int> _periodFast;
	private readonly StrategyParam<int> _periodSlow;
	private readonly StrategyParam<decimal> _aoStrengthLimit;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<int> _stochSlow;
	private readonly StrategyParam<decimal> _stochLower;
	private readonly StrategyParam<decimal> _stochUpper;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _openHours;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;

	private readonly BollingerBands _bollinger = new();
	private readonly StochasticOscillator _stochastic = new();
	private readonly AwesomeOscillator _awesome = new();
	private readonly Highest _aoAbsMax = new() { Length = 100 };

	private readonly decimal[] _aoHistory = new decimal[5];
	private int _aoSamples;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	public AwesomeOscTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signal generation", "General");

		_closeTrade = Param(nameof(CloseTrade), true)
			.SetDisplay("Close On Opposite Signal", "Close positions when an opposite Awesome Oscillator signal appears", "Risk");

		_profitTypeClTrd = Param(nameof(ProfitTypeClTrd), 1)
			.SetDisplay("Close Profit Filter", "0=all, 1=only profitable, 2=only losing", "Risk");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Length of the Bollinger Bands filter", "Filters");

		_bollingerSigma = Param(nameof(BollingerSigma), 2m)
			.SetDisplay("Bollinger Sigma", "Standard deviation multiplier for Bollinger Bands", "Filters");

		_bollingerSpreadLowerLimit = Param(nameof(BollingerSpreadLowerLimit), 55m)
			.SetDisplay("Bollinger Spread Min", "Minimum Bollinger band width in pips", "Filters");

		_bollingerSpreadUpperLimit = Param(nameof(BollingerSpreadUpperLimit), 380m)
			.SetDisplay("Bollinger Spread Max", "Maximum Bollinger band width in pips", "Filters");

		_periodFast = Param(nameof(PeriodFast), 3)
			.SetDisplay("AO Fast Period", "Fast moving average period inside the Awesome Oscillator", "Awesome Oscillator");

		_periodSlow = Param(nameof(PeriodSlow), 32)
			.SetDisplay("AO Slow Period", "Slow moving average period inside the Awesome Oscillator", "Awesome Oscillator");

		_aoStrengthLimit = Param(nameof(AoStrengthLimit), 0.13m)
			.SetDisplay("AO Strength Threshold", "Minimum normalized Awesome Oscillator value required for entries", "Awesome Oscillator");

		_stochK = Param(nameof(StochK), 8)
			.SetDisplay("Stochastic %K", "Stochastic main period", "Momentum");

		_stochD = Param(nameof(StochD), 3)
			.SetDisplay("Stochastic %D", "Stochastic signal period", "Momentum");

		_stochSlow = Param(nameof(StochSlow), 3)
			.SetDisplay("Stochastic Smoothing", "Additional smoothing for Stochastic", "Momentum");

		_stochLower = Param(nameof(StochLower), 18m)
			.SetDisplay("Stochastic Lower", "Lower bound for bullish signals", "Momentum");

		_stochUpper = Param(nameof(StochUpper), 76m)
			.SetDisplay("Stochastic Upper", "Upper bound for bearish signals", "Momentum");

		_entryHour = Param(nameof(EntryHour), 0)
			.SetDisplay("Entry Hour", "Start hour of the trading window (0-23)", "Schedule");

		_openHours = Param(nameof(OpenHours), 16)
			.SetDisplay("Trading Window Hours", "Number of consecutive hours available for trading", "Schedule");

		_lots = Param(nameof(Lots), 0.01m)
			.SetDisplay("Lot Size", "Order volume used for market entries", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 200m)
			.SetDisplay("Take Profit (pips)", "Distance to the take profit target in pips", "Risk");

		_stopLoss = Param(nameof(StopLoss), 80m)
			.SetDisplay("Stop Loss (pips)", "Distance to the stop loss in pips", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 40m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public bool CloseTrade
	{
		get => _closeTrade.Value;
		set => _closeTrade.Value = value;
	}

	public int ProfitTypeClTrd
	{
		get => _profitTypeClTrd.Value;
		set => _profitTypeClTrd.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerSigma
	{
		get => _bollingerSigma.Value;
		set => _bollingerSigma.Value = value;
	}

	public decimal BollingerSpreadLowerLimit
	{
		get => _bollingerSpreadLowerLimit.Value;
		set => _bollingerSpreadLowerLimit.Value = value;
	}

	public decimal BollingerSpreadUpperLimit
	{
		get => _bollingerSpreadUpperLimit.Value;
		set => _bollingerSpreadUpperLimit.Value = value;
	}

	public int PeriodFast
	{
		get => _periodFast.Value;
		set => _periodFast.Value = value;
	}

	public int PeriodSlow
	{
		get => _periodSlow.Value;
		set => _periodSlow.Value = value;
	}

	public decimal AoStrengthLimit
	{
		get => _aoStrengthLimit.Value;
		set => _aoStrengthLimit.Value = value;
	}

	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}

	public int StochD
	{
		get => _stochD.Value;
		set => _stochD.Value = value;
	}

	public int StochSlow
	{
		get => _stochSlow.Value;
		set => _stochSlow.Value = value;
	}

	public decimal StochLower
	{
		get => _stochLower.Value;
		set => _stochLower.Value = value;
	}

	public decimal StochUpper
	{
		get => _stochUpper.Value;
		set => _stochUpper.Value = value;
	}

	public int EntryHour
	{
		get => _entryHour.Value;
		set => _entryHour.Value = value;
	}

	public int OpenHours
	{
		get => _openHours.Value;
		set => _openHours.Value = value;
	}

	public decimal Lots
	{
		get => _lots.Value;
		set
		{
			_lots.Value = value;
			Volume = value;
		}
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = Lots;

		_bollinger.Length = BollingerPeriod;
		_bollinger.Width = BollingerSigma;

		_stochastic.KPeriod = StochK;
		_stochastic.DPeriod = StochD;
		_stochastic.Slowing = StochSlow;

		_awesome.ShortPeriod = PeriodFast;
		_awesome.LongPeriod = PeriodSlow;

		_aoAbsMax.Length = Math.Max(100, PeriodSlow * 3);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bollinger, _stochastic, _awesome, ProcessCandle)
			.Start();

		var stopLossUnit = StopLoss > 0m ? new Unit(GetPriceOffset(StopLoss), UnitTypes.Absolute) : null;
		var takeProfitUnit = TakeProfit > 0m ? new Unit(GetPriceOffset(TakeProfit), UnitTypes.Absolute) : null;
		var trailingUnit = TrailingStop > 0m ? new Unit(GetPriceOffset(TrailingStop), UnitTypes.Absolute) : null;

		StartProtection(
			stopLoss: stopLossUnit,
			takeProfit: takeProfitUnit,
			trailingStop: trailingUnit,
			trailingStep: trailingUnit);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue stochValue, IIndicatorValue aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bollingerValue is not BollingerBandsValue bands)
			return;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower)
			return;

		if (stochValue is not StochasticOscillatorValue stoch || stoch.K is not decimal stochMain)
			return;

		if (!aoValue.IsFinal)
			return;

		var aoRaw = aoValue.GetValue<decimal>();
		var maxValue = _aoAbsMax.Process(new DecimalIndicatorValue(_aoAbsMax, Math.Abs(aoRaw), candle.OpenTime));
		var aoMax = maxValue.IsFormed ? maxValue.GetValue<decimal>() : Math.Abs(aoRaw);
		if (aoMax <= 0m)
			return;

		for (var i = _aoHistory.Length - 1; i > 0; i--)
			_aoHistory[i] = _aoHistory[i - 1];

		var aoNormalized = aoRaw / aoMax;
		_aoHistory[0] = aoNormalized;
		if (_aoSamples < _aoHistory.Length)
			_aoSamples++;

		var step = Security?.PriceStep ?? 0m;
		var spread = upper - lower;
		if (step > 0m)
			spread /= step;

		var inTradingWindow = IsInsideTradingWindow(candle.OpenTime);
		var withinSpread = spread >= BollingerSpreadLowerLimit && spread <= BollingerSpreadUpperLimit;

		var buySignal = false;
		var sellSignal = false;

		if (inTradingWindow && withinSpread && _aoSamples >= _aoHistory.Length)
		{
			buySignal = IsBuySignal(stochMain);
			sellSignal = IsSellSignal(stochMain);
		}

		ManagePositions(candle, aoNormalized, buySignal, sellSignal);
	}

	private bool IsBuySignal(decimal stochasticMain)
	{
		if (stochasticMain <= StochLower)
			return false;

		if (Math.Abs(_aoHistory[0]) <= AoStrengthLimit)
			return false;

		if (_aoHistory[4] >= 0m || _aoHistory[3] >= 0m || _aoHistory[2] >= 0m)
			return false;

		if (!(_aoHistory[1] < _aoHistory[2]))
			return false;

		if (!(_aoHistory[0] > _aoHistory[1]))
			return false;

		return _aoHistory[0] < 0m;
	}

	private bool IsSellSignal(decimal stochasticMain)
	{
		if (stochasticMain >= StochUpper)
			return false;

		if (Math.Abs(_aoHistory[0]) <= AoStrengthLimit)
			return false;

		if (_aoHistory[4] <= 0m || _aoHistory[3] <= 0m || _aoHistory[2] <= 0m)
			return false;

		if (!(_aoHistory[1] > _aoHistory[2]))
			return false;

		if (!(_aoHistory[0] < _aoHistory[1]))
			return false;

		return _aoHistory[0] > 0m;
	}

	private void ManagePositions(ICandleMessage candle, decimal aoNormalized, bool buySignal, bool sellSignal)
	{
		var price = candle.ClosePrice;

		if (CloseTrade && Position > 0 && (sellSignal || aoNormalized >= 0m) && CanCloseLong(price))
		{
			SellMarket(Position);
			_longEntryPrice = null;
		}
		else if (CloseTrade && Position < 0 && (buySignal || aoNormalized <= 0m) && CanCloseShort(price))
		{
			BuyMarket(-Position);
			_shortEntryPrice = null;
		}

		if (buySignal && Position <= 0 && Lots > 0m)
		{
			if (Position < 0)
			{
				BuyMarket(-Position);
				_shortEntryPrice = null;
			}

			BuyMarket();
			_longEntryPrice = price;
		}
		else if (sellSignal && Position >= 0 && Lots > 0m)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				_longEntryPrice = null;
			}

			SellMarket();
			_shortEntryPrice = price;
		}
	}

	private bool CanCloseLong(decimal price)
	{
		return ProfitTypeClTrd switch
		{
			0 => true,
			1 => _longEntryPrice is decimal entry && price >= entry,
			2 => _longEntryPrice is decimal entry && price <= entry,
			_ => true,
		};
	}

	private bool CanCloseShort(decimal price)
	{
		return ProfitTypeClTrd switch
		{
			0 => true,
			1 => _shortEntryPrice is decimal entry && entry >= price,
			2 => _shortEntryPrice is decimal entry && entry <= price,
			_ => true,
		};
	}

	private bool IsInsideTradingWindow(DateTimeOffset time)
	{
		var start = Math.Clamp(EntryHour, 0, 23);
		var length = Math.Max(0, OpenHours);
		var hour = time.Hour;
		var close = (start + length) % 24;
		var wraps = start + length > 23;

		if (length == 0)
			return hour == start;

		return wraps
			? hour >= start || hour <= close
			: hour >= start && hour <= close;
	}

	private decimal GetPriceOffset(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var step = Security?.PriceStep;
		if (step is null || step == 0m)
			return pips;

		return pips * step.Value;
	}

	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position <= 0)
			_longEntryPrice = null;

		if (Position >= 0)
			_shortEntryPrice = null;
	}
}
