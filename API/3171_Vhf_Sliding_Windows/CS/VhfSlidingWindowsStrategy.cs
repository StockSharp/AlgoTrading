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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using the Vertical Horizontal Filter to detect trending regimes
/// and opens positions following the dominant price direction.
/// </summary>
public class VhfSlidingWindowsStrategy : Strategy
{
	private readonly StrategyParam<int> _mainWindow;
	private readonly StrategyParam<int> _workingWindow;
	private readonly StrategyParam<int> _vhfPeriod;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _vhfHistory = new();
	private readonly List<decimal> _closeHistory = new();
	private readonly List<decimal> _highHistory = new();
	private readonly List<decimal> _lowHistory = new();
	private readonly List<decimal> _closeForVhf = new();
	private bool _parameterErrorLogged;

	public int MainWindowSize
	{
		get => _mainWindow.Value;
		set => _mainWindow.Value = value;
	}

	public int WorkingWindowSize
	{
		get => _workingWindow.Value;
		set => _workingWindow.Value = value;
	}

	public int VhfPeriod
	{
		get => _vhfPeriod.Value;
		set => _vhfPeriod.Value = value;
	}

	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public VhfSlidingWindowsStrategy()
	{
		_mainWindow = Param(nameof(MainWindowSize), 11)
			.SetGreaterThanZero()
			.SetDisplay("Main Window", "Number of VHF values used for the primary filter", "Filters")
			.SetOptimize(5, 30, 1);

		_workingWindow = Param(nameof(WorkingWindowSize), 7)
			.SetGreaterThanZero()
			.SetDisplay("Working Window", "Number of VHF values used for the secondary filter", "Filters")
			.SetOptimize(3, 20, 1);

		_vhfPeriod = Param(nameof(VhfPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("VHF Period", "Lookback period of the Vertical Horizontal Filter", "Indicators")
			.SetOptimize(5, 40, 1);

		_reverseSignals = Param(nameof(ReverseSignals), true)
			.SetDisplay("Reverse", "Invert buy and sell signals", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candles for calculations", "Data");
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

		_vhfHistory.Clear();
		_closeHistory.Clear();
		_highHistory.Clear();
		_lowHistory.Clear();
		_closeForVhf.Clear();
		_parameterErrorLogged = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(null, null);

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

		// Accumulate data for VHF calculation
		_highHistory.Add(candle.HighPrice);
		_lowHistory.Add(candle.LowPrice);
		_closeForVhf.Add(candle.ClosePrice);

		while (_highHistory.Count > VhfPeriod)
			_highHistory.RemoveAt(0);
		while (_lowHistory.Count > VhfPeriod)
			_lowHistory.RemoveAt(0);

		var closeRequired = VhfPeriod + 1;
		if (closeRequired < 2) closeRequired = 2;
		while (_closeForVhf.Count > closeRequired)
			_closeForVhf.RemoveAt(0);

		// Compute VHF if enough data
		if (_highHistory.Count >= VhfPeriod && _lowHistory.Count >= VhfPeriod && _closeForVhf.Count >= closeRequired)
		{
			var highest = decimal.MinValue;
			for (var i = 0; i < _highHistory.Count; i++)
				if (_highHistory[i] > highest) highest = _highHistory[i];

			var lowest = decimal.MaxValue;
			for (var i = 0; i < _lowHistory.Count; i++)
				if (_lowHistory[i] < lowest) lowest = _lowHistory[i];

			var numerator = highest - lowest;
			var denominator = 0m;
			for (var i = 1; i < _closeForVhf.Count; i++)
				denominator += Math.Abs(_closeForVhf[i] - _closeForVhf[i - 1]);

			var vhfValue = denominator != 0m ? numerator / denominator : 0m;

			UpdateHistory(_vhfHistory, vhfValue, MainWindowSize);
		}

		UpdateHistory(_closeHistory, candle.ClosePrice, MainWindowSize);

		if (MainWindowSize <= 0 || WorkingWindowSize <= 0 || MainWindowSize <= WorkingWindowSize)
		{
			if (!_parameterErrorLogged)
			{
				LogError($"Invalid window configuration. Main={MainWindowSize}, Working={WorkingWindowSize}.");
				_parameterErrorLogged = true;
			}
			return;
		}

		if (_closeHistory.Count < MainWindowSize || _vhfHistory.Count < MainWindowSize)
			return;

		var currentVhf = _vhfHistory[^1];

		var mainMax = decimal.MinValue;
		var mainMin = decimal.MaxValue;
		for (var i = 0; i < _vhfHistory.Count; i++)
		{
			var value = _vhfHistory[i];
			if (value > mainMax) mainMax = value;
			if (value < mainMin) mainMin = value;
		}

		var mainMid = (mainMax + mainMin) / 2m;

		var workingCount = WorkingWindowSize;
		if (workingCount > _vhfHistory.Count)
			workingCount = _vhfHistory.Count;

		var workingMax = decimal.MinValue;
		var workingMin = decimal.MaxValue;
		for (var i = _vhfHistory.Count - workingCount; i < _vhfHistory.Count; i++)
		{
			var value = _vhfHistory[i];
			if (value > workingMax) workingMax = value;
			if (value < workingMin) workingMin = value;
		}

		var workingMid = (workingMax + workingMin) / 2m;

		if (currentVhf > mainMid && currentVhf > workingMid)
		{
			var latestClose = _closeHistory[^1];
			var referenceClose = _closeHistory[0];

			if (latestClose > referenceClose)
			{
				ExecuteDirectionalSignal(true, candle);
			}
			else if (latestClose < referenceClose)
			{
				ExecuteDirectionalSignal(false, candle);
			}
		}
		else if (Position != 0)
		{
			// Range mode - close position
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));
		}
	}

	private void ExecuteDirectionalSignal(bool trendUp, ICandleMessage candle)
	{
		var shouldBuy = trendUp;
		if (ReverseSignals)
			shouldBuy = !shouldBuy;

		if (shouldBuy)
		{
			if (Position <= 0)
			{
				var volumeToBuy = Volume + Math.Abs(Position);
				if (volumeToBuy > 0)
					BuyMarket(volumeToBuy);
			}
		}
		else
		{
			if (Position >= 0)
			{
				var volumeToSell = Volume + Math.Abs(Position);
				if (volumeToSell > 0)
					SellMarket(volumeToSell);
			}
		}
	}

	private static void UpdateHistory(List<decimal> history, decimal value, int maxCount)
	{
		history.Add(value);

		if (maxCount <= 0)
		{
			history.Clear();
			return;
		}

		while (history.Count > maxCount)
		{
			history.RemoveAt(0);
		}
	}
}
