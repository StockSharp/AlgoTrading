using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long on Fisher crossing upward over DownLevel, short on crossing downward under UpLevel.
/// Each reversal first closes the actual position volume, then opens the configured Volume.
/// </summary>
public class FisherOrgSignStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<DataType> _candleType;
	private decimal? _previousFisher;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public decimal DownLevel { get => _downLevel.Value; set => _downLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FisherOrgSignStrategy()
	{
		_length = Param(nameof(Length), 7).SetGreaterThanZero()
			.SetDisplay("Fisher Length", "Period for Fisher Transform", "General")
			.SetOptimize(5, 20, 1);
		_upLevel = Param(nameof(UpLevel), 1.5m)
			.SetDisplay("Upper Level", "Sell signal level", "General");
		_downLevel = Param(nameof(DownLevel), -1.5m)
			.SetDisplay("Lower Level", "Buy signal level", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousFisher = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousFisher = null;
		var fisher = new EhlersFisherTransform { Length = Length };
		SubscribeCandles(CandleType).BindEx(fisher, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fisherValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (fisherValue is not IEhlersFisherTransformValue typed || typed.MainLine is not decimal current)
			return;

		var previous = _previousFisher;
		_previousFisher = current;
		if (previous is null || !IsFormedAndOnlineAndAllowTrading())
			return;

		var position = Position;
		if (previous <= DownLevel && current > DownLevel && position <= 0m)
		{
			if (position < 0m)
				BuyMarket(-position);
			BuyMarket(Volume);
		}
		else if (previous >= UpLevel && current < UpLevel && position >= 0m)
		{
			if (position > 0m)
				SellMarket(position);
			SellMarket(Volume);
		}
	}
}
