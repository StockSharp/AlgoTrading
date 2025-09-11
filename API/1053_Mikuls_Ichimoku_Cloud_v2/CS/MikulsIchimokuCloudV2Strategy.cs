using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mikul's Ichimoku Cloud v2 strategy.
/// Breakout strategy with optional moving average filter and trailing stop.
/// </summary>
public class MikulsIchimokuCloudV2Strategy : Strategy
{
	private readonly StrategyParam<TrailSource> _trailSource;
	private readonly StrategyParam<TrailMethod> _trailMethod;
	private readonly StrategyParam<decimal> _trailPercent;
	private readonly StrategyParam<int> _swingLookback;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _addIchiExit;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<MovingAverageType> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouBPeriod;
	private readonly StrategyParam<int> _displacement;
	private readonly StrategyParam<DataType> _candleType;

	private Ichimoku _ichimoku;
	private AverageTrueRange _atr;
	private IIndicator _ma;
	private Highest _swingHigh;
	private Lowest _swingLow;
	private decimal? _trailPrice;
	private decimal? _prevTenkan;
	private decimal? _prevKijun;

	/// <summary>
	/// Source for trailing stop.
	/// </summary>
	public TrailSource TrailSource { get => _trailSource.Value; set => _trailSource.Value = value; }

	/// <summary>
	/// Trailing calculation method.
	/// </summary>
	public TrailMethod TrailMethod { get => _trailMethod.Value; set => _trailMethod.Value = value; }

	/// <summary>
	/// Percent for trailing stop.
	/// </summary>
	public decimal TrailPercent { get => _trailPercent.Value; set => _trailPercent.Value = value; }

