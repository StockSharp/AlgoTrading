using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on volume spikes and CCI ranges.
/// </summary>
public class VolumeEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _cciLevel1;
	private readonly StrategyParam<decimal> _cciLevel2;
	private readonly StrategyParam<decimal> _cciLevel3;
	private readonly StrategyParam<decimal> _cciLevel4;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevVolume;
	private decimal _prevPrevVolume;
	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _longStop;
	private decimal _shortStop;

	/// <summary>
	/// Volume multiplier threshold.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Lower CCI level for long trades.
	/// </summary>
	public decimal CciLevel1
	{
		get => _cciLevel1.Value;
		set => _cciLevel1.Value = value;
	}

	/// <summary>
	/// Upper CCI level for long trades.
	/// </summary>
	public decimal CciLevel2
	{
		get => _cciLevel2.Value;
		set => _cciLevel2.Value = value;
	}

	/// <summary>
	/// Upper CCI level for short trades.
	/// </summary>
	public decimal CciLevel3
	{
		get => _cciLevel3.Value;
		set => _cciLevel3.Value = value;
	}

	/// <summary>
	/// Lower CCI level for short trades.
	/// </summary>
	public decimal CciLevel4
	{
		get => _cciLevel4.Value;
		set => _cciLevel4.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public VolumeEaStrategy()
	{
		_factor = Param(nameof(Factor), 1.55m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "Volume multiplier", "Trading");

		_trailingStop = Param(nameof(TrailingStop), 350m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing distance in steps", "Risk");

		_cciLevel1 = Param(nameof(CciLevel1), 50m)
			.SetDisplay("CCI Level1", "Lower CCI for buys", "Trading");

		_cciLevel2 = Param(nameof(CciLevel2), 190m)
			.SetDisplay("CCI Level2", "Upper CCI for buys", "Trading");

		_cciLevel3 = Param(nameof(CciLevel3), -50m)
			.SetDisplay("CCI Level3", "Upper CCI for sells", "Trading");

		_cciLevel4 = Param(nameof(CciLevel4), -190m)
			.SetDisplay("CCI Level4", "Lower CCI for sells", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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

		var cci = new CommodityChannelIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentVolume = candle.TotalVolume ?? 0m;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			var step = Security?.Step ?? 1m;
			var trailDist = TrailingStop * step;
			var volumeOk = _prevVolume > _prevPrevVolume * Factor;

			if (volumeOk)
			{
				if (_prevClose > _prevOpen && cciValue > CciLevel1 && cciValue < CciLevel2 && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					_longStop = candle.ClosePrice - trailDist;
					_shortStop = 0m;
				}
				else if (_prevClose < _prevOpen && cciValue < CciLevel3 && cciValue > CciLevel4 && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					_shortStop = candle.ClosePrice + trailDist;
					_longStop = 0m;
				}
			}

			if (Position > 0)
			{
				var candidate = candle.ClosePrice - trailDist;
				if (candidate > _longStop)
					_longStop = candidate;

				if (candle.ClosePrice <= _longStop)
				{
					SellMarket(Math.Abs(Position));
					_longStop = 0m;
				}
			}
			else if (Position < 0)
			{
				var candidate = candle.ClosePrice + trailDist;
				if (_shortStop == 0m || candidate < _shortStop)
					_shortStop = candidate;

				if (candle.ClosePrice >= _shortStop)
				{
					BuyMarket(Math.Abs(Position));
					_shortStop = 0m;
				}
			}

			if (candle.OpenTime.Hour == 23 && Position != 0)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				_longStop = 0m;
				_shortStop = 0m;
			}
		}

		_prevPrevVolume = _prevVolume;
		_prevVolume = currentVolume;
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}
}
