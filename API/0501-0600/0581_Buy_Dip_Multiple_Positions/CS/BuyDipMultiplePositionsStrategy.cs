using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buy Dip Multiple Positions strategy.
/// Long only: a candle that closes 0.2% below the previous low, with volume above 120% of the average of the two previous bars and
/// a close below PriceSurgePercent percent of the close SurgeLookbackBars bars ago, adds a long (up to MaxPositions entries) while
/// the last closed trade was profitable. Each entry risks 2% of the portfolio value against the stop. Every entry resets the
/// shared levels: the stop at InitialStopPercent percent of the entry bar low, which then rises by TrailRatePercent percent each
/// bar, and the target TargetPricePercent percent above the entry bar low. Touching either level closes the whole position.
/// </summary>
public class BuyDipMultiplePositionsStrategy : Strategy
{
	private const decimal _dipPercent = 0.2m;
	private const decimal _volumeRatio = 1.2m;
	private const decimal _riskPercent = 2m;

	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _trailRatePercent;
	private readonly StrategyParam<decimal> _initialStopPercent;
	private readonly StrategyParam<decimal> _targetPricePercent;
	private readonly StrategyParam<decimal> _priceSurgePercent;
	private readonly StrategyParam<int> _surgeLookbackBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();
	private decimal? _prevLow;
	private decimal? _prevVolume1;
	private decimal? _prevVolume2;

	private int _entries;
	private decimal? _stopPrice;
	private decimal? _targetPrice;
	private bool _lastTradeProfitable = true;
	private decimal _pnlAtOpen;

	/// <summary>
	/// Maximum number of stacked entries.
	/// </summary>
	public int MaxPositions { get => _maxPositions.Value; set => _maxPositions.Value = value; }

	/// <summary>
	/// Percent the trailing stop rises each bar.
	/// </summary>
	public decimal TrailRatePercent { get => _trailRatePercent.Value; set => _trailRatePercent.Value = value; }

	/// <summary>
	/// Initial stop as a percent of the entry bar low.
	/// </summary>
	public decimal InitialStopPercent { get => _initialStopPercent.Value; set => _initialStopPercent.Value = value; }

	/// <summary>
	/// Target distance above the entry bar low in percent.
	/// </summary>
	public decimal TargetPricePercent { get => _targetPricePercent.Value; set => _targetPricePercent.Value = value; }

	/// <summary>
	/// Close must be below this percent of the close SurgeLookbackBars bars ago.
	/// </summary>
	public decimal PriceSurgePercent { get => _priceSurgePercent.Value; set => _priceSurgePercent.Value = value; }

	/// <summary>
	/// Bars back of the comparison close.
	/// </summary>
	public int SurgeLookbackBars { get => _surgeLookbackBars.Value; set => _surgeLookbackBars.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BuyDipMultiplePositionsStrategy()
	{
		_maxPositions = Param(nameof(MaxPositions), 20)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of stacked entries", "Trading");

		_trailRatePercent = Param(nameof(TrailRatePercent), 1m)
			.SetNotNegative()
			.SetDisplay("Trail Rate %", "Percent the trailing stop rises each bar", "Risk");

		_initialStopPercent = Param(nameof(InitialStopPercent), 85m)
			.SetRange(0m, 100m)
			.SetDisplay("Initial Stop %", "Initial stop as a percent of the entry bar low", "Risk");

		_targetPricePercent = Param(nameof(TargetPricePercent), 60m)
			.SetNotNegative()
			.SetDisplay("Target Price %", "Target distance above the entry bar low", "Risk");

		_priceSurgePercent = Param(nameof(PriceSurgePercent), 89m)
			.SetGreaterThanZero()
			.SetDisplay("Price Surge %", "Close must be below this percent of the older close", "Entry");

		_surgeLookbackBars = Param(nameof(SurgeLookbackBars), 14)
			.SetGreaterThanZero()
			.SetDisplay("Surge Lookback Bars", "Bars back of the comparison close", "Entry");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame())
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_closes.Clear();
		_prevLow = null;
		_prevVolume1 = null;
		_prevVolume2 = null;
		_entries = 0;
		_stopPrice = null;
		_targetPrice = null;
		_lastTradeProfitable = true;
		_pnlAtOpen = 0;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var prevLow = _prevLow;
		var prevVolume1 = _prevVolume1;
		var prevVolume2 = _prevVolume2;

		decimal? oldClose = _closes.Count >= SurgeLookbackBars ? _closes[_closes.Count - SurgeLookbackBars] : null;

		_closes.Add(close);
		while (_closes.Count > SurgeLookbackBars)
			_closes.RemoveAt(0);

		_prevLow = candle.LowPrice;
		_prevVolume2 = prevVolume1;
		_prevVolume1 = candle.TotalVolume;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && CheckExit(candle))
			return;

		if (prevLow is not decimal pl || prevVolume1 is not decimal v1 || prevVolume2 is not decimal v2 || oldClose is not decimal oc)
			return;

		var dip = close < pl * (1m - _dipPercent / 100m);
		var highVolume = candle.TotalVolume > _volumeRatio * (v1 + v2) / 2m;
		var surge = close < oc * PriceSurgePercent / 100m;

		if (!dip || !highVolume || !surge || !_lastTradeProfitable || _entries >= MaxPositions)
			return;

		var stop = candle.LowPrice * InitialStopPercent / 100m;
		var volume = GetEntryVolume(close, stop);
		if (volume <= 0)
			return;

		BuyMarket(volume);
		_entries++;
		_stopPrice = stop;
		_targetPrice = candle.LowPrice * (1m + TargetPricePercent / 100m);
	}

	private bool CheckExit(ICandleMessage candle)
	{
		if (_stopPrice is decimal stop && candle.LowPrice <= stop
			|| _targetPrice is decimal target && candle.HighPrice >= target)
		{
			SellMarket(Position);
			_entries = 0;
			_stopPrice = null;
			_targetPrice = null;
			return true;
		}

		if (_stopPrice is decimal current)
			_stopPrice = current * (1m + TrailRatePercent / 100m);

		return false;
	}

	private decimal GetEntryVolume(decimal price, decimal stop)
	{
		var risk = price - stop;
		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0 || risk <= 0)
			return Volume;

		var volume = equity * _riskPercent / 100m / risk;

		if (Security?.VolumeStep is decimal step && step > 0)
			volume = Math.Floor(volume / step) * step;

		if (Security?.MaxVolume is decimal maxVolume && maxVolume > 0 && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order?.Side == Sides.Sell && Position <= 0)
		{
			_lastTradeProfitable = PnL - _pnlAtOpen > 0;
			_pnlAtOpen = PnL;
		}
	}
}
