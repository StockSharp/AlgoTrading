using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on CCI crossing specific thresholds.
/// </summary>
public class CciAutomatedStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _tradesDuplicator;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevCci;
	private decimal? _trailPrice;

	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Maximum number of duplicated trades.
	/// </summary>
	public int TradesDuplicator
	{
		get => _tradesDuplicator.Value;
		set => _tradesDuplicator.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
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
	/// Initializes a new instance of the <see cref="CciAutomatedStrategy" /> class.
	/// </summary>
	public CciAutomatedStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 9)
			.SetRange(5, 50)
			.SetDisplay("CCI Period", "CCI indicator length", "Indicators")
			.SetCanOptimize(true);

		_tradesDuplicator = Param(nameof(TradesDuplicator), 3)
			.SetRange(1, 10)
			.SetDisplay("Trades Duplicator", "Maximum number of concurrent trades", "General")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 0.03m)
			.SetRange(0.01m, 1m)
			.SetDisplay("Volume", "Order volume", "General")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetRange(10m, 200m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 200m)
			.SetRange(10m, 500m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk")
			.SetCanOptimize(true);

		_trailingStop = Param(nameof(TrailingStop), 50m)
			.SetRange(10m, 200m)
			.SetDisplay("Trailing Stop", "Trailing stop in price units", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Price),
			stopLoss: new Unit(StopLoss, UnitTypes.Price));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var maxVolume = TradesDuplicator * Volume;

		if (_prevCci is decimal prev)
		{
			if (prev < -90m && cciValue > -80m && Position + Volume <= maxVolume)
			{
				BuyMarket(Volume);
				_trailPrice = candle.ClosePrice - TrailingStop;
			}
			else if (prev > 90m && cciValue < 80m && Position - Volume >= -maxVolume)
			{
				SellMarket(Volume);
				_trailPrice = candle.ClosePrice + TrailingStop;
			}
		}

		if (Position > 0)
		{
			var candidate = candle.ClosePrice - TrailingStop;
			if (_trailPrice is null || candidate > _trailPrice)
				_trailPrice = candidate;
			if (_trailPrice is decimal tp && candle.ClosePrice <= tp)
			{
				SellMarket(Position);
				_trailPrice = null;
			}
		}
		else if (Position < 0)
		{
			var candidate = candle.ClosePrice + TrailingStop;
			if (_trailPrice is null || candidate < _trailPrice)
				_trailPrice = candidate;
			if (_trailPrice is decimal tp && candle.ClosePrice >= tp)
			{
				BuyMarket(Math.Abs(Position));
				_trailPrice = null;
			}
		}

		_prevCci = cciValue;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
			_trailPrice = null;
	}
}

