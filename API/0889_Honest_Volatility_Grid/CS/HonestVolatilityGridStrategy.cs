using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility grid strategy based on Keltner Channel levels.
/// </summary>
public class HonestVolatilityGridStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _multiplier;

	private readonly StrategyParam<int> _lEntry1Level;
	private readonly StrategyParam<int> _lEntry2Level;
	private readonly StrategyParam<int> _lEntry3Level;
	private readonly StrategyParam<decimal> _lEntry1Size;
	private readonly StrategyParam<decimal> _lEntry2Size;
	private readonly StrategyParam<decimal> _lEntry3Size;
	private readonly StrategyParam<bool> _useLEntry1;
	private readonly StrategyParam<bool> _useLEntry2;
	private readonly StrategyParam<bool> _useLEntry3;

	private readonly StrategyParam<int> _sEntry1Level;
	private readonly StrategyParam<int> _sEntry2Level;
	private readonly StrategyParam<int> _sEntry3Level;
	private readonly StrategyParam<decimal> _sEntry1Size;
	private readonly StrategyParam<decimal> _sEntry2Size;
	private readonly StrategyParam<decimal> _sEntry3Size;
	private readonly StrategyParam<bool> _useSEntry1;
	private readonly StrategyParam<bool> _useSEntry2;
	private readonly StrategyParam<bool> _useSEntry3;

	private readonly StrategyParam<int> _longRangeSL;
	private readonly StrategyParam<int> _longRangeTP;
	private readonly StrategyParam<int> _shortRangeSL;
	private readonly StrategyParam<int> _shortRangeTP;
	private readonly StrategyParam<int> _rawStopLevel;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;

	private bool _l1;
	private bool _l2;
	private bool _l3;
	private bool _s1;
	private bool _s2;
	private bool _s3;

	/// <summary>
	/// Initializes a new instance of <see cref="HonestVolatilityGridStrategy"/>.
	/// </summary>
	public HonestVolatilityGridStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 200).SetDisplay("EMA Period", "Length of EMA", "Channel");

		_multiplier = Param(nameof(Multiplier), 1m).SetDisplay("Multiplier", "Base Keltner multiplier", "Channel");

		_lEntry1Level =
			Param(nameof(LEntry1Level), -2).SetDisplay("Long Level 1", "Level for first long entry", "Long");
		_lEntry2Level =
			Param(nameof(LEntry2Level), -6).SetDisplay("Long Level 2", "Level for second long entry", "Long");
		_lEntry3Level =
			Param(nameof(LEntry3Level), -8).SetDisplay("Long Level 3", "Level for third long entry", "Long");

		_lEntry1Size =
			Param(nameof(LEntry1Size), 0.01m).SetDisplay("Long Size 1", "Volume fraction for first long", "Long");
		_lEntry2Size =
			Param(nameof(LEntry2Size), 0.01m).SetDisplay("Long Size 2", "Volume fraction for second long", "Long");
		_lEntry3Size =
			Param(nameof(LEntry3Size), 0.01m).SetDisplay("Long Size 3", "Volume fraction for third long", "Long");

		_useLEntry1 = Param(nameof(UseLEntry1), true).SetDisplay("Enable L1", "Enable first long entry", "Long");
		_useLEntry2 = Param(nameof(UseLEntry2), true).SetDisplay("Enable L2", "Enable second long entry", "Long");
		_useLEntry3 = Param(nameof(UseLEntry3), true).SetDisplay("Enable L3", "Enable third long entry", "Long");

		_sEntry1Level =
			Param(nameof(SEntry1Level), 2).SetDisplay("Short Level 1", "Level for first short entry", "Short");
		_sEntry2Level =
			Param(nameof(SEntry2Level), 6).SetDisplay("Short Level 2", "Level for second short entry", "Short");
		_sEntry3Level =
			Param(nameof(SEntry3Level), 8).SetDisplay("Short Level 3", "Level for third short entry", "Short");

		_sEntry1Size =
			Param(nameof(SEntry1Size), 0.01m).SetDisplay("Short Size 1", "Volume fraction for first short", "Short");
		_sEntry2Size =
			Param(nameof(SEntry2Size), 0.01m).SetDisplay("Short Size 2", "Volume fraction for second short", "Short");
		_sEntry3Size =
			Param(nameof(SEntry3Size), 0.01m).SetDisplay("Short Size 3", "Volume fraction for third short", "Short");

		_useSEntry1 = Param(nameof(UseSEntry1), true).SetDisplay("Enable S1", "Enable first short entry", "Short");
		_useSEntry2 = Param(nameof(UseSEntry2), true).SetDisplay("Enable S2", "Enable second short entry", "Short");
		_useSEntry3 = Param(nameof(UseSEntry3), true).SetDisplay("Enable S3", "Enable third short entry", "Short");

		_longRangeSL = Param(nameof(LongRangeSL), -10).SetDisplay("Long SL", "Stop level for long grid", "Long");
		_longRangeTP = Param(nameof(LongRangeTP), 0).SetDisplay("Long TP", "Take profit level for long grid", "Long");
		_shortRangeSL = Param(nameof(ShortRangeSL), 10).SetDisplay("Short SL", "Stop level for short grid", "Short");
		_shortRangeTP =
			Param(nameof(ShortRangeTP), 0).SetDisplay("Short TP", "Take profit level for short grid", "Short");

		_rawStopLevel = Param(nameof(RawStopLevel), 20).SetDisplay("Raw Stop", "Emergency stop multiplier", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <summary>
	/// EMA period for volatility calculation.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Base multiplier for Keltner levels.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Level for first long entry.
	/// </summary>
	public int LEntry1Level
	{
		get => _lEntry1Level.Value;
		set => _lEntry1Level.Value = value;
	}

	/// <summary>
	/// Level for second long entry.
	/// </summary>
	public int LEntry2Level
	{
		get => _lEntry2Level.Value;
		set => _lEntry2Level.Value = value;
	}

	/// <summary>
	/// Level for third long entry.
	/// </summary>
	public int LEntry3Level
	{
		get => _lEntry3Level.Value;
		set => _lEntry3Level.Value = value;
	}

	/// <summary>
	/// Volume fraction for first long entry.
	/// </summary>
	public decimal LEntry1Size
	{
		get => _lEntry1Size.Value;
		set => _lEntry1Size.Value = value;
	}

	/// <summary>
	/// Volume fraction for second long entry.
	/// </summary>
	public decimal LEntry2Size
	{
		get => _lEntry2Size.Value;
		set => _lEntry2Size.Value = value;
	}

	/// <summary>
	/// Volume fraction for third long entry.
	/// </summary>
	public decimal LEntry3Size
	{
		get => _lEntry3Size.Value;
		set => _lEntry3Size.Value = value;
	}

	/// <summary>
	/// Enable first long entry.
	/// </summary>
	public bool UseLEntry1
	{
		get => _useLEntry1.Value;
		set => _useLEntry1.Value = value;
	}

	/// <summary>
	/// Enable second long entry.
	/// </summary>
	public bool UseLEntry2
	{
		get => _useLEntry2.Value;
		set => _useLEntry2.Value = value;
	}

	/// <summary>
	/// Enable third long entry.
	/// </summary>
	public bool UseLEntry3
	{
		get => _useLEntry3.Value;
		set => _useLEntry3.Value = value;
	}

	/// <summary>
	/// Level for first short entry.
	/// </summary>
	public int SEntry1Level
	{
		get => _sEntry1Level.Value;
		set => _sEntry1Level.Value = value;
	}

	/// <summary>
	/// Level for second short entry.
	/// </summary>
	public int SEntry2Level
	{
		get => _sEntry2Level.Value;
		set => _sEntry2Level.Value = value;
	}

	/// <summary>
	/// Level for third short entry.
	/// </summary>
	public int SEntry3Level
	{
		get => _sEntry3Level.Value;
		set => _sEntry3Level.Value = value;
	}

	/// <summary>
	/// Volume fraction for first short entry.
	/// </summary>
	public decimal SEntry1Size
	{
		get => _sEntry1Size.Value;
		set => _sEntry1Size.Value = value;
	}

	/// <summary>
	/// Volume fraction for second short entry.
	/// </summary>
	public decimal SEntry2Size
	{
		get => _sEntry2Size.Value;
		set => _sEntry2Size.Value = value;
	}

	/// <summary>
	/// Volume fraction for third short entry.
	/// </summary>
	public decimal SEntry3Size
	{
		get => _sEntry3Size.Value;
		set => _sEntry3Size.Value = value;
	}

	/// <summary>
	/// Enable first short entry.
	/// </summary>
	public bool UseSEntry1
	{
		get => _useSEntry1.Value;
		set => _useSEntry1.Value = value;
	}

	/// <summary>
	/// Enable second short entry.
	/// </summary>
	public bool UseSEntry2
	{
		get => _useSEntry2.Value;
		set => _useSEntry2.Value = value;
	}

	/// <summary>
	/// Enable third short entry.
	/// </summary>
	public bool UseSEntry3
	{
		get => _useSEntry3.Value;
		set => _useSEntry3.Value = value;
	}

	/// <summary>
	/// Stop level for long grid.
	/// </summary>
	public int LongRangeSL
	{
		get => _longRangeSL.Value;
		set => _longRangeSL.Value = value;
	}

	/// <summary>
	/// Take profit level for long grid.
	/// </summary>
	public int LongRangeTP
	{
		get => _longRangeTP.Value;
		set => _longRangeTP.Value = value;
	}

	/// <summary>
	/// Stop level for short grid.
	/// </summary>
	public int ShortRangeSL
	{
		get => _shortRangeSL.Value;
		set => _shortRangeSL.Value = value;
	}

	/// <summary>
	/// Take profit level for short grid.
	/// </summary>
	public int ShortRangeTP
	{
		get => _shortRangeTP.Value;
		set => _shortRangeTP.Value = value;
	}

	/// <summary>
	/// Emergency stop multiplier.
	/// </summary>
	public int RawStopLevel
	{
		get => _rawStopLevel.Value;
		set => _rawStopLevel.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_l1 = _l2 = _l3 = false;
		_s1 = _s2 = _s3 = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_atr = new AverageTrueRange { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema, _atr, ProcessCandle).Start();

		StartProtection();
	}

	private decimal LevelSelect(int level, decimal ema, decimal atr)
	{
		if (level > 0)
			return ema + atr * Multiplier * level;
		if (level < 0)
			return ema - atr * Multiplier * (-level);
		return ema;
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_atr.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		var l1 = LevelSelect(LEntry1Level, emaValue, atrValue);
		var l2 = LevelSelect(LEntry2Level, emaValue, atrValue);
		var l3 = LevelSelect(LEntry3Level, emaValue, atrValue);
		var s1 = LevelSelect(SEntry1Level, emaValue, atrValue);
		var s2 = LevelSelect(SEntry2Level, emaValue, atrValue);
		var s3 = LevelSelect(SEntry3Level, emaValue, atrValue);

		var longSl = LevelSelect(LongRangeSL, emaValue, atrValue);
		var longTp = LevelSelect(LongRangeTP, emaValue, atrValue);
		var shortSl = LevelSelect(ShortRangeSL, emaValue, atrValue);
		var shortTp = LevelSelect(ShortRangeTP, emaValue, atrValue);

		var rawLower = LevelSelect(-RawStopLevel, emaValue, atrValue);
		var rawUpper = LevelSelect(RawStopLevel, emaValue, atrValue);

		if (price <= rawLower && Position > 0)
		{
			SellMarket(Position);
			_l1 = _l2 = _l3 = false;
		}
		else if (price >= rawUpper && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_s1 = _s2 = _s3 = false;
		}

		if (UseLEntry1 && !_l1 && price <= l1 && Position >= 0)
		{
			var volume = Volume * LEntry1Size + (Position < 0 ? Math.Abs(Position) : 0);
			BuyMarket(volume);
			_l1 = true;
		}

		if (UseLEntry2 && !_l2 && price <= l2 && Position >= 0)
		{
			var volume = Volume * LEntry2Size;
			BuyMarket(volume);
			_l2 = true;
		}

		if (UseLEntry3 && !_l3 && price <= l3 && Position >= 0)
		{
			var volume = Volume * LEntry3Size;
			BuyMarket(volume);
			_l3 = true;
		}

		if (UseSEntry1 && !_s1 && price >= s1 && Position <= 0)
		{
			var volume = Volume * SEntry1Size + (Position > 0 ? Position : 0);
			SellMarket(volume);
			_s1 = true;
		}

		if (UseSEntry2 && !_s2 && price >= s2 && Position <= 0)
		{
			var volume = Volume * SEntry2Size;
			SellMarket(volume);
			_s2 = true;
		}

		if (UseSEntry3 && !_s3 && price >= s3 && Position <= 0)
		{
			var volume = Volume * SEntry3Size;
			SellMarket(volume);
			_s3 = true;
		}

		if (Position > 0)
		{
			if (price >= longTp && LongRangeTP != 0)
			{
				SellMarket(Position);
				_l1 = _l2 = _l3 = false;
			}
			else if (price <= longSl)
			{
				SellMarket(Position);
				_l1 = _l2 = _l3 = false;
			}
		}
		else if (Position < 0)
		{
			if (price <= shortTp && ShortRangeTP != 0)
			{
				BuyMarket(Math.Abs(Position));
				_s1 = _s2 = _s3 = false;
			}
			else if (price >= shortSl)
			{
				BuyMarket(Math.Abs(Position));
				_s1 = _s2 = _s3 = false;
			}
		}
	}
}
