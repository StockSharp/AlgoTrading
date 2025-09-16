using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<int> _maRoundTicks;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<bool> _allowCloseLong;
	private readonly StrategyParam<bool> _allowCloseShort;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;

	/// <summary>
	/// Initializes a new instance of <see cref="ExpMaRoundingChannelStrategy"/>.
	/// </summary>
	public ExpMaRoundingChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculation", "General");

		_maLength = Param(nameof(MaLength), 12)
			.SetDisplay("MA Period", "Length of moving average", "Indicator")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 12)
			.SetDisplay("ATR Period", "ATR period for channel width", "Indicator")
			.SetCanOptimize(true);

		_atrFactor = Param(nameof(AtrFactor), 1m)
			.SetDisplay("ATR Factor", "Multiplier for ATR channel", "Indicator")
			.SetCanOptimize(true);

		_maRoundTicks = Param(nameof(MaRoundTicks), 500)
			.SetDisplay("MA Rounding (ticks)", "Rounding step in ticks", "Indicator")
			.SetCanOptimize(true);

		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Buy", "Permission to open long positions", "Trading");

		_allowSell = Param(nameof(AllowSell), true)
			.SetDisplay("Allow Sell", "Permission to open short positions", "Trading");

		_allowCloseLong = Param(nameof(AllowCloseLong), true)
			.SetDisplay("Close Long", "Allow closing long positions", "Trading");

		_allowCloseShort = Param(nameof(AllowCloseShort), true)
			.SetDisplay("Close Short", "Allow closing short positions", "Trading");

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
			.SetDisplay("Stop Loss (ticks)", "Stop loss distance in ticks", "Risk")
			.SetCanOptimize(true);

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
			.SetDisplay("Take Profit (ticks)", "Take profit distance in ticks", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier to form the channel.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Rounding step in ticks for the moving average.
	/// </summary>
	public int MaRoundTicks
	{
		get => _maRoundTicks.Value;
		set => _maRoundTicks.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool AllowCloseLong
	{
		get => _allowCloseLong.Value;
		set => _allowCloseLong.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool AllowCloseShort
	{
		get => _allowCloseShort.Value;
		set => _allowCloseShort.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = MaLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossTicks * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var roundStep = (Security.PriceStep ?? 1m) * MaRoundTicks;
		var roundedMa = roundStep > 0 ? Math.Round(maValue / roundStep) * roundStep : maValue;
		var upper = roundedMa + atrValue * AtrFactor;
		var lower = roundedMa - atrValue * AtrFactor;

		if (_prevClose != 0m)
		{
			if (AllowBuy && _prevClose > _prevUpper)
			{
				if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
				if (AllowCloseShort && Position < 0)
				BuyMarket(Math.Abs(Position));
			}
			else if (AllowSell && _prevClose < _prevLower)
			{
				if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
				if (AllowCloseLong && Position > 0)
				SellMarket(Position);
			}
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
	}
}
