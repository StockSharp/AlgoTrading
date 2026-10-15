using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI threshold-cross reversal strategy.
/// </summary>
public class RsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overSold;
	private readonly StrategyParam<decimal> _overBought;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal? _previousRsi;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal OverSold { get => _overSold.Value; set => _overSold.Value = value; }
	public decimal OverBought { get => _overBought.Value; set => _overBought.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero();
		_overSold = Param(nameof(OverSold), 25m);
		_overBought = Param(nameof(OverBought), 75m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_rsi = null;
		_previousRsi = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		SubscribeCandles(CandleType)
			.Bind(_rsi, (candle, value) =>
			{
				if (candle.State != CandleStates.Finished || !_rsi.IsFormed)
					return;

				if (_previousRsi is decimal previous)
				ApplySignal(GetSignal(previous, value, OverSold, OverBought));

				_previousRsi = value;
			})
			.Start();
	}

	private void ApplySignal(int signal)
	{
		if (signal > 0 && Position <= 0m)
			BuyMarket(Volume + Math.Abs(Position));
		else if (signal < 0 && Position >= 0m)
			SellMarket(Volume + Math.Abs(Position));
	}

	internal static int GetSignal(decimal previous, decimal current, decimal overSold, decimal overBought)
	{
		if (previous <= overSold && current > overSold)
			return 1;

		if (previous >= overBought && current < overBought)
			return -1;

		return 0;
	}
}
