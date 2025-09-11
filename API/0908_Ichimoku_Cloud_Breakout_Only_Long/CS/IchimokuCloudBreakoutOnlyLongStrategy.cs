namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Ichimoku cloud breakout strategy trading only long.
/// Buys when price closes above the cloud and exits when price closes below it.
/// </summary>
public class IchimokuCloudBreakoutOnlyLongStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevCloudTop;
	private decimal _prevCloudBottom;
	private bool _isInitialized;

	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="IchimokuCloudBreakoutOnlyLongStrategy"/>.
	/// </summary>
	public IchimokuCloudBreakoutOnlyLongStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen length", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen length", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Senkou Span B length", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(30, 60, 5);

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
		_prevClose = 0m;
		_prevCloudTop = 0m;
		_prevCloudBottom = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ichimokuValue = (IchimokuValue)value;

		if (ichimokuValue.SenkouA is not decimal senkouA ||
			ichimokuValue.SenkouB is not decimal senkouB)
		{
			return;
		}

		var cloudTop = Math.Max(senkouA, senkouB);
		var cloudBottom = Math.Min(senkouA, senkouB);

		if (!_isInitialized)
		{
			_prevClose = candle.ClosePrice;
			_prevCloudTop = cloudTop;
			_prevCloudBottom = cloudBottom;
			_isInitialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossedAbove = _prevClose <= _prevCloudTop && candle.ClosePrice > cloudTop;
		var crossedBelow = _prevClose >= _prevCloudBottom && candle.ClosePrice < cloudBottom;

		if (crossedAbove && Position <= 0)
			BuyMarket();
		else if (crossedBelow && Position > 0)
			SellMarket(Math.Abs(Position));

		_prevClose = candle.ClosePrice;
		_prevCloudTop = cloudTop;
		_prevCloudBottom = cloudBottom;
	}
}
