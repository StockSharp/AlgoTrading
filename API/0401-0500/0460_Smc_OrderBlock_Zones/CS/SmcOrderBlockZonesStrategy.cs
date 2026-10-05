namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// SMC Order Block Zones Strategy.
/// The swing high of SwingHighLength bars is the premium zone, the swing low of SwingLowLength bars the discount zone and
/// their midpoint the equilibrium. The bullish order block is the lowest low and the bearish order block the highest high of
/// the previous OrderBlockLength bars. A long opens when the close is between the discount zone and the equilibrium, above
/// the SMA, and the candle touched the bullish order block; a short mirrors this between the equilibrium and the premium zone
/// below the SMA after touching the bearish order block. An opposite signal closes or reverses the position and a percent stop
/// limits the loss.
/// </summary>
public class SmcOrderBlockZonesStrategy : Strategy
{
	private readonly StrategyParam<int> _swingHighLength;
	private readonly StrategyParam<int> _swingLowLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _orderBlockLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _swingHigh;
	private Lowest _swingLow;
	private Highest _blockHigh;
	private Lowest _blockLow;
	private decimal? _prevBlockHigh;
	private decimal? _prevBlockLow;

	/// <summary>
	/// Bars of the swing high.
	/// </summary>
	public int SwingHighLength
	{
		get => _swingHighLength.Value;
		set => _swingHighLength.Value = value;
	}

	/// <summary>
	/// Bars of the swing low.
	/// </summary>
	public int SwingLowLength
	{
		get => _swingLowLength.Value;
		set => _swingLowLength.Value = value;
	}

	/// <summary>
	/// SMA period of the trend filter.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Bars searched for order blocks.
	/// </summary>
	public int OrderBlockLength
	{
		get => _orderBlockLength.Value;
		set => _orderBlockLength.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public SmcOrderBlockZonesStrategy()
	{
		_swingHighLength = Param(nameof(SwingHighLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Swing High Length", "Bars of the swing high", "Zones");

		_swingLowLength = Param(nameof(SwingLowLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Swing Low Length", "Bars of the swing low", "Zones");

		_smaLength = Param(nameof(SmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA period of the trend filter", "Indicators");

		_orderBlockLength = Param(nameof(OrderBlockLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Order Block Length", "Bars searched for order blocks", "Zones");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Allow long trades", "Trading");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Allow short trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevBlockHigh = null;
		_prevBlockLow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevBlockHigh = null;
		_prevBlockLow = null;

		var sma = new SimpleMovingAverage { Length = SmaLength };
		_swingHigh = new Highest { Length = SwingHighLength };
		_swingLow = new Lowest { Length = SwingLowLength };
		_blockHigh = new Highest { Length = OrderBlockLength };
		_blockLow = new Lowest { Length = OrderBlockLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, ProcessCandle)
			.Start();

		if (StopLossPercent > 0m)
		{
			StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

			// The stop has to see prices between candles, not only at their close.
			foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
			{
				var quotes = new Subscription(DataType.Level1, Security);
				quotes.MarketData.BuildField = field;
				SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
			}
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var premium = _swingHigh.Process(candle.HighPrice, time, true).ToDecimal();
		var discount = _swingLow.Process(candle.LowPrice, time, true).ToDecimal();

		// Order blocks are taken from the candles before this one so the current candle can touch them.
		var bullishBlock = _prevBlockLow;
		var bearishBlock = _prevBlockHigh;
		var blockHigh = _blockHigh.Process(candle.HighPrice, time, true).ToDecimal();
		var blockLow = _blockLow.Process(candle.LowPrice, time, true).ToDecimal();

		if (_blockHigh.IsFormed && _blockLow.IsFormed)
		{
			_prevBlockHigh = blockHigh;
			_prevBlockLow = blockLow;
		}

		if (!smaValue.IsFormed || !_swingHigh.IsFormed || !_swingLow.IsFormed || bullishBlock is not decimal bullBlock || bearishBlock is not decimal bearBlock)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sma = smaValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var equilibrium = (premium + discount) / 2m;

		var longSignal = close < equilibrium && close > discount && close > sma && candle.LowPrice <= bullBlock;
		var shortSignal = close > equilibrium && close < premium && close < sma && candle.HighPrice >= bearBlock;

		if (longSignal)
		{
			if (AllowLong && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(-Position);
		}
		else if (shortSignal)
		{
			if (AllowShort && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (Position > 0)
				SellMarket(Position);
		}
	}
}
