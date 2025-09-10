using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades CME Bitcoin futures based on the spread to Bitfinex spot using Bollinger Bands.
/// </summary>
public class BitcoinCmeSpotSpreadStrategy : Strategy
{
	private readonly StrategyParam<Security> _spot;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbStdDev;
	private readonly StrategyParam<decimal> _takeProfitStep1;
	private readonly StrategyParam<decimal> _takeProfitStep2;
	private readonly StrategyParam<decimal> _takeProfitStep3;
	private readonly StrategyParam<decimal> _takeProfitStep4;
	private readonly StrategyParam<decimal> _tpPercent1;
	private readonly StrategyParam<decimal> _tpPercent2;
	private readonly StrategyParam<decimal> _tpPercent3;
	private readonly StrategyParam<decimal> _tpPercent4;
	private readonly StrategyParam<int> _holdBars;
	private readonly StrategyParam<bool> _useRisk;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _cmePrice;
	private decimal? _spotPrice;
	private BollingerBands _bb = null!;
	private decimal _prevSpread;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _hasPrev;
	private decimal _entryPrice;
	private int _barsSinceEntry;
	private bool _tp1Done;
	private bool _tp2Done;
	private bool _tp3Done;
	private bool _tp4Done;

	/// <summary>
	/// CME futures security.
	/// </summary>
	public Security Cme
	{
		get => Security;
		set => Security = value;
	}

	/// <summary>
	/// Spot security.
	/// </summary>
	public Security Spot
	{
		get => _spot.Value;
		set => _spot.Value = value;
	}

	/// <summary>
	/// Bollinger bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger bands width (standard deviations).
	/// </summary>
	public decimal BollingerStdDev
	{
		get => _bbStdDev.Value;
		set => _bbStdDev.Value = value;
	}

	/// <summary>
	/// Take profit level 1 (percentage).
	/// </summary>
	public decimal TakeProfitStep1
	{
		get => _takeProfitStep1.Value;
		set => _takeProfitStep1.Value = value;
	}

	/// <summary>
	/// Take profit level 2 (percentage).
	/// </summary>
	public decimal TakeProfitStep2
	{
		get => _takeProfitStep2.Value;
		set => _takeProfitStep2.Value = value;
	}

	/// <summary>
	/// Take profit level 3 (percentage).
	/// </summary>
	public decimal TakeProfitStep3
	{
		get => _takeProfitStep3.Value;
		set => _takeProfitStep3.Value = value;
	}

	/// <summary>
	/// Take profit level 4 (percentage).
	/// </summary>
	public decimal TakeProfitStep4
	{
		get => _takeProfitStep4.Value;
		set => _takeProfitStep4.Value = value;
	}

	/// <summary>
	/// Exit percentage at level 1.
	/// </summary>
	public decimal TakeProfitPercent1
	{
		get => _tpPercent1.Value;
		set => _tpPercent1.Value = value;
	}

	/// <summary>
	/// Exit percentage at level 2.
	/// </summary>
	public decimal TakeProfitPercent2
	{
		get => _tpPercent2.Value;
		set => _tpPercent2.Value = value;
	}

	/// <summary>
	/// Exit percentage at level 3.
	/// </summary>
	public decimal TakeProfitPercent3
	{
		get => _tpPercent3.Value;
		set => _tpPercent3.Value = value;
	}

	/// <summary>
	/// Exit percentage at level 4.
	/// </summary>
	public decimal TakeProfitPercent4
	{
		get => _tpPercent4.Value;
		set => _tpPercent4.Value = value;
	}

	/// <summary>
	/// Exit after given number of bars.
	/// </summary>
	public int HoldBars
	{
		get => _holdBars.Value;
		set => _holdBars.Value = value;
	}

