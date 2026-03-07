using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combined RSI, Stochastic, and Moving Average strategy.
/// The MA defines the trend. Entries on RSI+Stochastic oversold/overbought in trend direction.
/// </summary>
public class RsiStochasticMaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stochUpperLevel;
	private readonly StrategyParam<decimal> _stochLowerLevel;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiUpperLevel { get => _rsiUpperLevel.Value; set => _rsiUpperLevel.Value = value; }
	public decimal RsiLowerLevel { get => _rsiLowerLevel.Value; set => _rsiLowerLevel.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public decimal StochUpperLevel { get => _stochUpperLevel.Value; set => _stochUpperLevel.Value = value; }
	public decimal StochLowerLevel { get => _stochLowerLevel.Value; set => _stochLowerLevel.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiStochasticMaStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 3)
			.SetDisplay("RSI Period", "RSI calculation period", "RSI");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 65m)
			.SetDisplay("RSI Upper Level", "RSI overbought level", "RSI");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 35m)
			.SetDisplay("RSI Lower Level", "RSI oversold level", "RSI");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "Moving average period", "Trend");

		_stochUpperLevel = Param(nameof(StochUpperLevel), 60m)
			.SetDisplay("Stochastic Upper", "Stochastic overbought level", "Stochastic");

		_stochLowerLevel = Param(nameof(StochLowerLevel), 40m)
			.SetDisplay("Stochastic Lower", "Stochastic oversold level", "Stochastic");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_stochastic = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ma = new ExponentialMovingAverage { Length = MaPeriod };
		_stochastic = new StochasticOscillator();

		Indicators.Add(_stochastic);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, rsi, (candle, maValue, rsiValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var stochResult = _stochastic.Process(candle);
				if (!stochResult.IsFormed)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var stochVal = (StochasticOscillatorValue)stochResult;
				if (stochVal.K is not decimal k || stochVal.D is not decimal d)
					return;

				var price = candle.ClosePrice;
				var isUpTrend = price > maValue;
				var isDownTrend = price < maValue;

				if (isUpTrend && rsiValue < RsiLowerLevel && k < StochLowerLevel && Position <= 0)
				{
					if (Position < 0) BuyMarket();
					BuyMarket();
				}
				else if (isDownTrend && rsiValue > RsiUpperLevel && k > StochUpperLevel && Position >= 0)
				{
					if (Position > 0) SellMarket();
					SellMarket();
				}
				else if (Position > 0 && (k > StochUpperLevel || rsiValue > RsiUpperLevel))
				{
					SellMarket();
				}
				else if (Position < 0 && (k < StochLowerLevel || rsiValue < RsiLowerLevel))
				{
					BuyMarket();
				}
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
}
