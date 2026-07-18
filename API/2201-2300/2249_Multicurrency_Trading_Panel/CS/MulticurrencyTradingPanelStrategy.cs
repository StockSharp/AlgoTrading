using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that compares consecutive candle metrics and trades based on majority direction.
/// </summary>
public class MulticurrencyTradingPanelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;

	private ICandleMessage _prev;

	/// <summary>
	/// Candle type used for signal calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MulticurrencyTradingPanelStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (CandleType != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var warmup = new ExponentialMovingAverage { Length = 5 };
		SubscribeCandles(CandleType)
			.Bind(warmup, ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prev = null;
	}

	private void ProcessCandle(ICandleMessage candle, decimal _warmupVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prev == null)
		{
			_prev = candle;
			return;
		}

		var buy = 0;
		var sell = 0;

		void Compare(decimal current, decimal previous)
		{
			if (current > previous)
				buy++;
			else
				sell++;
		}

		Compare(candle.OpenPrice, _prev.OpenPrice);
		Compare(candle.HighPrice, _prev.HighPrice);
		Compare(candle.LowPrice, _prev.LowPrice);
		Compare((candle.HighPrice + candle.LowPrice) / 2m, (_prev.HighPrice + _prev.LowPrice) / 2m);
		Compare(candle.ClosePrice, _prev.ClosePrice);
		Compare((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			(_prev.HighPrice + _prev.LowPrice + _prev.ClosePrice) / 3m);
		Compare((candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			(_prev.HighPrice + _prev.LowPrice + _prev.ClosePrice + _prev.ClosePrice) / 4m);

		if (buy > sell && Position <= 0)
			BuyMarket();
		else if (sell > buy && Position >= 0)
			SellMarket();

		_prev = candle;
	}
}
