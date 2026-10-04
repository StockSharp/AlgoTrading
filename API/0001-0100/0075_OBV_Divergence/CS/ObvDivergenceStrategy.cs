using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OBV (On-Balance Volume) Divergence strategy.
/// Bullish divergence: the low falls below the lows of the previous DivergencePeriod candles while OBV stays above its value
/// at the earlier low. Bearish divergence: the high rises above their highs while OBV stays below its value at the earlier high.
/// A divergence opens a position while flat; it closes when the close crosses back over the moving average or at the percent stop.
/// </summary>
public class ObvDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _divergencePeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal High, decimal Low, decimal Obv)> _history = [];
	private decimal _obv;
	private decimal? _prevClose;

	/// <summary>
	/// Number of previous candles the new extreme is compared with.
	/// </summary>
	public int DivergencePeriod
	{
		get => _divergencePeriod.Value;
		set => _divergencePeriod.Value = value;
	}

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public ObvDivergenceStrategy()
	{
		_divergencePeriod = Param(nameof(DivergencePeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Divergence Period", "Previous candles the new extreme is compared with", "Indicators");

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA exit signal", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_history.Clear();
		_obv = 0;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_history.Clear();
		_obv = 0;
		_prevClose = null;

		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
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

		var close = candle.ClosePrice;

		if (_prevClose is decimal prevClose)
		{
			if (close > prevClose)
				_obv += candle.TotalVolume;
			else if (close < prevClose)
				_obv -= candle.TotalVolume;
		}

		_prevClose = close;

		// Compare this candle with the DivergencePeriod candles before it.
		var previous = _history.ToArray();

		_history.Add((candle.HighPrice, candle.LowPrice, _obv));

		if (_history.Count > DivergencePeriod)
			_history.RemoveAt(0);

		if (previous.Length < DivergencePeriod || !smaValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ma = smaValue.GetValue<decimal>();

		if (Position > 0)
		{
			if (close > ma)
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if (close < ma)
				BuyMarket(-Position);

			return;
		}

		var lowest = previous.MinBy(c => c.Low);
		var highest = previous.MaxBy(c => c.High);

		var bullish = candle.LowPrice < lowest.Low && _obv > lowest.Obv;
		var bearish = candle.HighPrice > highest.High && _obv < highest.Obv;

		if (bullish && !bearish)
			BuyMarket(Volume);
		else if (bearish && !bullish)
			SellMarket(Volume);
	}
}
