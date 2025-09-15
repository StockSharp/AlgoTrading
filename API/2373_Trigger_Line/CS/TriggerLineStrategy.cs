namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trigger Line cross strategy based on weighted trend line and LSMA.
/// </summary>
public class TriggerLineStrategy : Strategy
{
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<int> _lsmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _wma;
	private LinearRegression _lsma;

	private bool _initialized;
	private decimal _prevLine;
	private decimal _prevSignal;

	public TriggerLineStrategy()
	{
		_wmaPeriod = Param(nameof(WmaPeriod), 24)
			.SetDisplay("WT Period", "Period for weighted trend line", "Trigger Line");

		_lsmaPeriod = Param(nameof(LsmaPeriod), 6)
			.SetDisplay("LSMA Period", "Period for least squares moving average", "Trigger Line");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for the strategy", "General");
	}

	public int WmaPeriod
	{
		get => _wmaPeriod.Value;
		set => _wmaPeriod.Value = value;
	}

	public int LsmaPeriod
	{
		get => _lsmaPeriod.Value;
		set => _lsmaPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_initialized = false;
		_prevLine = 0m;
		_prevSignal = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_wma = new WeightedMovingAverage { Length = WmaPeriod };
		_lsma = new LinearRegression { Length = LsmaPeriod };

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
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.State != CandleStates.Finished)
			return;

		var line = _wma.Process(candle.ClosePrice, candle.ServerTime, true).ToDecimal();
		var signal = _lsma.Process(line, candle.ServerTime, true).ToDecimal();

		if (!_initialized)
		{
			_prevLine = line;
			_prevSignal = signal;
			_initialized = true;
			return;
		}

		var crossUp = _prevLine <= _prevSignal && line > signal;
		var crossDown = _prevLine >= _prevSignal && line < signal;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevLine = line;
		_prevSignal = signal;
	}
}
