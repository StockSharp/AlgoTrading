using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average rounding channel strategy.
/// Opens long positions when price closes above the rounded upper channel
/// and opens short positions when price closes below the rounded lower channel.
/// </summary>
public class ExpMaRoundingChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<decimal> _roundStep;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrFactor { get => _atrFactor.Value; set => _atrFactor.Value = value; }
	public decimal RoundStep { get => _roundStep.Value; set => _roundStep.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpMaRoundingChannelStrategy()
	{
		_maLength = Param(nameof(MaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of moving average", "Indicator");

		_atrPeriod = Param(nameof(AtrPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for channel width", "Indicator");

		_atrFactor = Param(nameof(AtrFactor), 1m)
			.SetDisplay("ATR Factor", "Multiplier for ATR channel", "Indicator");

		_roundStep = Param(nameof(RoundStep), 500m)
			.SetDisplay("Round Step", "Rounding step for the moving average", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;

		var ema = new ExponentialMovingAverage { Length = MaLength };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(3, UnitTypes.Percent),
			stopLoss: new Unit(2, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrResult = _atr.Process(candle);
		if (!atrResult.IsFormed)
			return;

		var atrValue = atrResult.ToDecimal();

		// Round the MA value
		var step = RoundStep;
		var roundedMa = step > 0 ? Math.Round(maValue / step) * step : maValue;
		var upper = roundedMa + atrValue * AtrFactor;
		var lower = roundedMa - atrValue * AtrFactor;

		if (_prevClose != 0m)
		{
			// Price broke above upper channel -> buy
			if (_prevClose > _prevUpper && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Price broke below lower channel -> sell
			else if (_prevClose < _prevLower && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
	}
}
