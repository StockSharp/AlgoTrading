using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Uhl adaptive moving average crossover system.
/// Buys when the CTS line crosses above CMA and sells when it crosses below.
/// </summary>
public class UhlMaCrossoverSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private Variance _variance;

	private decimal _cma;
	private decimal _cts;
	private bool _wasCtsAbove;

	/// <summary>
	/// Lookback length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Variance multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="UhlMaCrossoverSystemStrategy"/> class.
	/// </summary>
	public UhlMaCrossoverSystemStrategy()
	{
		_length = Param(nameof(Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback length", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);

		_multiplier = Param(nameof(Multiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Variance multiplier", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_cma = 0m;
		_cts = 0m;
		_wasCtsAbove = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage { Length = Length };
		_variance = new Variance { Length = Length };

		_cma = 0m;
		_cts = 0m;
		_wasCtsAbove = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, _variance, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal varValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed || !_variance.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var prevCma = _cma == 0m ? candle.ClosePrice : _cma;
		var prevCts = _cts == 0m ? candle.ClosePrice : _cts;

		var secma = (smaValue - prevCma) * (smaValue - prevCma);
		var sects = (candle.ClosePrice - prevCts) * (candle.ClosePrice - prevCts);

		var ka = varValue < secma ? 1m - varValue / secma : 0m;
		var kb = varValue < sects ? 1m - varValue / sects : 0m;

		_cma = ka * smaValue + (1m - ka) * prevCma;
		_cts = kb * candle.ClosePrice + (1m - kb) * prevCts;

		var isCtsAbove = _cts > _cma;

		if (_wasCtsAbove != isCtsAbove)
		{
			if (isCtsAbove && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (!isCtsAbove && Position >= 0)
				SellMarket(Volume + Math.Max(Position, 0m));

			_wasCtsAbove = isCtsAbove;
		}
	}
}

