using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic %K threshold-crossing reversal strategy.
/// </summary>
public class StochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<decimal> _overSold;
	private readonly StrategyParam<decimal> _overBought;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _candles = [];
	private decimal? _previousK;

	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public decimal OverSold { get => _overSold.Value; set => _overSold.Value = value; }
	public decimal OverBought { get => _overBought.Value; set => _overBought.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StochasticStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 14).SetGreaterThanZero();
		_overSold = Param(nameof(OverSold), 50m);
		_overBought = Param(nameof(OverBought), 50m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
		_previousK = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Add(candle);
		if (_candles.Count > KPeriod)
			_candles.RemoveAt(0);

		if (_candles.Count < KPeriod)
			return;

		var high = _candles.Max(c => c.HighPrice);
		var low = _candles.Min(c => c.LowPrice);
		var currentK = high == low
			? 50m
			: (candle.ClosePrice - low) / (high - low) * 100m;

		if (_previousK is decimal previous)
		{
			var signal = GetSignal(previous, currentK, OverSold, OverBought);

			if (signal > 0 && Position <= 0m)
				BuyMarket(Volume + Math.Abs(Position));
			else if (signal < 0 && Position >= 0m)
				SellMarket(Volume + Math.Abs(Position));
		}

		_previousK = currentK;
	}

	internal static int GetSignal(decimal previousK, decimal currentK, decimal overSold, decimal overBought)
	{
		if (previousK <= overSold && currentK > overSold)
			return 1;

		if (previousK >= overBought && currentK < overBought)
			return -1;

		return 0;
	}
}
