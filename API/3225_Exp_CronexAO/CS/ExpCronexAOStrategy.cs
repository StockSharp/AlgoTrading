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
/// Cronex Awesome Oscillator crossover strategy.
/// Buys when the fast Cronex line crosses above the slow line and sells on the opposite cross.
/// </summary>
public class ExpCronexAOStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyOpenEnabled;
	private readonly StrategyParam<bool> _sellOpenEnabled;
	private readonly StrategyParam<bool> _buyCloseEnabled;
	private readonly StrategyParam<bool> _sellCloseEnabled;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;

	private AwesomeOscillator _awesomeOscillator = null!;
	private SimpleMovingAverage _fastAverage = null!;
	private SimpleMovingAverage _slowAverage = null!;

	private decimal[] _fastHistory = Array.Empty<decimal>();
	private decimal[] _slowHistory = Array.Empty<decimal>();
	private int _historyCount;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public bool BuyOpenEnabled
	{
		get => _buyOpenEnabled.Value;
		set => _buyOpenEnabled.Value = value;
	}

	public bool SellOpenEnabled
	{
		get => _sellOpenEnabled.Value;
		set => _sellOpenEnabled.Value = value;
	}

	public bool BuyCloseEnabled
	{
		get => _buyCloseEnabled.Value;
		set => _buyCloseEnabled.Value = value;
	}

	public bool SellCloseEnabled
	{
		get => _sellCloseEnabled.Value;
		set => _sellCloseEnabled.Value = value;
	}

	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public ExpCronexAOStrategy()
	{
		Volume = 1m;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Cronex Timeframe", "Time frame for Cronex AO candles", "General");

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast Cronex smoothing period", "Cronex AO")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow Cronex smoothing period", "Cronex AO")
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualTo(1)
			.SetDisplay("Signal Bar", "Bars back to evaluate cross", "Cronex AO")
			.SetCanOptimize(true)
			.SetOptimize(1, 3, 1);

		_buyOpenEnabled = Param(nameof(BuyOpenEnabled), true)
			.SetDisplay("Allow Long Entries", "Enable opening buy trades", "Trading");

		_sellOpenEnabled = Param(nameof(SellOpenEnabled), true)
			.SetDisplay("Allow Short Entries", "Enable opening sell trades", "Trading");

		_buyCloseEnabled = Param(nameof(BuyCloseEnabled), true)
			.SetDisplay("Close Long", "Allow closing long positions on sell signal", "Trading");

		_sellCloseEnabled = Param(nameof(SellCloseEnabled), true)
			.SetDisplay("Close Short", "Allow closing short positions on buy signal", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Take Profit", "Profit target in points", "Protection")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Stop Loss", "Loss limit in points", "Protection")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetHistory();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetHistory();

		_awesomeOscillator = new AwesomeOscillator();
		_fastAverage = new SimpleMovingAverage { Length = FastPeriod };
		_slowAverage = new SimpleMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_awesomeOscillator, _fastAverage, _slowAverage, ProcessCronex)
			.Start();

		StartProtection();
	}

	private void ProcessCronex(ICandleMessage candle, decimal aoValue, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastAverage.IsFormed || !_slowAverage.IsFormed)
			return;

		EnsureHistoryCapacity();
		ShiftHistory(fastValue, slowValue);

		var required = SignalBar + 2;
		if (_historyCount < required)
			return;

		var fastCurrent = _fastHistory[SignalBar];
		var slowCurrent = _slowHistory[SignalBar];
		var fastPrevious = _fastHistory[SignalBar + 1];
		var slowPrevious = _slowHistory[SignalBar + 1];

		var bullishNow = fastCurrent > slowCurrent;
		var bearishNow = fastCurrent < slowCurrent;
		var bullishCross = bullishNow && fastPrevious <= slowPrevious;
		var bearishCross = bearishNow && fastPrevious >= slowPrevious;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bullishNow)
		{
			HandleBullishSignal(candle.ClosePrice, bullishCross);
		}
		else if (bearishNow)
		{
			HandleBearishSignal(candle.ClosePrice, bearishCross);
		}
	}

	private void HandleBullishSignal(decimal price, bool bullishCross)
	{
		var closingVolume = 0m;
		if (SellCloseEnabled && Position < 0m)
			closingVolume = Math.Abs(Position);

		var openingVolume = 0m;
		if (bullishCross && BuyOpenEnabled && Position <= 0m)
		{
			if (Position < 0m && !SellCloseEnabled)
				return;

			openingVolume = Volume;
		}

		ExecuteBuy(price, closingVolume, openingVolume);
	}

	private void HandleBearishSignal(decimal price, bool bearishCross)
	{
		var closingVolume = 0m;
		if (BuyCloseEnabled && Position > 0m)
			closingVolume = Position;

		var openingVolume = 0m;
		if (bearishCross && SellOpenEnabled && Position >= 0m)
		{
			if (Position > 0m && !BuyCloseEnabled)
				return;

			openingVolume = Volume;
		}

		ExecuteSell(price, closingVolume, openingVolume);
	}

	private void ExecuteBuy(decimal price, decimal closingVolume, decimal openingVolume)
	{
		var totalVolume = closingVolume + openingVolume;
		if (totalVolume <= 0m)
			return;

		var resultingPosition = Position + totalVolume;
		BuyMarket(totalVolume);

		if (openingVolume > 0m)
			ApplyProtection(price, resultingPosition);
	}

	private void ExecuteSell(decimal price, decimal closingVolume, decimal openingVolume)
	{
		var totalVolume = closingVolume + openingVolume;
		if (totalVolume <= 0m)
			return;

		var resultingPosition = Position - totalVolume;
		SellMarket(totalVolume);

		if (openingVolume > 0m)
			ApplyProtection(price, resultingPosition);
	}

	private void ApplyProtection(decimal price, decimal resultingPosition)
	{
		if (TakeProfit > 0)
			SetTakeProfit(TakeProfit, price, resultingPosition);

		if (StopLoss > 0)
			SetStopLoss(StopLoss, price, resultingPosition);
	}

	private void ResetHistory()
	{
		_fastHistory = Array.Empty<decimal>();
		_slowHistory = Array.Empty<decimal>();
		_historyCount = 0;
	}

	private void EnsureHistoryCapacity()
	{
		var required = SignalBar + 2;
		if (_fastHistory.Length == required)
			return;

		_fastHistory = new decimal[required];
		_slowHistory = new decimal[required];
		_historyCount = 0;
	}

	private void ShiftHistory(decimal fastValue, decimal slowValue)
	{
		var length = _fastHistory.Length;
		if (length == 0)
			return;

		for (var i = Math.Min(_historyCount, length - 1); i > 0; i--)
		{
			_fastHistory[i] = _fastHistory[i - 1];
			_slowHistory[i] = _slowHistory[i - 1];
		}

		_fastHistory[0] = fastValue;
		_slowHistory[0] = slowValue;

		if (_historyCount < length)
			_historyCount++;
	}
}


