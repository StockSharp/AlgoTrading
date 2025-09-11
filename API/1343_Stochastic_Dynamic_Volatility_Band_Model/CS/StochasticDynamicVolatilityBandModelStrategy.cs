using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic-Dynamic Volatility Band Model strategy.
/// Trades crossovers of price with volatility bands and exits after a fixed number of bars.
/// </summary>
public class StochasticDynamicVolatilityBandModelStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _exitBars;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands _bands;
	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private int _barIndex;
	private int _longEntryBar;
	private int _shortEntryBar;

	/// <summary>
	/// Band length for moving average and volatility.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for bands.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Number of bars after which to exit the trade.
	/// </summary>
	public int ExitBars
	{
		get => _exitBars.Value;
		set => _exitBars.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="StochasticDynamicVolatilityBandModelStrategy"/>.
	/// </summary>
	public StochasticDynamicVolatilityBandModelStrategy()
	{
		_length = Param(nameof(Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("Band Length", "Period for bands", "General")
			.SetCanOptimize(true);

		_multiplier = Param(nameof(Multiplier), 1.67m)
			.SetDisplay("Multiplier", "Standard deviation multiplier", "General")
			.SetCanOptimize(true);

		_exitBars = Param(nameof(ExitBars), 7)
			.SetGreaterThanZero()
			.SetDisplay("Exit After Bars", "Bars to hold position", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_bands = default;
		_prevClose = default;
		_prevUpper = default;
		_prevLower = default;
		_barIndex = 0;
		_longEntryBar = -1;
		_shortEntryBar = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bands = new BollingerBands
		{
			Length = Length,
			Width = Multiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bands, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bands);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)indicatorValue;

		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upper;
			_prevLower = lower;
			_barIndex++;
			return;
		}

		var longCondition = _prevClose <= _prevLower && candle.ClosePrice > lower;
		var shortCondition = _prevClose >= _prevUpper && candle.ClosePrice < upper;

		if (longCondition && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
			_longEntryBar = _barIndex;
		}
		else if (shortCondition && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
			_shortEntryBar = _barIndex;
		}

		if (Position > 0 && _longEntryBar >= 0 && _barIndex - _longEntryBar >= ExitBars)
		{
			SellMarket(Position);
			_longEntryBar = -1;
		}
		else if (Position < 0 && _shortEntryBar >= 0 && _barIndex - _shortEntryBar >= ExitBars)
		{
			BuyMarket(-Position);
			_shortEntryBar = -1;
		}

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
		_barIndex++;
	}
}
