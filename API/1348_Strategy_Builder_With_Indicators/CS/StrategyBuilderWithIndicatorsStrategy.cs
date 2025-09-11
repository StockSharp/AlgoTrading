using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA filter with RSI trigger.
/// </summary>
public class StrategyBuilderWithIndicatorsStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<DataType> _candleType;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StrategyBuilderWithIndicatorsStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetDisplay("EMA Length", "EMA period length", "Parameters")
			.SetCanOptimize(true);
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Length", "RSI period length", "Parameters")
			.SetCanOptimize(true);
		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Parameters")
			.SetCanOptimize(true);
		_rsiOversold = Param(nameof(RsiOversold), 30)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Parameters")
			.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new EMA { Length = EmaPeriod };
		var rsi = new RSI { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (Position == 0)
		{
			if (candle.ClosePrice > emaValue && rsiValue < RsiOversold)
			{
				BuyMarket(Volume);
			}
			else if (candle.ClosePrice < emaValue && rsiValue > RsiOverbought)
			{
				SellMarket(Volume);
			}
		}
		else if (Position > 0 && rsiValue > RsiOverbought)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && rsiValue < RsiOversold)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
