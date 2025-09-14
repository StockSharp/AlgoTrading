using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy ported from Robust_EA_Template.
/// Uses CCI and RSI indicators to generate trading signals with fixed take profit and stop loss.
/// </summary>
public class RobustEaTemplateStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci;
	private RelativeStrengthIndex _rsi;

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public new decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RobustEaTemplateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "Commodity Channel Index period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Relative Strength Index period", "Indicators");

		_takeProfit = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Take profit in pips", "Risk");

		_stopLoss = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss (pips)", "Stop loss in pips", "Risk");

		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Trade volume", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_cci, _rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitPips * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossPips * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longSignal = ((cciValue > -200 && cciValue <= -150) || (cciValue > -100 && cciValue <= -50)) &&
			(rsiValue > 0 && rsiValue <= 25);

		var shortSignal = (cciValue > 50 && cciValue <= 150) &&
			(rsiValue > 80 && rsiValue <= 100);

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
