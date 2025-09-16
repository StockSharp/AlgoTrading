using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades pullbacks using the Center of Gravity oscillator on two timeframes.
/// </summary>
public class CGOscillatorX2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<bool> _buyCloseSignal;
	private readonly StrategyParam<bool> _sellCloseSignal;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private CenterOfGravityOscillatorIndicator _trendIndicator;
	private CenterOfGravityOscillatorIndicator _signalIndicator;

	private int _trendDirection;
	private decimal? _signalPrevMain;
	private decimal? _signalPrevSignal;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	public DataType TrendCandleType { get => _trendCandleType.Value; set => _trendCandleType.Value = value; }
	public DataType SignalCandleType { get => _signalCandleType.Value; set => _signalCandleType.Value = value; }
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public bool BuyCloseSignal { get => _buyCloseSignal.Value; set => _buyCloseSignal.Value = value; }
	public bool SellCloseSignal { get => _sellCloseSignal.Value; set => _sellCloseSignal.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	public CGOscillatorX2Strategy()
	{
		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Trend Candle Type", "Higher timeframe for trend detection", "General");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Signal Candle Type", "Lower timeframe for trade execution", "General");

		_trendLength = Param(nameof(TrendLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "CG length on the trend timeframe", "Indicator");

		_signalLength = Param(nameof(SignalLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "CG length on the signal timeframe", "Indicator");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Long Entries", "Enable long entries during uptrend", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Short Entries", "Enable short entries during downtrend", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close Long On Trend Flip", "Exit long positions when higher trend turns bearish", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close Short On Trend Flip", "Exit short positions when higher trend turns bullish", "Trading");

		_buyCloseSignal = Param(nameof(BuyCloseSignal), false)
			.SetDisplay("Close Long On Pullback", "Exit long positions when the oscillator confirms a bearish hook", "Trading");

		_sellCloseSignal = Param(nameof(SellCloseSignal), false)
			.SetDisplay("Close Short On Pullback", "Exit short positions when the oscillator confirms a bullish hook", "Trading");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Distance", "Absolute stop-loss distance in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit Distance", "Absolute take-profit distance in price units", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TrendCandleType);

		if (!TrendCandleType.Equals(SignalCandleType))
			yield return (Security, SignalCandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_trendIndicator = new CenterOfGravityOscillatorIndicator
		{
			Length = TrendLength
		};

		_signalIndicator = new CenterOfGravityOscillatorIndicator
		{
			Length = SignalLength
		};

		SubscribeCandles(TrendCandleType)
			.BindEx(_trendIndicator, ProcessTrend)
			.Start();

		SubscribeCandles(SignalCandleType)
			.BindEx(_signalIndicator, ProcessSignal)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trendDirection = 0;
		_signalPrevMain = null;
		_signalPrevSignal = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void ProcessTrend(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_trendIndicator.IsFormed)
			return;

		var cg = (CenterOfGravityOscillatorValue)value;

		if (cg.Main > cg.Signal)
			_trendDirection = 1;
		else if (cg.Main < cg.Signal)
			_trendDirection = -1;
		else
			_trendDirection = 0;
	}

	private void ProcessSignal(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_signalIndicator.IsFormed)
			return;

		var cg = (CenterOfGravityOscillatorValue)value;

		var prevMain = _signalPrevMain;
		var prevSignal = _signalPrevSignal;

		_signalPrevMain = cg.Main;
		_signalPrevSignal = cg.Signal;

		if (prevMain is null || prevSignal is null)
			return;

		if (TryCloseByRisk(candle))
			return;

		var closeBuy = BuyCloseSignal && prevMain < prevSignal;
		var closeSell = SellCloseSignal && prevMain > prevSignal;
		var openBuy = false;
		var openSell = false;

		if (_trendDirection < 0)
		{
			if (BuyClose)
				closeBuy = true;

			if (SellOpen && cg.Main >= cg.Signal && prevMain < prevSignal)
				openSell = true;
		}
		else if (_trendDirection > 0)
		{
			if (SellClose)
				closeSell = true;

			if (BuyOpen && cg.Main <= cg.Signal && prevMain > prevSignal)
				openBuy = true;
		}

		if (closeBuy && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			ResetRiskTargets();
		}

		if (closeSell && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ResetRiskTargets();
		}

		if (openBuy && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
			BuyMarket(volume);
			SetRiskTargets(candle.ClosePrice, true);
		}
		else if (openSell && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Math.Abs(Position) : 0m);
			SellMarket(volume);
			SetRiskTargets(candle.ClosePrice, false);
		}
	}

	private bool TryCloseByRisk(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetRiskTargets();
				return true;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(Position));
				ResetRiskTargets();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetRiskTargets();
				return true;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetRiskTargets();
				return true;
			}
		}

		return false;
	}

	private void SetRiskTargets(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		if (StopLoss > 0m)
			_stopPrice = isLong ? entryPrice - StopLoss : entryPrice + StopLoss;
		else
			_stopPrice = null;

		if (TakeProfit > 0m)
			_takePrice = isLong ? entryPrice + TakeProfit : entryPrice - TakeProfit;
		else
			_takePrice = null;
	}

	private void ResetRiskTargets()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}
}

/// <summary>
/// Center of Gravity oscillator indicator producing main and signal lines.
/// </summary>
public class CenterOfGravityOscillatorIndicator : BaseIndicator<decimal>
{
	private readonly Queue<decimal> _medianPrices = new();
	private decimal? _previousMain;

	public int Length { get; set; } = 10;

	private decimal Shift => (Length + 1m) / 2m;

	public override bool IsFormed => _medianPrices.Count >= Length;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DecimalIndicatorValue(this, default, input.Time);

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		_medianPrices.Enqueue(median);

		while (_medianPrices.Count > Length)
			_medianPrices.Dequeue();

		if (_medianPrices.Count < Length || Length <= 0)
		{
			_previousMain = null;
			return new DecimalIndicatorValue(this, default, input.Time);
		}

		decimal numerator = 0m;
		decimal denominator = 0m;
		var index = 1m;

		foreach (var price in _medianPrices)
		{
			numerator += index * price;
			denominator += price;
			index += 1m;
		}

		decimal main = 0m;

		if (denominator != 0m)
			main = -numerator / denominator + Shift;

		var signal = _previousMain ?? main;
		_previousMain = main;

		return new CenterOfGravityOscillatorValue(this, input, main, signal);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_medianPrices.Clear();
		_previousMain = null;
	}
}

/// <summary>
/// Indicator value for <see cref="CenterOfGravityOscillatorIndicator"/>.
/// </summary>
public class CenterOfGravityOscillatorValue : ComplexIndicatorValue
{
	public CenterOfGravityOscillatorValue(IIndicator indicator, IIndicatorValue input, decimal main, decimal signal)
	: base(indicator, input, (nameof(Main), main), (nameof(Signal), signal))
	{
	}

	/// <summary>
	/// Main oscillator value.
	/// </summary>
	public decimal Main => (decimal)GetValue(nameof(Main));

	/// <summary>
	/// Signal value (previous oscillator value).
	/// </summary>
	public decimal Signal => (decimal)GetValue(nameof(Signal));
}
