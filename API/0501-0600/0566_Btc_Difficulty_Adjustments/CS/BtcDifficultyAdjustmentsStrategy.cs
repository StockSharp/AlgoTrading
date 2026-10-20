namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Trades published Bitcoin difficulty adjustments without substituting price momentum.</summary>
public class BtcDifficultyAdjustmentsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _thresholdMode;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<string> _difficultyHistoryFile;
	private readonly List<(DateTime time, decimal difficulty)> _observations = [];
	private int _cursor;
	private decimal? _previous;
	private DateTime _lastTime;
	private Order _pending;

	/// <summary>Finished decision candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Use contrarian thresholded adjustments.</summary>
	public bool ThresholdMode
	{
		get => _thresholdMode.Value;
		set => _thresholdMode.Value = value;
	}

	/// <summary>Minimum absolute difficulty change in percent.</summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>Required Time,Difficulty CSV with UTC publication timestamps.</summary>
	public string DifficultyHistoryFile
	{
		get => _difficultyHistoryFile.Value;
		set => _difficultyHistoryFile.Value = value;
	}

	/// <summary>Initializes the published difficulty strategy parameters.</summary>
	public BtcDifficultyAdjustmentsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("CandleType", "Finished decision candle interval", "Parameters");
		_thresholdMode = Param(nameof(ThresholdMode), false)
			.SetDisplay("ThresholdMode", "Use contrarian thresholded adjustments", "Parameters");
		_threshold = Param(nameof(Threshold), 10m).SetGreaterThanZero()
			.SetDisplay("Threshold", "Minimum absolute difficulty change in percent", "Parameters");
		_difficultyHistoryFile = Param(nameof(DifficultyHistoryFile), string.Empty)
			.SetDisplay("DifficultyHistoryFile", "Required Time,Difficulty CSV with UTC publication timestamps", "Parameters");
		OrderRegistering += order => _pending = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_observations.Clear();
		_cursor = 0;
		_previous = null;
		_lastTime = default;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (string.IsNullOrWhiteSpace(DifficultyHistoryFile) || !File.Exists(DifficultyHistoryFile))
			throw new InvalidOperationException("DifficultyHistoryFile must identify an existing Time,Difficulty CSV.");
		_observations.Clear();
		var lines = File.ReadAllLines(DifficultyHistoryFile);
		if (lines.Length < 3 || lines[0].Trim() != "Time,Difficulty")
			throw new FormatException("DifficultyHistoryFile requires Time,Difficulty and at least two observations.");
		foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
		{
			var cells = line.Split(',');
			if (cells.Length != 2 || !cells[0].Trim().EndsWith("Z", StringComparison.OrdinalIgnoreCase))
				throw new FormatException("DifficultyHistoryFile timestamps must be ISO UTC values ending in Z.");
			var stamp = DateTime.Parse(cells[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
			var value = decimal.Parse(cells[1], CultureInfo.InvariantCulture);
			if (value <= 0m || _observations.Count > 0 && stamp <= _observations[^1].time)
				throw new FormatException("DifficultyHistoryFile requires positive values and strictly increasing timestamps.");
			_observations.Add((stamp, value));
		}
		if (_observations.Count < 2)
			throw new FormatException("DifficultyHistoryFile requires at least two observations.");
		base.OnStarted2(time);
		var candles = SubscribeCandles(CandleType);
		candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || candle.OpenTime <= _lastTime)
			return;
		_lastTime = candle.OpenTime;
		Sides? signal = null;
		while (_cursor < _observations.Count && _observations[_cursor].time <= candle.OpenTime)
		{
			var value = _observations[_cursor++].difficulty;
			if (_previous is decimal prior && value != prior)
			{
				var change = (value / prior - 1m) * 100m;
				signal = !ThresholdMode || Math.Abs(change) >= Threshold
					? ThresholdMode ? change < 0m ? Sides.Buy : Sides.Sell : change > 0m ? Sides.Buy : Sides.Sell
					: null;
			}
			_previous = value;
		}
		if (signal is null || !IsFormedAndOnlineAndAllowTrading()
			|| _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (signal == Sides.Buy && Position <= 0m)
			BuyMarket(Volume + Math.Abs(Position));
		else if (signal == Sides.Sell && Position >= 0m)
			SellMarket(Volume + Math.Abs(Position));
	}
}
