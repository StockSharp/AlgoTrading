namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Rally Base Drop SND Pivots strategy.
/// Enters on price crossing pivot levels derived from rally/drop sequences.
/// </summary>
public class RallyBaseDropSndPivotsStrategy : Strategy
{
	public enum TradeMode
	{
		LongAndShort,
		LongOnly,
		ShortOnly
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _mult;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<bool> _reverseConditions;
	private readonly StrategyParam<TradeMode> _mode;

	private readonly List<ICandleMessage> _candles = [];
	private readonly AverageTrueRange _atr = new() { Length = 14 };

	private decimal? _up;
	private decimal? _down;
	private decimal? _lastLongLevel;
	private decimal? _lastShortLevel;
	private decimal? _lastLongEntryPrice;
	private decimal? _lastLongEntryAtr;
	private decimal? _lastShortEntryPrice;
	private decimal? _lastShortEntryAtr;
	private decimal? _prevClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="RallyBaseDropSndPivotsStrategy"/>.
	/// </summary>
	public RallyBaseDropSndPivotsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_length = Param(nameof(Length), 3)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Pivot detection length", "General");

		_mult = Param(nameof(Mult), 1m)
			.SetDisplay("ATR Exit Multiplier", "ATR multiplier for stop", "Risk");

		_riskReward = Param(nameof(RiskReward), 6m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward Ratio", "Take profit multiple", "Risk");

		_reverseConditions = Param(nameof(ReverseConditions), false)
			.SetDisplay("Reverse Conditions", "Swap long/short levels", "General");

		_mode = Param(nameof(Mode), TradeMode.LongAndShort)
			.SetDisplay("Trade Mode", "Allowed trade direction", "General");
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Pivot detection length.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// ATR exit multiplier.
	/// </summary>
	public decimal Mult { get => _mult.Value; set => _mult.Value = value; }

	/// <summary>
	/// Risk reward ratio.
	/// </summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	/// <summary>
	/// Reverse long/short conditions.
	/// </summary>
	public bool ReverseConditions { get => _reverseConditions.Value; set => _reverseConditions.Value = value; }

	/// <summary>
	/// Trading mode.
	/// </summary>
	public TradeMode Mode { get => _mode.Value; set => _mode.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candles.Clear();
		_up = _down = _lastLongLevel = _lastShortLevel = null;
		_lastLongEntryPrice = _lastLongEntryAtr = null;
		_lastShortEntryPrice = _lastShortEntryAtr = null;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Add(candle);
		var max = Length * 2;
		if (_candles.Count > max)
			_candles.RemoveAt(0);

		DetectPivots();

		if (!_atr.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (!ReverseConditions)
		{
			if (Mode != TradeMode.ShortOnly && _up is decimal upLvl && _prevClose is decimal pc1 && pc1 <= upLvl && candle.ClosePrice > upLvl && (_lastLongLevel != upLvl))
			{
				_lastLongLevel = upLvl;
				_lastLongEntryPrice = candle.ClosePrice;
				_lastLongEntryAtr = atrValue;
				BuyMarket();
			}

			if (Mode != TradeMode.LongOnly && _down is decimal dnLvl && _prevClose is decimal pc2 && pc2 >= dnLvl && candle.ClosePrice < dnLvl && (_lastShortLevel != dnLvl))
			{
				_lastShortLevel = dnLvl;
				_lastShortEntryPrice = candle.ClosePrice;
				_lastShortEntryAtr = atrValue;
				SellMarket();
			}
		}
		else
		{
			if (Mode != TradeMode.ShortOnly && _down is decimal dnLvl && _prevClose is decimal pc1 && pc1 >= dnLvl && candle.ClosePrice < dnLvl && (_lastLongLevel != dnLvl))
			{
				_lastLongLevel = dnLvl;
				_lastLongEntryPrice = candle.ClosePrice;
				_lastLongEntryAtr = atrValue;
				BuyMarket();
			}

			if (Mode != TradeMode.LongOnly && _up is decimal upLvl && _prevClose is decimal pc2 && pc2 <= upLvl && candle.ClosePrice > upLvl && (_lastShortLevel != upLvl))
			{
				_lastShortLevel = upLvl;
				_lastShortEntryPrice = candle.ClosePrice;
				_lastShortEntryAtr = atrValue;
				SellMarket();
			}
		}

		if (Position > 0 && _lastLongEntryPrice is decimal lep && _lastLongEntryAtr is decimal la)
		{
			var longProfit = lep + la * Mult * RiskReward;
			var longStop = lep - la * Mult;
			if (candle.LowPrice <= longStop || candle.HighPrice >= longProfit)
			{
				SellMarket();
				_lastLongEntryPrice = _lastLongEntryAtr = null;
			}
		}

		if (Position < 0 && _lastShortEntryPrice is decimal sep && _lastShortEntryAtr is decimal sa)
		{
			var shortProfit = sep - sa * Mult * RiskReward;
			var shortStop = sep + sa * Mult;
			if (candle.HighPrice >= shortStop || candle.LowPrice <= shortProfit)
			{
				BuyMarket();
				_lastShortEntryPrice = _lastShortEntryAtr = null;
			}
		}

		_prevClose = candle.ClosePrice;
	}

	private void DetectPivots()
	{
		var count = _candles.Count;
		if (count < Length * 2)
			return;

		var currClose = _candles[^1].ClosePrice;
		var closeLenAgo = _candles[count - Length - 1].ClosePrice;

		for (var i = 0; i < Length; i++)
		{
			var recent = _candles[count - 1 - i];
			var prev = _candles[count - Length - 1 - i];

			if (!(recent.ClosePrice > recent.OpenPrice) || !(prev.ClosePrice < prev.OpenPrice))
				goto HighCheck;

			if (i == Length - 1 && currClose > prev.OpenPrice && closeLenAgo < prev.ClosePrice)
				_down = recent.LowPrice;
		}

	HighCheck:
		for (var i = 0; i < Length; i++)
		{
			var recent = _candles[count - 1 - i];
			var prev = _candles[count - Length - 1 - i];

			if (!(recent.ClosePrice < recent.OpenPrice) || !(prev.ClosePrice > prev.OpenPrice))
				return;

			if (i == Length - 1 && currClose < prev.OpenPrice && closeLenAgo > prev.ClosePrice)
				_up = recent.HighPrice;
		}
	}
}
