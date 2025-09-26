using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader "VHF EA" expert advisor.
/// It uses the Vertical Horizontal Filter to detect trending regimes
/// and opens positions following the dominant price direction.
/// </summary>
public class VhfSlidingWindowsStrategy : Strategy
{
	private readonly StrategyParam<int> _mainWindow;
	private readonly StrategyParam<int> _workingWindow;
	private readonly StrategyParam<int> _vhfPeriod;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<DataType> _candleType;

	private VerticalHorizontalFilter _vhf = null!;
	private readonly List<decimal> _vhfHistory = new();
	private readonly List<decimal> _closeHistory = new();
	private bool _parameterErrorLogged;

	/// <summary>
	/// Number of VHF values used to compute the main regime filter.
	/// </summary>
	public int MainWindowSize
	{
		get => _mainWindow.Value;
		set => _mainWindow.Value = value;
	}

	/// <summary>
	/// Number of VHF values used for the working window filter.
	/// </summary>
	public int WorkingWindowSize
	{
		get => _workingWindow.Value;
		set => _workingWindow.Value = value;
	}

	/// <summary>
	/// Averaging period of the Vertical Horizontal Filter indicator.
	/// </summary>
	public int VhfPeriod
	{
		get => _vhfPeriod.Value;
		set => _vhfPeriod.Value = value;
	}


	/// <summary>
	/// Inverts trading direction when enabled.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes default parameters.
	/// </summary>
	public VhfSlidingWindowsStrategy()
	{
		_mainWindow = Param(nameof(MainWindowSize), 11)
			.SetGreaterThanZero()
			.SetDisplay("Main Window", "Number of VHF values used for the primary filter", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_workingWindow = Param(nameof(WorkingWindowSize), 7)
			.SetGreaterThanZero()
			.SetDisplay("Working Window", "Number of VHF values used for the secondary filter", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_vhfPeriod = Param(nameof(VhfPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("VHF Period", "Lookback period of the Vertical Horizontal Filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);


		_reverseSignals = Param(nameof(ReverseSignals), true)
			.SetDisplay("Reverse", "Invert buy and sell signals", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_parameterErrorLogged = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_vhf = new VerticalHorizontalFilter
		{
			Length = VhfPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_vhf, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vhf);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vhfValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateHistory(_closeHistory, candle.ClosePrice, MainWindowSize);
		UpdateHistory(_vhfHistory, vhfValue, MainWindowSize);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (MainWindowSize <= 0 || WorkingWindowSize <= 0 || MainWindowSize <= WorkingWindowSize)
		{
			if (!_parameterErrorLogged)
			{
				LogError($"Invalid window configuration. Main={MainWindowSize}, Working={WorkingWindowSize}.");
				_parameterErrorLogged = true;
			}

			return;
		}

		if (!_vhf.IsFormed)
			return;

		if (_closeHistory.Count < MainWindowSize || _vhfHistory.Count < MainWindowSize)
			return;

		var currentVhf = _vhfHistory[^1];

		var mainMax = decimal.MinValue;
		var mainMin = decimal.MaxValue;
		for (var i = 0; i < _vhfHistory.Count; i++)
		{
			var value = _vhfHistory[i];
			if (value > mainMax)
				mainMax = value;
			if (value < mainMin)
				mainMin = value;
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
			if (value > workingMax)
				workingMax = value;
			if (value < workingMin)
				workingMin = value;
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
			ClosePosition();
			LogInfo($"Trend filter switched to range mode at {candle.ClosePrice}. Closing open position.");
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
				{
					BuyMarket(volumeToBuy);
					LogInfo($"Opening long position at {candle.ClosePrice}. VHF={_vhfHistory[^1]:F4}.");
				}
			}
		}
		else
		{
			if (Position >= 0)
			{
				var volumeToSell = Volume + Math.Abs(Position);
				if (volumeToSell > 0)
				{
					SellMarket(volumeToSell);
					LogInfo($"Opening short position at {candle.ClosePrice}. VHF={_vhfHistory[^1]:F4}.");
				}
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

	private sealed class VerticalHorizontalFilter : LengthIndicator<decimal>
	{
		private readonly Queue<decimal> _highs = new();
		private readonly Queue<decimal> _lows = new();
		private readonly Queue<decimal> _closes = new();

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
				return new DecimalIndicatorValue(this, default, input.Time);

			_highs.Enqueue(candle.HighPrice);
			_lows.Enqueue(candle.LowPrice);
			_closes.Enqueue(candle.ClosePrice);

			while (_highs.Count > Length)
			{
				_highs.Dequeue();
			}

			while (_lows.Count > Length)
			{
				_lows.Dequeue();
			}

			var closeRequired = Length + 1;
			if (closeRequired < 2)
				closeRequired = 2;

			while (_closes.Count > closeRequired)
			{
				_closes.Dequeue();
			}

			if (_highs.Count < Length || _lows.Count < Length || _closes.Count < closeRequired)
				return new DecimalIndicatorValue(this, default, input.Time);

			var highest = decimal.MinValue;
			foreach (var value in _highs)
			{
				if (value > highest)
					highest = value;
			}

			var lowest = decimal.MaxValue;
			foreach (var value in _lows)
			{
				if (value < lowest)
					lowest = value;
			}

			var numerator = highest - lowest;

			decimal denominator = 0m;
			decimal? previous = null;
			foreach (var close in _closes)
			{
				if (previous != null)
					denominator += Math.Abs(close - previous.Value);

				previous = close;
			}

			var vhf = denominator != 0m ? numerator / denominator : 0m;
			return new DecimalIndicatorValue(this, vhf, input.Time);
		}

		public override void Reset()
		{
			base.Reset();

			_highs.Clear();
			_lows.Clear();
			_closes.Clear();
		}
	}
}
