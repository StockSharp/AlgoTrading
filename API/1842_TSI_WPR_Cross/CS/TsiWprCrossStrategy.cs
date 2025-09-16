using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Williams %R driven True Strength Index.
/// Buys when TSI crosses above its signal line and sells when it crosses below.
/// </summary>
public class TsiWprCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR _wpr;
	private TrueStrengthIndex _tsi;
	private ExponentialMovingAverage _signal;

	private decimal _prevTsi;
	private decimal _prevSignal;
	private bool _isInitialized;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Short length for TSI.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Long length for TSI.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Signal line length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Candles type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="TsiWprCrossStrategy"/>.
	/// </summary>
	public TsiWprCrossStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators");

		_shortLength = Param(nameof(ShortLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "Short EMA length for TSI", "Indicators");

		_longLength = Param(nameof(LongLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Long Length", "Long EMA length for TSI", "Indicators");

		_signalLength = Param(nameof(SignalLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Length of the signal moving average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
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
		_prevTsi = 0m;
		_prevSignal = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_wpr = new WilliamsR { Length = WprPeriod };
		_tsi = new TrueStrengthIndex { ShortLength = ShortLength, LongLength = LongLength };
		_signal = new ExponentialMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_wpr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			var oscArea = CreateChartArea();
			if (oscArea != null)
			{
				DrawIndicator(oscArea, _tsi);
				DrawIndicator(oscArea, _signal);
			}
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var tsiValue = _tsi.Process(wprValue, candle.ServerTime).ToDecimal();
		var signalValue = _signal.Process(tsiValue, candle.ServerTime).ToDecimal();

		if (!_isInitialized)
		{
			_prevTsi = tsiValue;
			_prevSignal = signalValue;
			_isInitialized = true;
			return;
		}

		var crossedUp = _prevTsi <= _prevSignal && tsiValue > signalValue;
		var crossedDown = _prevTsi >= _prevSignal && tsiValue < signalValue;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (crossedUp)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				if (Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			}
			else if (crossedDown)
			{
				if (Position > 0)
					SellMarket(Position);
				if (Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevTsi = tsiValue;
		_prevSignal = signalValue;
	}
}
