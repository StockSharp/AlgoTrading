using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DCA strategy using support and resistance levels with RSI and EMA trend filter.
/// Buys at support during uptrend when RSI is oversold.
/// Sells at resistance during downtrend when RSI is overbought.
/// </summary>
public class DcaSupportAndResistanceWithRsiAndTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private EMA _ema;
	private RSI _rsi;

	private decimal _support;
	private decimal _resistance;

	/// <summary>
	/// Number of bars for support/resistance calculation.
	/// </summary>
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought threshold.
	/// </summary>
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	/// <summary>
	/// RSI oversold threshold.
	/// </summary>
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }

	/// <summary>
	/// EMA period for trend filter.
	/// </summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public DcaSupportAndResistanceWithRsiAndTrendFilterStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of bars for support/resistance", "Levels");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period of RSI", "RSI");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "RSI overbought level", "RSI");

		_oversold = Param(nameof(Oversold), 40m)
			.SetDisplay("Oversold", "RSI oversold level", "RSI");

		_emaPeriod = Param(nameof(EmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for trend filter", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_support = 0m;
		_resistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = LookbackPeriod };
		_lowest = new Lowest { Length = LookbackPeriod };
		_ema = new EMA { Length = EmaPeriod };
		_rsi = new RSI { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _rsi, _highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal rsi, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_support = lowest;
		_resistance = highest;

		var price = candle.ClosePrice;
		var uptrend = price >= ema;
		var downtrend = price <= ema;

		if (Position <= 0 && uptrend && price <= _support && rsi <= Oversold)
		{
			BuyMarket();
		}
		else if (Position >= 0 && downtrend && price >= _resistance && rsi >= Overbought)
		{
			SellMarket();
		}
		else if (Position > 0 && (price >= _resistance || rsi >= Overbought))
		{
			SellMarket();
		}
		else if (Position < 0 && (price <= _support || rsi <= Oversold))
		{
			BuyMarket();
		}
	}
}
