using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double-smoothed moving average slope strategy.
/// Uses a SMA then a JMA. Trades on slope direction changes.
/// </summary>
public class ColorX2MaDigitNn3MmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;

	private SimpleMovingAverage _sma;
	private JurikMovingAverage _jma;
	private decimal? _prevValue;
	private int _prevSignal;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public ColorX2MaDigitNn3MmrecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "SMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "JMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_sma = null;
		_jma = null;
		_prevValue = null;
		_prevSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = FastLength };
		_jma = new JurikMovingAverage { Length = SlowLength };

		// Chain: SMA -> JMA
		_jma.InnerIndicators.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		var price = candle.ClosePrice;

		// First smoothing: SMA
		var smaResult = _sma.Process(new DecimalIndicatorValue(_sma, price, candle.OpenTime));
		if (!_sma.IsFormed)
			return;

		var smaVal = smaResult.ToDecimal();

		// Second smoothing: JMA
		var jmaResult = _jma.Process(new DecimalIndicatorValue(_jma, smaVal, candle.OpenTime));
		if (!_jma.IsFormed)
		{
			_prevValue = jmaResult.ToDecimal();
			return;
		}

		var current = jmaResult.ToDecimal();

		if (_prevValue == null)
		{
			_prevValue = current;
			return;
		}

		var diff = current - _prevValue.Value;
		var signal = diff > 0 ? 1 : diff < 0 ? -1 : _prevSignal;
		_prevValue = current;

		if (signal == _prevSignal)
			return;

		var oldSignal = _prevSignal;
		_prevSignal = signal;

		if (signal == 1 && oldSignal == -1)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (signal == -1 && oldSignal == 1)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}
	}
}
