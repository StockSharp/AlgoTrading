using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover with long term trend filter.
/// </summary>
public class ZapTeamProStrategyV6EmaStrategy : Strategy
{
	private readonly StrategyParam<int> _ema21Length;
	private readonly StrategyParam<int> _ema50Length;
	private readonly StrategyParam<int> _ema200Length;
	private readonly StrategyParam<bool> _enableShorts;
	private readonly StrategyParam<DataType> _candleType;

	private readonly ExponentialMovingAverage _ema21 = new();
	private readonly ExponentialMovingAverage _ema50 = new();
	private readonly ExponentialMovingAverage _ema200 = new();

	private decimal? _prev21;
	private decimal? _prev50;

	public int Ema21Length { get => _ema21Length.Value; set => _ema21Length.Value = value; }
	public int Ema50Length { get => _ema50Length.Value; set => _ema50Length.Value = value; }
	public int Ema200Length { get => _ema200Length.Value; set => _ema200Length.Value = value; }
	public bool EnableShorts { get => _enableShorts.Value; set => _enableShorts.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZapTeamProStrategyV6EmaStrategy()
	{
		_ema21Length = Param(nameof(Ema21Length), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA 21", "Fast EMA length", "Indicators")
			.SetCanOptimize(true);

		_ema50Length = Param(nameof(Ema50Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA 50", "Slow EMA length", "Indicators")
			.SetCanOptimize(true);

		_ema200Length = Param(nameof(Ema200Length), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA 200", "Trend EMA length", "Indicators")
			.SetCanOptimize(true);

		_enableShorts = Param(nameof(EnableShorts), false)
			.SetDisplay("Enable Shorts", "Allow short trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema21.Length = Ema21Length;
		_ema50.Length = Ema50Length;
		_ema200.Length = Ema200Length;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema21, _ema50, _ema200, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema21, decimal ema50, decimal ema200)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prev21 is null)
		{
			_prev21 = ema21;
			_prev50 = ema50;
			return;
		}

		var crossUp = _prev21 <= _prev50 && ema21 > ema50;
		var crossDown = _prev21 >= _prev50 && ema21 < ema50;
		var trendLong = candle.ClosePrice > ema200;
		var trendShort = candle.ClosePrice < ema200;

		if (crossUp && trendLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && trendShort && EnableShorts && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prev21 = ema21;
		_prev50 = ema50;
	}
}
