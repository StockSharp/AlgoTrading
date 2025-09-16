using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI filter combined with engulfing candle pattern taken from the SSEATwRSI expert advisor.
/// </summary>
public class SuperSimpleRsiEngulfingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _profitGoal;
	private readonly StrategyParam<decimal> _maxLoss;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<CandlePrice> _rsiPrice;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;

	private decimal? _prevOpen;
	private decimal? _prevClose;
	private decimal? _prevPrevOpen;
	private decimal? _prevPrevClose;

	/// <summary>
	/// Order volume in contracts.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Currency profit target that forces a flatten.
	/// </summary>
	public decimal ProfitGoal
	{
		get => _profitGoal.Value;
		set => _profitGoal.Value = value;
	}

	/// <summary>
	/// Maximum allowed currency loss before closing positions.
	/// </summary>
	public decimal MaxLoss
	{
		get => _maxLoss.Value;
		set => _maxLoss.Value = value;
	}

	/// <summary>
	/// RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Price source used by the RSI indicator.
	/// </summary>
	public CandlePrice RsiPrice
	{
		get => _rsiPrice.Value;
		set => _rsiPrice.Value = value;
	}

	/// <summary>
	/// RSI level considered overbought.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// RSI level considered oversold.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SuperSimpleRsiEngulfingStrategy"/>.
	/// </summary>
	public SuperSimpleRsiEngulfingStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_profitGoal = Param(nameof(ProfitGoal), 190m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Goal", "Currency profit target to flatten", "Risk");

		_maxLoss = Param(nameof(MaxLoss), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Max Loss", "Maximum currency drawdown before flattening", "Risk");

		_rsiPeriod = Param(nameof(RsiPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI averaging period", "Indicators");

		_rsiPrice = Param(nameof(RsiPrice), CandlePrice.High)
			.SetDisplay("RSI Price", "Price source for RSI", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 88m)
			.SetDisplay("Overbought Level", "RSI threshold for bullish reversals", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), 37m)
			.SetDisplay("Oversold Level", "RSI threshold for bearish reversals", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series to process", "General");
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

		_rsi = null!;
		_prevOpen = null;
		_prevClose = null;
		_prevPrevOpen = null;
		_prevPrevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenNew(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = GetPrice(candle, RsiPrice);
		var rsiValue = _rsi.Process(price, candle.OpenTime, true).ToDecimal();

		if (!_rsi.IsFormed)
		{
			UpdateHistory(candle);
			return;
		}

		var hasPattern = _prevOpen is decimal prevOpen &&
			_prevClose is decimal prevClose &&
			_prevPrevOpen is decimal prevPrevOpen &&
			_prevPrevClose is decimal prevPrevClose;

		if (hasPattern && IsFormedAndOnlineAndAllowTrading())
		{
			// Detect the two-candle engulfing pattern from the previous bars.
			var bullishEngulfing = prevPrevOpen > prevPrevClose &&
				prevOpen < prevClose &&
				prevPrevOpen < prevClose;

			var bearishEngulfing = prevPrevOpen < prevPrevClose &&
				prevOpen > prevClose &&
				prevPrevOpen > prevClose;

			// Only enter long if RSI indicates overbought exhaustion and pattern flips to bullish.
			var longSignal = rsiValue > OverboughtLevel && bullishEngulfing && Position <= 0m;

			// Only enter short if RSI indicates oversold exhaustion and pattern flips to bearish.
			var shortSignal = rsiValue < OversoldLevel && bearishEngulfing && Position >= 0m;

			if (longSignal)
			{
				var volume = Volume + (Position < 0m ? Math.Abs(Position) : 0m);
				BuyMarket(volume);
			}
			else if (shortSignal)
			{
				var volume = Volume + (Position > 0m ? Math.Abs(Position) : 0m);
				SellMarket(volume);
			}
		}

		if (Position != 0m)
		{
			// Flatten the position once floating PnL reaches the configured thresholds.
			var totalPnL = PnL;

			if (totalPnL >= ProfitGoal || totalPnL <= -MaxLoss)
				ClosePosition();
		}

		UpdateHistory(candle);
	}

	private void ClosePosition()
	{
		if (Position > 0m)
			SellMarket(Math.Abs(Position));
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		// Shift the last two completed candles so pattern checks use historical data only.
		_prevPrevOpen = _prevOpen;
		_prevPrevClose = _prevClose;
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}

	private static decimal GetPrice(ICandleMessage candle, CandlePrice price)
	{
		// Support different RSI inputs without duplicating indicator logic.
		return price switch
		{
			CandlePrice.Open => candle.OpenPrice,
			CandlePrice.High => candle.HighPrice,
			CandlePrice.Low => candle.LowPrice,
			CandlePrice.Close => candle.ClosePrice,
			CandlePrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}
}
