namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo.Candles;

/// <summary>
/// Strategy converted from the Exp_FisherCGOscillator MetaTrader expert advisor.
/// It reproduces the Fisher Center of Gravity oscillator crossover logic with optional stop and target management.
/// </summary>
public class ExpFisherCgOscillatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _depositShare;
	private readonly StrategyParam<PositionSizingMode> _sizingMode;

	private FisherCgOscillatorIndicator _oscillator = null!;
	private readonly List<(decimal Main, decimal Trigger)> _oscillatorHistory = new();
	private decimal? _entryPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="ExpFisherCgOscillatorStrategy"/>.
	/// </summary>
	public ExpFisherCgOscillatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe used for signals", "General");

		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Oscillator Length", "Number of bars in the Fisher CG calculation", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 25, 1);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Shift in closed candles used to read the oscillator", "Indicator");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entries", "Enable opening long positions", "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entries", "Enable opening short positions", "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exits", "Enable closing long positions on opposite signals", "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exits", "Enable closing short positions on opposite signals", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetNotNegative()
			.SetDisplay("Stop Loss Points", "Protective stop distance expressed in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetNotNegative()
			.SetDisplay("Take Profit Points", "Target distance expressed in price steps", "Risk");

		_fixedVolume = Param(nameof(FixedVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Volume", "Trade volume used in fixed-size mode", "Risk");

		_depositShare = Param(nameof(DepositShare), 0.1m)
			.SetNotNegative()
			.SetDisplay("Deposit Share", "Fraction of portfolio value allocated per trade", "Risk");

		_sizingMode = Param(nameof(SizingMode), PositionSizingMode.FixedVolume)
			.SetDisplay("Position Sizing", "Method used to convert the deposit share into volume", "Risk");
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fisher CG oscillator length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Number of closed candles used for signal evaluation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Allow opening long trades.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short trades.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on oscillator reversals.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on oscillator reversals.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in instrument steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in instrument steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Fixed trade volume when sizing mode is set to <see cref="PositionSizingMode.FixedVolume"/>.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Fraction of portfolio value allocated per trade in share-based sizing mode.
	/// </summary>
	public decimal DepositShare
	{
		get => _depositShare.Value;
		set => _depositShare.Value = value;
	}

	/// <summary>
	/// Position sizing behaviour.
	/// </summary>
	public PositionSizingMode SizingMode
	{
		get => _sizingMode.Value;
		set => _sizingMode.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_oscillatorHistory.Clear();
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_oscillator = new FisherCgOscillatorIndicator
		{
			Length = Length
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_oscillator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _oscillator);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		HandleRiskManagement(candle.ClosePrice);

		if (!_oscillator.IsFormed)
			return;

		var value = (FisherCgOscillatorValue)indicatorValue;
		if (value.Main is not decimal main || value.Trigger is not decimal trigger)
			return;

		AddToHistory(main, trigger);

		var required = SignalBar + 2;
		if (_oscillatorHistory.Count < required)
			return;

		var currentIndex = _oscillatorHistory.Count - 1 - SignalBar;
		if (currentIndex <= 0)
			return;

		var current = _oscillatorHistory[currentIndex];
		var previous = _oscillatorHistory[currentIndex - 1];

		// The previous bar determines the raw direction of the oscillator.
		var previousAbove = previous.Main > previous.Trigger;
		var previousBelow = previous.Main < previous.Trigger;

		// Entry signals require the previous direction plus a confirmed crossover on the signal bar.
		var buyOpen = previousAbove && current.Main <= current.Trigger;
		var sellOpen = previousBelow && current.Main >= current.Trigger;

		// Exit signals mirror the original EA: they depend only on the previous bar relationship.
		var buyClose = previousBelow;
		var sellClose = previousAbove;

		if (sellClose && AllowShortExit && Position < 0)
		{
			ClosePosition();
			_entryPrice = null;
		}

		if (buyClose && AllowLongExit && Position > 0)
		{
			ClosePosition();
			_entryPrice = null;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var closePrice = candle.ClosePrice;
		if (buyOpen && AllowLongEntry && Position <= 0)
		{
			if (Position < 0)
			{
				ClosePosition();
				_entryPrice = null;
				return;
			}

			var volume = CalculateOrderVolume(closePrice);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_entryPrice = closePrice;
			}
		}

		if (sellOpen && AllowShortEntry && Position >= 0)
		{
			if (Position > 0)
			{
				ClosePosition();
				_entryPrice = null;
				return;
			}

			var volume = CalculateOrderVolume(closePrice);
			if (volume > 0m)
			{
				SellMarket(volume);
				_entryPrice = closePrice;
			}
		}
	}

	private void AddToHistory(decimal main, decimal trigger)
	{
		_oscillatorHistory.Add((main, trigger));

		var maxItems = Math.Max(6, SignalBar + 6);
		while (_oscillatorHistory.Count > maxItems)
			_oscillatorHistory.RemoveAt(0);
	}

	private decimal CalculateOrderVolume(decimal referencePrice)
	{
		if (SizingMode == PositionSizingMode.FixedVolume)
			return FixedVolume;

		if (Portfolio is null || referencePrice <= 0m)
			return FixedVolume;

		var accountValue = Portfolio.CurrentValue;
		if (accountValue <= 0m || DepositShare <= 0m)
			return FixedVolume;

		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var rawVolume = accountValue * DepositShare / referencePrice;
		var steps = Math.Floor(rawVolume / step);
		if (steps <= 0m)
			return 0m;

		return steps * step;
	}

	private void HandleRiskManagement(decimal closePrice)
	{
		if (_entryPrice is null || Position == 0)
			return;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var stopDistance = StopLossPoints > 0 ? StopLossPoints * step : 0m;
		var takeDistance = TakeProfitPoints > 0 ? TakeProfitPoints * step : 0m;

		if (Position > 0)
		{
			if (stopDistance > 0m && closePrice <= _entryPrice.Value - stopDistance)
			{
				ClosePosition();
				_entryPrice = null;
				return;
			}

			if (takeDistance > 0m && closePrice >= _entryPrice.Value + takeDistance)
			{
				ClosePosition();
				_entryPrice = null;
				return;
			}
		}
		else if (Position < 0)
		{
			if (stopDistance > 0m && closePrice >= _entryPrice.Value + stopDistance)
			{
				ClosePosition();
				_entryPrice = null;
				return;
			}

			if (takeDistance > 0m && closePrice <= _entryPrice.Value - takeDistance)
			{
				ClosePosition();
				_entryPrice = null;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
			_entryPrice = null;
	}
}

/// <summary>
/// Position sizing modes supported by the strategy.
/// </summary>
public enum PositionSizingMode
{
	/// <summary>Always trade the configured fixed volume.</summary>
	FixedVolume,

	/// <summary>Convert a share of the portfolio value into contracts.</summary>
	PortfolioShare
}

/// <summary>
/// Fisher Center of Gravity oscillator with trigger line output.
/// </summary>
public sealed class FisherCgOscillatorIndicator : BaseIndicator<decimal>
{
	private readonly List<decimal> _medianPrices = new();
	private readonly List<decimal> _cgValues = new();
	private readonly decimal[] _valueBuffer = new decimal[4];
	private int _valueCount;
	private decimal? _previousFisher;
	private int _length = 10;

	/// <summary>
	/// Gets or sets the calculation length.
	/// </summary>
	public int Length
	{
		get => _length;
		set => _length = Math.Max(1, value);
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new FisherCgOscillatorValue(this, input, null, null);

		var price = (candle.HighPrice + candle.LowPrice) / 2m;
		_medianPrices.Add(price);
		while (_medianPrices.Count > Length)
			_medianPrices.RemoveAt(0);

		if (_medianPrices.Count < Length)
		{
			IsFormed = false;
			return new FisherCgOscillatorValue(this, input, null, null);
		}

		decimal num = 0m;
		decimal denom = 0m;
		var weight = 1;

		for (var index = _medianPrices.Count - 1; index >= 0; index--)
		{
			var median = _medianPrices[index];
			num += weight * median;
			denom += median;
			weight++;
		}

		decimal cg;
		if (denom != 0m)
			cg = -num / denom + (Length + 1m) / 2m;
		else
			cg = 0m;

		_cgValues.Add(cg);
		while (_cgValues.Count > Length)
			_cgValues.RemoveAt(0);

		var high = cg;
		var low = cg;
		for (var i = 0; i < _cgValues.Count; i++)
		{
			var value = _cgValues[i];
			if (value > high)
				high = value;
			if (value < low)
				low = value;
		}

		decimal normalized;
		if (high != low)
			normalized = (cg - low) / (high - low);
		else
			normalized = 0m;

		var limit = Math.Min(_valueCount, 3);
		for (var shift = limit; shift > 0; shift--)
			_valueBuffer[shift] = _valueBuffer[shift - 1];

		_valueBuffer[0] = normalized;
		if (_valueCount < 4)
			_valueCount++;

		if (_valueCount < 4)
		{
			IsFormed = false;
			return new FisherCgOscillatorValue(this, input, null, null);
		}

		var value2 = (4m * _valueBuffer[0] + 3m * _valueBuffer[1] + 2m * _valueBuffer[2] + _valueBuffer[3]) / 10m;
		var x = 1.98m * (value2 - 0.5m);
		if (x > 0.999m)
			x = 0.999m;
		else if (x < -0.999m)
			x = -0.999m;

		var numerator = 1m + x;
		var denominator = 1m - x;
		if (denominator == 0m)
			denominator = 0.0000001m;

		var ratio = numerator / denominator;
		if (ratio <= 0m)
			ratio = 0.0000001m;

		var fisher = 0.5m * (decimal)Math.Log((double)ratio);
		var trigger = _previousFisher ?? fisher;
		_previousFisher = fisher;
		IsFormed = true;

		return new FisherCgOscillatorValue(this, input, fisher, trigger);
	}

	/// <inheritdoc />
	protected override void OnReset()
	{
		base.OnReset();

		_medianPrices.Clear();
		_cgValues.Clear();
		Array.Clear(_valueBuffer, 0, _valueBuffer.Length);
		_valueCount = 0;
		_previousFisher = null;
		IsFormed = false;
	}
}

/// <summary>
/// Complex indicator value exposing both oscillator and trigger line.
/// </summary>
public sealed class FisherCgOscillatorValue : ComplexIndicatorValue
{
	public FisherCgOscillatorValue(IIndicator indicator, IIndicatorValue input, decimal? main, decimal? trigger)
		: base(indicator, input, (nameof(Main), main), (nameof(Trigger), trigger))
	{
	}

	/// <summary>Gets the oscillator main value.</summary>
	public decimal? Main => (decimal?)GetValue(nameof(Main));

	/// <summary>Gets the trigger line value.</summary>
	public decimal? Trigger => (decimal?)GetValue(nameof(Trigger));
}

