using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades hammer and hanging man candlestick patterns.
/// </summary>
public class ForexHammerAndHangingManStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bodyLengthMultiplier;
	private readonly StrategyParam<decimal> _shadowRatio;
	private readonly StrategyParam<int> _holdPeriods;

	private int _barIndex;
	private int _entryBarIndex = -1;

	/// <summary>
	/// Candle type and timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum candle body length multiplier.
	/// </summary>
	public int BodyLengthMultiplier
	{
		get => _bodyLengthMultiplier.Value;
		set => _bodyLengthMultiplier.Value = value;
	}

	/// <summary>
	/// Lower shadow to body ratio.
	/// </summary>
	public decimal ShadowRatio
	{
		get => _shadowRatio.Value;
		set => _shadowRatio.Value = value;
	}

	/// <summary>
	/// Holding period in bars.
	/// </summary>
	public int HoldPeriods
	{
		get => _holdPeriods.Value;
		set => _holdPeriods.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ForexHammerAndHangingManStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use for pattern detection", "General");

		_bodyLengthMultiplier = Param(nameof(BodyLengthMultiplier), 5)
			.SetDisplay("Minimum Candle Body Length (Multiplier)", "Minimum body length relative to candle height", "General")
			.SetCanOptimize(true);

		_shadowRatio = Param(nameof(ShadowRatio), 1m)
			.SetDisplay("Lower Shadow to Candle Height Ratio", "Lower shadow to body ratio", "General")
			.SetCanOptimize(true);

		_holdPeriods = Param(nameof(HoldPeriods), 26)
			.SetDisplay("Hold Periods (Bars)", "Holding period in bars", "General")
			.SetCanOptimize(true);
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
		_barIndex = 0;
		_entryBarIndex = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		if (Position == 0)
			_entryBarIndex = -1;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bodyLength = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var candleHeight = candle.HighPrice - candle.LowPrice;
		var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);

		var smallBody = bodyLength <= candleHeight / BodyLengthMultiplier;
		var longLowerShadow = lowerShadow >= bodyLength * ShadowRatio;
		var shortUpperShadow = upperShadow <= bodyLength;

		var isHammer = smallBody && longLowerShadow && shortUpperShadow && candle.ClosePrice > candle.OpenPrice;
		var isHangingMan = smallBody && longLowerShadow && shortUpperShadow && candle.ClosePrice < candle.OpenPrice;

		if (Position == 0)
		{
			if (isHammer)
			{
				BuyMarket(Volume);
				_entryBarIndex = _barIndex;
			}
			else if (isHangingMan)
			{
				SellMarket(Volume);
				_entryBarIndex = _barIndex;
			}
		}
		else if (_barIndex - _entryBarIndex >= HoldPeriods)
		{
			ClosePosition();
		}
	}
}
