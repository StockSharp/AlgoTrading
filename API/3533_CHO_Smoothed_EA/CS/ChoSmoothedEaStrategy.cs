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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal calculations", "General");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI oscillator", "Indicator");

		_maPeriod = Param(nameof(MaPeriod), 5)
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
		foreach (var v in _cciHistory)
			sum += v;
		var signalValue = sum / MaPeriod;

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

		if (crossUp)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossDown)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevCci = cciValue;
		_prevSignal = signalValue;
	}
}
