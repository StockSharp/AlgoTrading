using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Envelope mean reversion strategy converted from the MetaTrader 4 expert "EnvelopesEA".
/// Opens contrarian trades when price stretches beyond the envelope bands and exits once it returns inside.
/// </summary>
public class EnvelopesEaStrategy : Strategy
{
	private readonly StrategyParam<int> _envelopePeriod;
	private readonly StrategyParam<decimal> _upperDeviationPercent;
	private readonly StrategyParam<decimal> _lowerDeviationPercent;
	private readonly StrategyParam<decimal> _entryOffsetPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private bool _isClosing;
	private bool _longActive;
	private bool _shortActive;


	/// <summary>
	/// Length of the exponential moving average used as the envelope basis.
	/// </summary>
	public int EnvelopePeriod
	{
		get => _envelopePeriod.Value;
		set => _envelopePeriod.Value = value;
	}

	/// <summary>
	/// Percentage width of the upper envelope relative to the moving average.
	/// </summary>
	public decimal UpperDeviationPercent
	{
		get => _upperDeviationPercent.Value;
		set => _upperDeviationPercent.Value = value;
	}

	/// <summary>
	/// Percentage width of the lower envelope relative to the moving average.
	/// </summary>
	public decimal LowerDeviationPercent
	{
		get => _lowerDeviationPercent.Value;
		set => _lowerDeviationPercent.Value = value;
	}

	/// <summary>
	/// Additional entry offset measured in price steps.
	/// </summary>
	public decimal EntryOffsetPoints
	{
		get => _entryOffsetPoints.Value;
		set => _entryOffsetPoints.Value = value;
	}

	/// <summary>
	/// Candle series processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EnvelopesEaStrategy"/> class.
	/// </summary>
	public EnvelopesEaStrategy()
	{

		_envelopePeriod = Param(nameof(EnvelopePeriod), 50)
			.SetDisplay("Envelope Period", "EMA length used for the envelope", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 5);

		_upperDeviationPercent = Param(nameof(UpperDeviationPercent), 0.5m)
			.SetDisplay("Upper Deviation %", "Percent width of the upper band", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);

		_lowerDeviationPercent = Param(nameof(LowerDeviationPercent), 0.5m)
			.SetDisplay("Lower Deviation %", "Percent width of the lower band", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);

		_entryOffsetPoints = Param(nameof(EntryOffsetPoints), 100m)
			.SetDisplay("Entry Offset (points)", "Extra distance from the band before entering", "Trading")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for analysis", "General");
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

		_ema = null;
		_isClosing = false;
		_longActive = false;
		_shortActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage
		{
			Length = EnvelopePeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_ema?.IsFormed != true)
			return;

		if (Position == 0)
		{
			if (_isClosing)
				_isClosing = false;

			_longActive = false;
			_shortActive = false;
		}

		var priceStep = Security.PriceStep ?? 0.0001m;
		if (priceStep <= 0m)
			priceStep = 0.0001m;

		var deviationUpper = UpperDeviationPercent / 100m;
		var deviationLower = LowerDeviationPercent / 100m;
		var offset = EntryOffsetPoints * priceStep;

		var upper = emaValue * (1m + deviationUpper);
		var lower = emaValue * (1m - deviationLower);
		var entryUpper = upper + offset;
		var entryLower = lower - offset;
		var close = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isClosing && Position == 0)
		{
			if (close <= entryLower && Volume > 0m)
			{
				BuyMarket(Volume);
				_longActive = true;
				return;
			}

			if (close >= entryUpper && Volume > 0m)
			{
				SellMarket(Volume);
				_shortActive = true;
				return;
			}
		}

		if (_isClosing || Position == 0)
			return;

		if (Position > 0)
		{
			_longActive = true;

			if (close >= upper)
			{
				_isClosing = true;
				ClosePosition();
			}
		}
		else if (Position < 0)
		{
			_shortActive = true;

			if (close <= lower)
			{
				_isClosing = true;
				ClosePosition();
			}
		}
	}
}