	/// <summary>
	/// Swing lookback period.
	/// </summary>
	public int SwingLookback { get => _swingLookback.Value; set => _swingLookback.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Use extra Ichimoku exit.
	/// </summary>
	public bool AddIchiExit { get => _addIchiExit.Value; set => _addIchiExit.Value = value; }

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Use moving average filter.
	/// </summary>
	public bool UseMaFilter { get => _useMaFilter.Value; set => _useMaFilter.Value = value; }

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MovingAverageType MaType { get => _maType.Value; set => _maType.Value = value; }

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod { get => _tenkanPeriod.Value; set => _tenkanPeriod.Value = value; }

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod { get => _kijunPeriod.Value; set => _kijunPeriod.Value = value; }

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouBPeriod { get => _senkouBPeriod.Value; set => _senkouBPeriod.Value = value; }

	/// <summary>
	/// Ichimoku displacement.
	/// </summary>
	public int Displacement { get => _displacement.Value; set => _displacement.Value = value; }

	/// <summary>
	/// Candle type used for strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="MikulsIchimokuCloudV2Strategy"/>.
	/// </summary>
	public MikulsIchimokuCloudV2Strategy()
	{
		_trailSource = Param(nameof(TrailSource), Strategies.TrailSource.LowsHighs)
			.SetDisplay("Trail Source", "Source for trailing stop", "Trailing");

		_trailMethod = Param(nameof(TrailMethod), Strategies.TrailMethod.Atr)
			.SetDisplay("Trail Method", "Trailing calculation method", "Trailing");

		_trailPercent = Param(nameof(TrailPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Percent", "Percent for trailing stop", "Trailing");

		_swingLookback = Param(nameof(SwingLookback), 7)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Swing lookback", "Trailing");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Trailing");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier", "Trailing");

		_addIchiExit = Param(nameof(AddIchiExit), false)
			.SetDisplay("Add Ichimoku Exit", "Use Ichimoku exit with trailing", "Exit");

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Use Take Profit", "Enable take profit", "Exit");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percent", "Exit");

		_useMaFilter = Param(nameof(UseMaFilter), false)
			.SetDisplay("Use MA Filter", "Enable moving average filter", "MA Filter");

		_maType = Param(nameof(MaType), Strategies.MovingAverageType.Ema)
			.SetDisplay("MA Type", "Moving average type", "MA Filter");

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length of moving average", "MA Filter");

		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen length", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen length", "Ichimoku");

		_senkouBPeriod = Param(nameof(SenkouBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou B Period", "Senkou Span B length", "Ichimoku");

		_displacement = Param(nameof(Displacement), 26)
			.SetGreaterThanZero()
			.SetDisplay("Displacement", "Ichimoku displacement", "Ichimoku");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
			SenkouB = { Length = SenkouBPeriod },
			Displacement = Displacement
		};

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_ma = CreateMa(MaType, MaLength);
		_swingHigh = new Highest { Length = SwingLookback };
		_swingLow = new Lowest { Length = SwingLookback };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_atr, _ma, _swingHigh, _swingLow)
			.BindEx(_ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private static IIndicator CreateMa(MovingAverageType type, int length)
	{
		return type switch
		{
			MovingAverageType.Sma => new SMA { Length = length },
			MovingAverageType.Wma => new WMA { Length = length },
			MovingAverageType.Hma => new HMA { Length = length },
			MovingAverageType.Vwma => new VWMA { Length = length },
			MovingAverageType.Vwap => new VWAP { Length = length },
			_ => new EMA { Length = length }
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal ma, decimal swingHigh, decimal swingLow, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ichimokuTyped = (IchimokuValue)ichimokuValue;

		if (ichimokuTyped.Tenkan is not decimal tenkan ||
			ichimokuTyped.Kijun is not decimal kijun ||
			ichimokuTyped.SenkouA is not decimal senkouA ||
			ichimokuTyped.SenkouB is not decimal senkouB)
			return;

		var upperCloud = Math.Max(senkouA, senkouB);
		var lowerCloud = Math.Min(senkouA, senkouB);

		var crossUp = _prevTenkan is decimal prevTenkan && _prevKijun is decimal prevKijun && prevTenkan <= prevKijun && tenkan > kijun;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		var maPass = !UseMaFilter || close > ma;

		var pamp = close > upperCloud && close > lowerCloud && close > open && senkouA > senkouB && tenkan > kijun && maPass;
		var trend = crossUp && close > upperCloud && close > open && tenkan > upperCloud && kijun > upperCloud && maPass;

		if ((trend || pamp) && Position <= 0)
		{
			_trailPrice = null;
			BuyMarket();
		}

		decimal? nextTrail = null;

		if (TrailMethod == TrailMethod.Atr)
		{
			var atrValue = atr * AtrMultiplier;
			nextTrail = TrailSource switch
			{
				TrailSource.Close => close - atrValue,
				TrailSource.Open => open - atrValue,
				_ => swingLow - atrValue
			};
		}
		else if (TrailMethod == TrailMethod.Percent)
		{
			var percentMulti = (100m - TrailPercent) / 100m;
			var basis = TrailSource switch
			{
				TrailSource.Close => close,
				TrailSource.Open => open,
				_ => swingLow
			};
			nextTrail = basis * percentMulti;
		}
		else
		{
			var shortSignal = tenkan < kijun || close < lowerCloud;
			if (shortSignal)
				SellMarket();
		}

		if (AddIchiExit && Position > 0)
		{
			var shortSignal = tenkan < kijun || close < lowerCloud;
			if (shortSignal)
				SellMarket();
		}

		if (Position > 0 && nextTrail.HasValue)
		{
			if (_trailPrice == null || nextTrail > _trailPrice)
				_trailPrice = nextTrail;
		}

		if (Position > 0 && _trailPrice != null)
		{
			var profitTarget = PositionPrice * (1 + TakeProfitPercent / 100m);

			if (close <= _trailPrice || (UseTakeProfit && close >= profitTarget))
				SellMarket();
		}

		if (Position == 0)
			_trailPrice = 0;

		_prevTenkan = tenkan;
		_prevKijun = kijun;
	}

	/// <summary>
	/// Trailing stop source options.
	/// </summary>
	public enum TrailSource
	{
		LowsHighs,
		Close,
		Open
	}

	/// <summary>
	/// Trailing stop methods.
	/// </summary>
	public enum TrailMethod
	{
		Atr,
		Percent,
		IchiExit
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MovingAverageType
	{
		Ema,
		Sma,
		Wma,
		Hma,
		Vwma,
		Vwap
	}
}
