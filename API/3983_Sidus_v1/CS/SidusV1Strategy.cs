using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Sidus v1 expert advisor using EMA and RSI filters.
/// Buys when the fast EMA is sufficiently below the slow EMA and RSI is oversold.
/// Sells when the fast EMA is sufficiently above the slow EMA and RSI is overbought.
/// </summary>
public class SidusV1Strategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _fastEma2Length;
	private readonly StrategyParam<int> _slowEma2Length;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiPeriod2;
	private readonly StrategyParam<decimal> _buyDifferenceThreshold;
	private readonly StrategyParam<decimal> _buyRsiThreshold;
	private readonly StrategyParam<decimal> _sellDifferenceThreshold;
	private readonly StrategyParam<decimal> _sellRsiThreshold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Length of the fast EMA for buy signal calculation.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow EMA for buy signal calculation.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the fast EMA for sell signal calculation.
	/// </summary>
	public int FastEma2Length
	{
		get => _fastEma2Length.Value;
		set => _fastEma2Length.Value = value;
	}

	/// <summary>
	/// Length of the slow EMA for sell signal calculation.
	/// </summary>
	public int SlowEma2Length
	{
		get => _slowEma2Length.Value;
		set => _slowEma2Length.Value = value;
	}

	/// <summary>
	/// RSI period used for buy signals.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI period used for sell signals.
	/// </summary>
	public int RsiPeriod2
	{
		get => _rsiPeriod2.Value;
		set => _rsiPeriod2.Value = value;
	}

	/// <summary>
	/// Threshold for EMA difference to allow buy orders (negative means fast below slow).
	/// </summary>
	public decimal BuyDifferenceThreshold
	{
		get => _buyDifferenceThreshold.Value;
		set => _buyDifferenceThreshold.Value = value;
	}

	/// <summary>
	/// RSI threshold to confirm oversold conditions.
	/// </summary>
	public decimal BuyRsiThreshold
	{
		get => _buyRsiThreshold.Value;
		set => _buyRsiThreshold.Value = value;
	}

	/// <summary>
	/// Threshold for EMA difference to allow sell orders (positive means fast above slow).
	/// </summary>
	public decimal SellDifferenceThreshold
	{
		get => _sellDifferenceThreshold.Value;
		set => _sellDifferenceThreshold.Value = value;
	}

	/// <summary>
	/// RSI threshold to confirm overbought conditions.
	/// </summary>
	public decimal SellRsiThreshold
	{
		get => _sellRsiThreshold.Value;
		set => _sellRsiThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss distance in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SidusV1Strategy"/> class.
	/// </summary>
	public SidusV1Strategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 23)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Length of the fast EMA for buy signals", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 62)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Length of the slow EMA for buy signals", "Indicators");

		_fastEma2Length = Param(nameof(FastEma2Length), 18)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length (Sell)", "Length of the fast EMA for sell signals", "Indicators");

		_slowEma2Length = Param(nameof(SlowEma2Length), 54)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length (Sell)", "Length of the slow EMA for sell signals", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 67)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period used for buy signals", "Indicators");

		_rsiPeriod2 = Param(nameof(RsiPeriod2), 97)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period (Sell)", "RSI period used for sell signals", "Indicators");

		_buyDifferenceThreshold = Param(nameof(BuyDifferenceThreshold), -100m)
			.SetDisplay("Buy EMA Threshold", "Maximum fast-slow EMA difference to allow buy", "Trading Rules");

		_buyRsiThreshold = Param(nameof(BuyRsiThreshold), 45m)
			.SetDisplay("Buy RSI Threshold", "Maximum RSI level to allow buy", "Trading Rules");

		_sellDifferenceThreshold = Param(nameof(SellDifferenceThreshold), 100m)
			.SetDisplay("Sell EMA Threshold", "Minimum fast-slow EMA difference to allow sell", "Trading Rules");

		_sellRsiThreshold = Param(nameof(SellRsiThreshold), 55m)
			.SetDisplay("Sell RSI Threshold", "Minimum RSI level to allow sell", "Trading Rules");

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance in absolute price units", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance in absolute price units", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		var fastEma = new EMA { Length = FastEmaLength };
		var slowEma = new EMA { Length = SlowEmaLength };
		var fastEma2 = new EMA { Length = FastEma2Length };
		var slowEma2 = new EMA { Length = SlowEma2Length };
		var rsi = new RSI { Length = RsiPeriod };
		var rsi2 = new RSI { Length = RsiPeriod2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, fastEma2, slowEma2, rsi, rsi2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}

		// Use StartProtection for SL/TP
		var tp = TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Absolute) : null;
		var sl = StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Absolute) : null;
		StartProtection(tp, sl);

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal fastEmaValue,
		decimal slowEmaValue,
		decimal fastEma2Value,
		decimal slowEma2Value,
		decimal rsiValue,
		decimal rsi2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var diffBuy = fastEmaValue - slowEmaValue;
		var diffSell = fastEma2Value - slowEma2Value;

		// Buy when fast EMA is sufficiently below slow EMA and RSI is oversold
		if (diffBuy < BuyDifferenceThreshold && rsiValue < BuyRsiThreshold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		// Sell when fast EMA is sufficiently above slow EMA and RSI is overbought
		else if (diffSell > SellDifferenceThreshold && rsi2Value > SellRsiThreshold && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket(Volume);
		}
	}
}