	/// <summary>
	/// Enable take profit exits.
	/// </summary>
	public bool UseRiskManagement
	{
		get => _useRisk.Value;
		set => _useRisk.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BitcoinCmeSpotSpreadStrategy()
	{
		_spot = Param<Security>(nameof(Spot), null)
			.SetDisplay("Spot", "Spot security", "Universe");

		_bbPeriod = Param(nameof(BollingerPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger bands period", "Bollinger");

		_bbStdDev = Param(nameof(BollingerStdDev), 2.618m)
			.SetGreaterThanZero()
			.SetDisplay("BB Std Dev", "Bollinger standard deviation", "Bollinger");

		_takeProfitStep1 = Param(nameof(TakeProfitStep1), 0.03m)
			.SetDisplay("TP Level 1", "First take profit level", "Risk Management");

		_takeProfitStep2 = Param(nameof(TakeProfitStep2), 0.08m)
			.SetDisplay("TP Level 2", "Second take profit level", "Risk Management");

		_takeProfitStep3 = Param(nameof(TakeProfitStep3), 0.14m)
			.SetDisplay("TP Level 3", "Third take profit level", "Risk Management");

		_takeProfitStep4 = Param(nameof(TakeProfitStep4), 0.21m)
			.SetDisplay("TP Level 4", "Fourth take profit level", "Risk Management");

		_tpPercent1 = Param(nameof(TakeProfitPercent1), 0.25m)
			.SetDisplay("TP % Level 1", "Exit percent at level 1", "Risk Management");

		_tpPercent2 = Param(nameof(TakeProfitPercent2), 0.20m)
			.SetDisplay("TP % Level 2", "Exit percent at level 2", "Risk Management");

		_tpPercent3 = Param(nameof(TakeProfitPercent3), 0.15m)
			.SetDisplay("TP % Level 3", "Exit percent at level 3", "Risk Management");

		_tpPercent4 = Param(nameof(TakeProfitPercent4), 0.10m)
			.SetDisplay("TP % Level 4", "Exit percent at level 4", "Risk Management");

		_holdBars = Param(nameof(HoldBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Hold Bars", "Exit after bars", "Risk Management");

		_useRisk = Param(nameof(UseRiskManagement), true)
			.SetDisplay("Use Risk", "Enable take profits", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		if (Cme == null || Spot == null)
			throw new InvalidOperationException("Cme and Spot must be set.");

		return [(Cme, CandleType), (Spot, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_cmePrice = null;
		_spotPrice = null;
		_prevSpread = 0m;
		_prevUpper = 0m;
		_prevLower = 0m;
		_hasPrev = false;
		_entryPrice = 0m;
		_barsSinceEntry = 0;
		_tp1Done = _tp2Done = _tp3Done = _tp4Done = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		if (Cme == null || Spot == null)
			throw new InvalidOperationException("Cme and Spot must be set.");

		base.OnStarted(time);
		StartProtection();

		_bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerStdDev
		};

		SubscribeCandles(CandleType, true, Cme).Bind(c => ProcessCandle(c, Cme)).Start();
		SubscribeCandles(CandleType, true, Spot).Bind(c => ProcessCandle(c, Spot)).Start();
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (security == Cme)
			_cmePrice = candle.ClosePrice;
		else
			_spotPrice = candle.ClosePrice;

		if (_cmePrice is null || _spotPrice is null)
			return;

		if (security != Cme)
			return;

		var spread = _cmePrice.Value - _spotPrice.Value;
		var bbVal = (BollingerBandsValue)_bb.Process(spread);

		if (bbVal.UpBand is not decimal up || bbVal.LowBand is not decimal low)
			return;

		if (_hasPrev)
		{
			if (spread < low && _prevSpread >= _prevLower && Position <= 0)
				EnterLong(candle.ClosePrice);
			else if (spread > up && _prevSpread <= _prevUpper && Position >= 0)
				EnterShort(candle.ClosePrice);
		}

		if (Position != 0)
		{
			_barsSinceEntry++;

			if (UseRiskManagement)
				CheckTakeProfits(candle.ClosePrice);

			if (_barsSinceEntry >= HoldBars)
				ClosePosition();
		}

		_prevSpread = spread;
		_prevUpper = up;
		_prevLower = low;
		_hasPrev = true;
	}

	private void EnterLong(decimal price)
	{
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		_entryPrice = price;
		ResetTakeProfit();
	}

	private void EnterShort(decimal price)
	{
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		_entryPrice = price;
		ResetTakeProfit();
	}

	private void ResetTakeProfit()
	{
		_barsSinceEntry = 0;
		_tp1Done = _tp2Done = _tp3Done = _tp4Done = false;
	}

	private void CheckTakeProfits(decimal price)
	{
		if (Position > 0)
		{
			var profit = (price - _entryPrice) / _entryPrice;
			if (!_tp1Done && profit >= TakeProfitStep1)
				ExitPartial(TakeProfitPercent1, Sides.Sell, ref _tp1Done);
			if (!_tp2Done && profit >= TakeProfitStep2)
				ExitPartial(TakeProfitPercent2, Sides.Sell, ref _tp2Done);
			if (!_tp3Done && profit >= TakeProfitStep3)
				ExitPartial(TakeProfitPercent3, Sides.Sell, ref _tp3Done);
			if (!_tp4Done && profit >= TakeProfitStep4)
				ExitPartial(TakeProfitPercent4, Sides.Sell, ref _tp4Done);
		}
		else if (Position < 0)
		{
			var profit = (_entryPrice - price) / _entryPrice;
			if (!_tp1Done && profit >= TakeProfitStep1)
				ExitPartial(TakeProfitPercent1, Sides.Buy, ref _tp1Done);
			if (!_tp2Done && profit >= TakeProfitStep2)
				ExitPartial(TakeProfitPercent2, Sides.Buy, ref _tp2Done);
			if (!_tp3Done && profit >= TakeProfitStep3)
				ExitPartial(TakeProfitPercent3, Sides.Buy, ref _tp3Done);
			if (!_tp4Done && profit >= TakeProfitStep4)
				ExitPartial(TakeProfitPercent4, Sides.Buy, ref _tp4Done);
		}
	}

	private void ExitPartial(decimal percent, Sides side, ref bool flag)
	{
		var volume = Math.Abs(Position) * percent;
		if (volume <= 0)
			return;

		if (side == Sides.Sell)
			SellMarket(volume);
		else
			BuyMarket(volume);

		flag = true;
	}

	private void ClosePosition()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0)
			return;

		if (Position > 0)
			SellMarket(volume);
		else
			BuyMarket(volume);
	}
}

