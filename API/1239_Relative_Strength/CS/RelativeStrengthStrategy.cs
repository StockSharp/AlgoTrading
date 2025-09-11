using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades on relative strength using weighted moving averages.
/// Buys when strength crosses above the upper band and sells when it crosses below the lower band.
/// </summary>
public class RelativeStrengthStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;
	private readonly StrategyParam<int> _smaLength1;
	private readonly StrategyParam<int> _smaLength2;
	private readonly StrategyParam<int> _smaLength3;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaFast;
	private EMA _emaSlow;
	private SMA _sma1;
	private SMA _sma2;
	private SMA _sma3;
	private BollingerBands _bbFast;
	private BollingerBands _bbSlow;
	private BollingerBands _bb1;
	private BollingerBands _bb2;
	private BollingerBands _bb3;

	private decimal _prevStrength;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _initialized;

	private readonly decimal _weightEma = 1m / (decimal)Math.Log(9);
	private readonly decimal _weightSma1 = 1m / 20m;
	private readonly decimal _weightSma2 = 1m / 50m;
	private readonly decimal _weightSma3 = 1m / 200m;
	private readonly decimal _totalWeight;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int EmaFastLength { get => _emaFastLength.Value; set => _emaFastLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int EmaSlowLength { get => _emaSlowLength.Value; set => _emaSlowLength.Value = value; }

	/// <summary>
	/// First SMA length.
	/// </summary>
	public int SmaLength1 { get => _smaLength1.Value; set => _smaLength1.Value = value; }

	/// <summary>
	/// Second SMA length.
	/// </summary>
	public int SmaLength2 { get => _smaLength2.Value; set => _smaLength2.Value = value; }

	/// <summary>
	/// Third SMA length.
	/// </summary>
	public int SmaLength3 { get => _smaLength3.Value; set => _smaLength3.Value = value; }

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal Deviation { get => _deviation.Value; set => _deviation.Value = value; }

	/// <summary>
	/// The type of candles to use.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RelativeStrengthStrategy"/> class.
	/// </summary>
	public RelativeStrengthStrategy()
	{
		_totalWeight = _weightEma + _weightEma + _weightSma1 + _weightSma2 + _weightSma3;

		_emaFastLength = Param(nameof(EmaFastLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA length", "Parameters")
			.SetCanOptimize(true);

		_emaSlowLength = Param(nameof(EmaSlowLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA length", "Parameters")
			.SetCanOptimize(true);

		_smaLength1 = Param(nameof(SmaLength1), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA 1", "First SMA length", "Parameters")
			.SetCanOptimize(true);

		_smaLength2 = Param(nameof(SmaLength2), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA 2", "Second SMA length", "Parameters")
			.SetCanOptimize(true);

		_smaLength3 = Param(nameof(SmaLength3), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA 3", "Third SMA length", "Parameters")
			.SetCanOptimize(true);

		_deviation = Param(nameof(Deviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation", "Bollinger Bands deviation", "Bollinger")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_initialized = false;
		_prevStrength = 0m;
		_prevUpper = 0m;
		_prevLower = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_emaFast = new EMA { Length = EmaFastLength };
		_emaSlow = new EMA { Length = EmaSlowLength };
		_sma1 = new SMA { Length = SmaLength1 };
		_sma2 = new SMA { Length = SmaLength2 };
		_sma3 = new SMA { Length = SmaLength3 };

		_bbFast = new BollingerBands { Length = EmaFastLength, Width = Deviation };
		_bbSlow = new BollingerBands { Length = EmaSlowLength, Width = Deviation };
		_bb1 = new BollingerBands { Length = SmaLength1, Width = Deviation };
		_bb2 = new BollingerBands { Length = SmaLength2, Width = Deviation };
		_bb3 = new BollingerBands { Length = SmaLength3, Width = Deviation };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaSlow, _sma1, _sma2, _sma3, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal sma1, decimal sma2, decimal sma3)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var profitFast = (candle.ClosePrice - emaFast) * 100m / candle.ClosePrice;
		var profitSlow = (candle.ClosePrice - emaSlow) * 100m / candle.ClosePrice;
		var profitSma1 = (candle.ClosePrice - sma1) * 100m / candle.ClosePrice;
		var profitSma2 = (candle.ClosePrice - sma2) * 100m / candle.ClosePrice;
		var profitSma3 = (candle.ClosePrice - sma3) * 100m / candle.ClosePrice;

		var weightedProfit = profitFast * _weightEma + profitSlow * _weightEma + profitSma1 * _weightSma1 + profitSma2 * _weightSma2 + profitSma3 * _weightSma3;
		var strength = weightedProfit / _totalWeight;

		var bbFastVal = (BollingerBandsValue)_bbFast.Process(new DecimalIndicatorValue(_bbFast, strength));
		var bbSlowVal = (BollingerBandsValue)_bbSlow.Process(new DecimalIndicatorValue(_bbSlow, strength));
		var bb1Val = (BollingerBandsValue)_bb1.Process(new DecimalIndicatorValue(_bb1, strength));
		var bb2Val = (BollingerBandsValue)_bb2.Process(new DecimalIndicatorValue(_bb2, strength));
		var bb3Val = (BollingerBandsValue)_bb3.Process(new DecimalIndicatorValue(_bb3, strength));

		if (bbFastVal.UpBand is not decimal upFast || bbFastVal.LowBand is not decimal lowFast ||
			bbSlowVal.UpBand is not decimal upSlow || bbSlowVal.LowBand is not decimal lowSlow ||
			bb1Val.UpBand is not decimal up1 || bb1Val.LowBand is not decimal low1 ||
			bb2Val.UpBand is not decimal up2 || bb2Val.LowBand is not decimal low2 ||
			bb3Val.UpBand is not decimal up3 || bb3Val.LowBand is not decimal low3)
		{
			return;
		}

		var upper = (upFast * _weightEma + upSlow * _weightEma + up1 * _weightSma1 + up2 * _weightSma2 + up3 * _weightSma3) / _totalWeight;
		var lower = (lowFast * _weightEma + lowSlow * _weightEma + low1 * _weightSma1 + low2 * _weightSma2 + low3 * _weightSma3) / _totalWeight;

		if (!_initialized)
		{
			_prevStrength = strength;
			_prevUpper = upper;
			_prevLower = lower;
			_initialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevStrength = strength;
			_prevUpper = upper;
			_prevLower = lower;
			return;
		}

		var crossAbove = _prevStrength <= _prevUpper && strength > upper;
		var crossBelow = _prevStrength >= _prevLower && strength < lower;

		var volume = Volume + Math.Abs(Position);

		if (crossAbove && Position <= 0)
			BuyMarket(volume);
		else if (crossBelow && Position >= 0)
			SellMarket(volume);

		_prevStrength = strength;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
