using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades CCI oscillator zero-line crossovers with signal MA smoothing.
/// Originally based on Chaikin Oscillator concept, adapted to use CCI.
/// </summary>
public class ChoSmoothedEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _maPeriod;

	private CommodityChannelIndex _cci;
	private readonly Queue<decimal> _cciHistory = new();
	private decimal? _prevCci;
	private decimal? _prevSignal;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChoSmoothedEaStrategy"/> class.
	/// </summary>
	public ChoSmoothedEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal calculations", "General");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI oscillator", "Indicator");

		_maPeriod = Param(nameof(MaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal MA Period", "Period of smoothing moving average on CCI", "Indicator");
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// CCI oscillator period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Period of the smoothing moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCci = null;
		_prevSignal = null;
		_cciHistory.Clear();

		_cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_cciHistory.Enqueue(cciValue);
		while (_cciHistory.Count > MaPeriod)
			_cciHistory.Dequeue();

		if (!_cci.IsFormed)
			return;

		if (_cciHistory.Count < MaPeriod)
		{
			_prevCci = cciValue;
			return;
		}

		// Calculate signal line (SMA of CCI)
		var sum = 0m;
		var history = _cciHistory.ToArray();
		foreach (var v in history)
			sum += v;
		var signalValue = sum / history.Length;

		if (_prevCci is null || _prevSignal is null)
		{
			_prevCci = cciValue;
			_prevSignal = signalValue;
			return;
		}

		var crossUp = _prevCci <= _prevSignal && cciValue > signalValue;
		var crossDown = _prevCci >= _prevSignal && cciValue < signalValue;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var minSpread = 25m;

		if (crossUp && Math.Abs(cciValue - signalValue) >= minSpread)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		else if (crossDown && Math.Abs(cciValue - signalValue) >= minSpread)
		{
			if (Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
		}

		_prevCci = cciValue;
		_prevSignal = signalValue;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_prevCci = null;
		_prevSignal = null;
		_cci = null;
		_cciHistory.Clear();

		base.OnReseted();
	}
}
