namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Contrarian strategy based on Ichimoku cloud and Kijun line.
/// Buys when the Kijun line crosses down into the cloud and sells on the opposite cross.
/// </summary>
public class Exp3XmaIshimokuStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanPeriod;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<DataType> _candleType;

	private Ichimoku _ichimoku;
	private decimal? _prevKijun;
	private decimal? _prevUpper;
	private decimal? _prevLower;

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
	public int SenkouSpanPeriod
	{
		get => _senkouSpanPeriod.Value;
		set => _senkouSpanPeriod.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public Exp3XmaIshimokuStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 3)
			.SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 6)
			.SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku");

		_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 9)
			.SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku");

		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Buy", "Enable long trades", "General");

		_allowSell = Param(nameof(AllowSell), true)
			.SetDisplay("Allow Sell", "Enable short trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ich = (IchimokuValue)ichimokuValue;

		if (ich.Kijun is not decimal kijun ||
			ich.SenkouA is not decimal senkouA ||
			ich.SenkouB is not decimal senkouB)
			return;

		var upper = Math.Max(senkouA, senkouB);
		var lower = Math.Min(senkouA, senkouB);

		if (_prevKijun is null)
		{
			_prevKijun = kijun;
			_prevUpper = upper;
			_prevLower = lower;
			return;
		}

		var crossDown = _prevKijun > _prevUpper && kijun <= upper;
		var crossUp = _prevKijun < _prevLower && kijun >= lower;

		if (crossDown)
		{
			if (AllowBuy)
			{
				var volume = Volume + Math.Max(0m, -Position);
				BuyMarket(volume);
			}
			else if (Position < 0)
			{
				BuyMarket(-Position);
			}
		}
		else if (crossUp)
		{
			if (AllowSell)
			{
				var volume = Volume + Math.Max(0m, Position);
				SellMarket(volume);
			}
			else if (Position > 0)
			{
				SellMarket(Position);
			}
		}

		_prevKijun = kijun;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
