using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TrendSync Pro strategy using pivot levels and higher timeframe confirmation.
/// </summary>
public class TrendSyncProSmcStrategy : Strategy
{
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<Sides?> _tradeDirection;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;

	private decimal _trendValue;
	private bool _trendDirectionUp;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevClose;
	private decimal? _htfPrevClose;
	private decimal? _htfLastClose;
	private decimal _entryPrice;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	/// <summary>
	/// Trend period for pivot detection.
	/// </summary>
	public int TrendPeriod { get => _trendPeriod.Value; set => _trendPeriod.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal SlPercent { get => _slPercent.Value; set => _slPercent.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TpPercent { get => _tpPercent.Value; set => _tpPercent.Value = value; }

	/// <summary>
	/// Trade direction filter.
	/// </summary>
	public Sides? TradeDir { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }

	/// <summary>
	/// Base candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HigherCandleType { get => _higherCandleType.Value; set => _higherCandleType.Value = value; }

	public TrendSyncProSmcStrategy()
	{
		_trendPeriod = Param(nameof(TrendPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Trend Period", "Lookback for pivot", "General")
		.SetCanOptimize(true);
		_slPercent = Param(nameof(SlPercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Percent stop loss", "Risk")
		.SetCanOptimize(true);
		_tpPercent = Param(nameof(TpPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Percent take profit", "Risk")
		.SetCanOptimize(true);
		_tradeDirection = Param(nameof(TradeDir), (Sides?)null)
			.SetDisplay("Trade Direction", "Allowed direction", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Base timeframe", "General");
		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Higher TF", "Trend confirmation timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_trendValue = 0m;
		_trendDirectionUp = false;
		_prevHigh = 0m;
		_prevLow = 0m;
		_prevClose = 0m;
		_htfPrevClose = null;
		_htfLastClose = null;
		_entryPrice = 0m;
		_highest = default!;
		_lowest = default!;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = TrendPeriod };
		_lowest = new Lowest { Length = TrendPeriod };

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(_highest, _lowest, ProcessMain).Start();

		var htfSub = SubscribeCandles(HigherCandleType);
		htfSub.Bind(ProcessHigher).Start();
	}

	private void ProcessHigher(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_htfPrevClose = _htfLastClose;
		_htfLastClose = candle.ClosePrice;
	}

	private void ProcessMain(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_htfPrevClose is null || _htfLastClose is null)
		{
			_prevClose = candle.ClosePrice;
			_prevHigh = highestValue;
			_prevLow = lowestValue;
			return;
		}

		if (highestValue != _prevHigh)
		{
			_trendValue = highestValue;
			_trendDirectionUp = false;
			_prevHigh = highestValue;
		}
		if (lowestValue != _prevLow)
		{
			_trendValue = lowestValue;
			_trendDirectionUp = true;
			_prevLow = lowestValue;
		}

		var htfUp = _htfLastClose > _htfPrevClose;
		var htfDown = _htfLastClose < _htfPrevClose;

		var crossUp = _prevClose <= _trendValue && candle.ClosePrice > _trendValue;
		var crossDown = _prevClose >= _trendValue && candle.ClosePrice < _trendValue;

		var takeLong = TradeDir != Sides.Sell && crossUp && htfUp && _trendDirectionUp;
		var takeShort = TradeDir != Sides.Buy && crossDown && htfDown && !_trendDirectionUp;

		if (Position == 0)
		{
			if (takeLong)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (takeShort)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
		else
		{
			var stopLong = _entryPrice * (1m - SlPercent / 100m);
			var stopShort = _entryPrice * (1m + SlPercent / 100m);
			var tpLong = _entryPrice * (1m + TpPercent / 100m);
			var tpShort = _entryPrice * (1m - TpPercent / 100m);

			if (Position > 0)
			{
				if (candle.LowPrice <= stopLong || candle.HighPrice >= tpLong || crossDown)
					SellMarket(Position);
			}
			else if (Position < 0)
			{
				if (candle.HighPrice >= stopShort || candle.LowPrice <= tpShort || crossUp)
					BuyMarket(-Position);
			}
		}

		_prevClose = candle.ClosePrice;
	}

}