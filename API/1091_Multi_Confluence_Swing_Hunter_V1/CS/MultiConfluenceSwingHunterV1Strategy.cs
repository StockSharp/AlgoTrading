namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Multi-confluence swing hunter strategy using RSI, trend confirmation, and bullish candle scoring.
/// </summary>
public class MultiConfluenceSwingHunterV1Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _minEntryScore;
	private readonly StrategyParam<int> _minExitScore;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrevValues;
	private int _cooldownRemaining;
	private DateTimeOffset? _lastEntryTime;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public int MinEntryScore { get => _minEntryScore.Value; set => _minEntryScore.Value = value; }
	public int MinExitScore { get => _minExitScore.Value; set => _minExitScore.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiConfluenceSwingHunterV1Strategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "Indicators");

		_smaLength = Param(nameof(SmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA period", "Indicators");

		_minEntryScore = Param(nameof(MinEntryScore), 4)
			.SetDisplay("Min Entry Score", "Minimum entry score", "Entry");

		_minExitScore = Param(nameof(MinExitScore), 3)
			.SetDisplay("Min Exit Score", "Minimum exit score", "Exit");

		_rsiOversold = Param(nameof(RsiOversold), 35m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 65m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 48)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait after an entry", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_prevRsi = 0m;
		_hasPrevValues = false;
		_cooldownRemaining = 0;
		_lastEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var sma = new SMA { Length = SmaLength };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrevValues)
		{
			_prevRsi = rsiValue;
			_hasPrevValues = true;
			return;
		}

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var entryScore = 0;
		if (rsiValue < RsiOversold)
			entryScore += 2;
		if (rsiValue > _prevRsi)
			entryScore += 1;
		if (candle.ClosePrice > smaValue)
			entryScore += 1;
		if (candle.ClosePrice > candle.OpenPrice)
			entryScore += 1;

		var exitScore = 0;
		if (rsiValue > RsiOverbought)
			exitScore += 2;
		if (rsiValue < _prevRsi)
			exitScore += 1;
		if (candle.ClosePrice < smaValue)
			exitScore += 1;
		if (candle.ClosePrice < candle.OpenPrice)
			exitScore += 1;

		if (Position > 0 && exitScore >= MinExitScore)
		{
			SellMarket(Position);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (Position == 0 && _cooldownRemaining == 0 && !HasRecentEntry(candle) && entryScore >= MinEntryScore)
		{
			BuyMarket();
			_cooldownRemaining = SignalCooldownBars;
			_lastEntryTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		}

		_prevRsi = rsiValue;
	}

	private bool HasRecentEntry(ICandleMessage candle)
	{
		if (!_lastEntryTime.HasValue)
			return false;

		var candleTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		return candleTime.Date == _lastEntryTime.Value.Date;
	}
}
