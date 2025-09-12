namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Gradient Trend Filter strategy.
/// Uses triple exponential smoothing to detect trend direction and trades on zero crossovers.
/// </summary>
public class GradientTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<bool> _tradeLong;
	private readonly StrategyParam<bool> _tradeShort;

	private decimal _nf1;
	private decimal _nf2;
	private decimal _nf3;
	private decimal _basePrev1;
	private decimal _basePrev2;
	private decimal _prevDiff;

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Smoothing length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool TradeLong
	{
		get => _tradeLong.Value;
		set => _tradeLong.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool TradeShort
	{
		get => _tradeShort.Value;
		set => _tradeShort.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GradientTrendFilterStrategy"/> class.
	/// </summary>
	public GradientTrendFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation", "General");

		_length = Param(nameof(Length), 25)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Smoothing length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_tradeLong = Param(nameof(TradeLong), true)
			.SetDisplay("Enable long", "Allow long trades", "Trading");

		_tradeShort = Param(nameof(TradeShort), true)
			.SetDisplay("Enable short", "Allow short trades", "Trading");
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

		_nf1 = _nf2 = _nf3 = 0m;
		_basePrev1 = _basePrev2 = 0m;
		_prevDiff = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var alpha = 2m / (Length + 1);

		_nf1 = alpha * candle.ClosePrice + (1m - alpha) * _nf1;
		_nf2 = alpha * _nf1 + (1m - alpha) * _nf2;
		_nf3 = alpha * _nf2 + (1m - alpha) * _nf3;

		var baseValue = _nf3;
		var diff = baseValue - _basePrev2;

		var signalUp = _prevDiff < 0 && diff >= 0;
		var signalDown = _prevDiff > 0 && diff <= 0;

		if (signalUp)
		{
			if (Position < 0)
				ClosePosition();

			if (TradeLong && Position == 0)
				BuyMarket();
		}

		if (signalDown)
		{
			if (Position > 0)
				ClosePosition();

			if (TradeShort && Position == 0)
				SellMarket();
		}

		_basePrev2 = _basePrev1;
		_basePrev1 = baseValue;
		_prevDiff = diff;
	}
}
