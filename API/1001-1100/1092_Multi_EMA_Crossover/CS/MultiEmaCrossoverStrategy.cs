using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi EMA crossover strategy.
/// Four EMA pairs (EMA1/EMA5, EMA3/EMA10, EMA5/EMA20, EMA10/EMA40) trade independently: each pair adds one long lot when its
/// fast EMA crosses above its slow EMA and removes that lot when the fast EMA falls below the slow EMA. Long only, no stops.
/// </summary>
public class MultiEmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _ema1;
	private readonly StrategyParam<int> _ema3;
	private readonly StrategyParam<int> _ema5;
	private readonly StrategyParam<int> _ema10;
	private readonly StrategyParam<int> _ema20;
	private readonly StrategyParam<int> _ema40;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal?[] _prevFast = new decimal?[4];
	private readonly decimal?[] _prevSlow = new decimal?[4];
	private readonly bool[] _isOpen = new bool[4];

	/// <summary>
	/// Period of EMA1 (fast EMA of the first pair).
	/// </summary>
	public int EMA1
	{
		get => _ema1.Value;
		set => _ema1.Value = value;
	}

	/// <summary>
	/// Period of EMA3 (fast EMA of the second pair).
	/// </summary>
	public int EMA3
	{
		get => _ema3.Value;
		set => _ema3.Value = value;
	}

	/// <summary>
	/// Period of EMA5 (slow EMA of the first pair, fast EMA of the third pair).
	/// </summary>
	public int EMA5
	{
		get => _ema5.Value;
		set => _ema5.Value = value;
	}

	/// <summary>
	/// Period of EMA10 (slow EMA of the second pair, fast EMA of the fourth pair).
	/// </summary>
	public int EMA10
	{
		get => _ema10.Value;
		set => _ema10.Value = value;
	}

	/// <summary>
	/// Period of EMA20 (slow EMA of the third pair).
	/// </summary>
	public int EMA20
	{
		get => _ema20.Value;
		set => _ema20.Value = value;
	}

	/// <summary>
	/// Period of EMA40 (slow EMA of the fourth pair).
	/// </summary>
	public int EMA40
	{
		get => _ema40.Value;
		set => _ema40.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MultiEmaCrossoverStrategy()
	{
		_ema1 = Param(nameof(EMA1), 1)
			.SetGreaterThanZero()
			.SetDisplay("EMA1", "Fast EMA of the 1/5 pair", "Indicators");

		_ema3 = Param(nameof(EMA3), 3)
			.SetGreaterThanZero()
			.SetDisplay("EMA3", "Fast EMA of the 3/10 pair", "Indicators");

		_ema5 = Param(nameof(EMA5), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA5", "Slow EMA of the 1/5 pair and fast EMA of the 5/20 pair", "Indicators");

		_ema10 = Param(nameof(EMA10), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA10", "Slow EMA of the 3/10 pair and fast EMA of the 10/40 pair", "Indicators");

		_ema20 = Param(nameof(EMA20), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA20", "Slow EMA of the 5/20 pair", "Indicators");

		_ema40 = Param(nameof(EMA40), 40)
			.SetGreaterThanZero()
			.SetDisplay("EMA40", "Slow EMA of the 10/40 pair", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema1 = new ExponentialMovingAverage { Length = EMA1 };
		var ema3 = new ExponentialMovingAverage { Length = EMA3 };
		var ema5 = new ExponentialMovingAverage { Length = EMA5 };
		var ema10 = new ExponentialMovingAverage { Length = EMA10 };
		var ema20 = new ExponentialMovingAverage { Length = EMA20 };
		var ema40 = new ExponentialMovingAverage { Length = EMA40 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema1, ema3, ema5, ema10, ema20, ema40, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema1);
			DrawIndicator(area, ema3);
			DrawIndicator(area, ema5);
			DrawIndicator(area, ema10);
			DrawIndicator(area, ema20);
			DrawIndicator(area, ema40);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		Array.Clear(_prevFast);
		Array.Clear(_prevSlow);
		Array.Clear(_isOpen);
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema1, decimal ema3, decimal ema5, decimal ema10, decimal ema20, decimal ema40)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = new[] { ema1, ema3, ema5, ema10 };
		var slow = new[] { ema5, ema10, ema20, ema40 };

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		for (var i = 0; i < 4; i++)
		{
			var prevFast = _prevFast[i];
			var prevSlow = _prevSlow[i];

			_prevFast[i] = fast[i];
			_prevSlow[i] = slow[i];

			if (!canTrade)
				continue;

			// Each pair owns one lot of the combined long position.
			if (_isOpen[i])
			{
				if (fast[i] < slow[i])
				{
					SellMarket(Volume);
					_isOpen[i] = false;
				}
			}
			else if (prevFast is decimal pf && prevSlow is decimal ps && pf <= ps && fast[i] > slow[i])
			{
				BuyMarket(Volume);
				_isOpen[i] = true;
			}
		}
	}
}
