namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on True Strength Index calculated from Commodity Channel Index.
/// Opens long when TSI crosses above its signal line and short when below.
/// </summary>
public class ExpTsiCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _tsiShortLength;
	private readonly StrategyParam<int> _tsiLongLength;
	private readonly StrategyParam<int> _signalLength;

	private CommodityChannelIndex _cci;
	private TrueStrengthIndex _tsi;
	private ExponentialMovingAverage _signal;

	private decimal _prevTsi;
	private decimal _prevSignal;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Short smoothing length for TSI.
	/// </summary>
	public int TsiShortLength
	{
		get => _tsiShortLength.Value;
		set => _tsiShortLength.Value = value;
	}

	/// <summary>
	/// Long smoothing length for TSI.
	/// </summary>
	public int TsiLongLength
	{
		get => _tsiLongLength.Value;
		set => _tsiLongLength.Value = value;
	}

	/// <summary>
	/// Signal line EMA length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ExpTsiCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cciPeriod = Param(nameof(CciPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI calculation period", "CCI")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_tsiShortLength = Param(nameof(TsiShortLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("TSI Short Length", "Short smoothing length", "TSI")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_tsiLongLength = Param(nameof(TsiLongLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("TSI Long Length", "Long smoothing length", "TSI")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_signalLength = Param(nameof(SignalLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal EMA length", "TSI")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);
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
		_prevTsi = default;
		_prevSignal = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_tsi = new TrueStrengthIndex { ShortLength = TsiShortLength, LongLength = TsiLongLength };
		_signal = new ExponentialMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _tsi);
			DrawIndicator(area, _signal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var tsiValue = _tsi.Process(new DecimalIndicatorValue(_tsi, cciValue, candle.ServerTime));
		if (!tsiValue.IsFinal)
			return;
		var tsi = tsiValue.ToDecimal();

		var signalValue = _signal.Process(new DecimalIndicatorValue(_signal, tsi, candle.ServerTime));
		if (!signalValue.IsFinal)
			return;
		var signal = signalValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevTsi = tsi;
			_prevSignal = signal;
			return;
		}

		var crossUp = _prevTsi <= _prevSignal && tsi > signal;
		var crossDown = _prevTsi >= _prevSignal && tsi < signal;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevTsi = tsi;
		_prevSignal = signal;
	}
}
