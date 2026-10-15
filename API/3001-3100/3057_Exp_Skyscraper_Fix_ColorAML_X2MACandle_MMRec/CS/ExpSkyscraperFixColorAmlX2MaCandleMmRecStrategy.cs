namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Combines Skyscraper Fix, ColorAML and X2MA-style filters into a single consensus strategy.
/// </summary>
public class ExpSkyscraperFixColorAmlX2MaCandleMmRecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<decimal> _channelFactor;
	private readonly StrategyParam<int> _amlLength;
	private readonly StrategyParam<int> _x2FastLength;
	private readonly StrategyParam<int> _x2SlowLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _closes = new();
	private readonly List<decimal> _weightedPrices = new();
	private readonly List<decimal> _amlSeries = new();
	private readonly List<decimal> _fastSeries = new();
	private readonly List<decimal> _slowSeries = new();

	private decimal? _previousAml;
	private int _previousConsensus;
	private decimal? _entryPrice;
	private int _cooldownLeft;

	public ExpSkyscraperFixColorAmlX2MaCandleMmRecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_channelLength = Param(nameof(ChannelLength), 10).SetGreaterThanZero().SetDisplay("Channel Length", "ATR channel length", "Skyscraper");
		_channelFactor = Param(nameof(ChannelFactor), 0.9m).SetGreaterThanZero().SetDisplay("Channel Factor", "ATR multiplier", "Skyscraper");
		_amlLength = Param(nameof(AmlLength), 7).SetGreaterThanZero().SetDisplay("AML Length", "Adaptive smoothing length", "ColorAML");
		_x2FastLength = Param(nameof(X2FastLength), 12).SetGreaterThanZero().SetDisplay("X2 Fast", "Fast smoothing length", "X2MA");
		_x2SlowLength = Param(nameof(X2SlowLength), 5).SetGreaterThanZero().SetDisplay("X2 Slow", "Slow smoothing length", "X2MA");
		_cooldownBars = Param(nameof(CooldownBars), 2).SetNotNegative().SetDisplay("Cooldown Bars", "Bars between flips", "Trading");
		_stopLossPips = Param(nameof(StopLossPips), 500).SetNotNegative().SetDisplay("Stop Loss", "Stop distance in pips", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 900).SetNotNegative().SetDisplay("Take Profit", "Take-profit distance in pips", "Risk");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int ChannelLength { get => _channelLength.Value; set => _channelLength.Value = value; }
	public decimal ChannelFactor { get => _channelFactor.Value; set => _channelFactor.Value = value; }
	public int AmlLength { get => _amlLength.Value; set => _amlLength.Value = value; }
	public int X2FastLength { get => _x2FastLength.Value; set => _x2FastLength.Value = value; }
	public int X2SlowLength { get => _x2SlowLength.Value; set => _x2SlowLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highs.Clear();
		_lows.Clear();
		_closes.Clear();
		_weightedPrices.Clear();
		_amlSeries.Clear();
		_fastSeries.Clear();
		_slowSeries.Clear();
		_previousAml = null;
		_previousConsensus = 0;
		_entryPrice = null;
		_cooldownLeft = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		OnReseted();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownLeft > 0)
			_cooldownLeft--;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		_closes.Add(candle.ClosePrice);

		var weightedPrice = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 5m;
		_weightedPrices.Add(weightedPrice);

		var fast = CalculateEma(_closes, X2FastLength, _fastSeries);
		var slow = CalculateEma(_fastSeries, X2SlowLength, _slowSeries);
		var aml = CalculateEma(_weightedPrices, AmlLength, _amlSeries);

		if (Position != 0 && _entryPrice is null)
			_entryPrice = candle.ClosePrice;

		if (TryExitByRisk(candle))
			return;

		if (_highs.Count <= Math.Max(ChannelLength, Math.Max(AmlLength, X2FastLength + X2SlowLength)))
		{
			_previousAml = aml;
			return;
		}

		var skyscraperSignal = GetSkyscraperSignal();
		var colorAmlSignal = _previousAml is decimal previousAml
			? aml > previousAml ? 1 : aml < previousAml ? -1 : 0
			: 0;
		var x2MaSignal = fast > slow && candle.ClosePrice >= candle.OpenPrice
			? 1
			: fast < slow && candle.ClosePrice <= candle.OpenPrice
				? -1
				: 0;

		_previousAml = aml;

		var score = skyscraperSignal + colorAmlSignal + x2MaSignal;
		var consensus = score >= 2 ? 1 : score <= -2 ? -1 : 0;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousConsensus = consensus;
			return;
		}

		if (consensus == _previousConsensus || consensus == 0 || _cooldownLeft > 0)
		{
			_previousConsensus = consensus;
			return;
		}

		if (consensus > 0 && Position <= 0)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = null;
			}
			else
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}

			_cooldownLeft = CooldownBars;
		}
		else if (consensus < 0 && Position >= 0)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				_entryPrice = null;
			}
			else
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}

			_cooldownLeft = CooldownBars;
		}

		_previousConsensus = consensus;
	}

	private int GetSkyscraperSignal()
	{
		var length = ChannelLength;
		if (_closes.Count < length || _highs.Count < length || _lows.Count < length)
			return 0;

		var start = _closes.Count - length;
		decimal atrSum = 0m;

		for (var i = start; i < _closes.Count; i++)
		{
			var high = _highs[i];
			var low = _lows[i];
			var previousClose = i > 0 ? _closes[i - 1] : _closes[i];
			var trueRange = Math.Max(high - low, Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose)));
			atrSum += trueRange;
		}

		var atr = atrSum / length;
		var middle = (_highs[^1] + _lows[^1]) / 2m;
		var upper = middle + atr * ChannelFactor;
		var lower = middle - atr * ChannelFactor;
		var close = _closes[^1];

		if (close > upper)
			return 1;

		if (close < lower)
			return -1;

		return 0;
	}

	private bool TryExitByRisk(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entryPrice || Position == 0)
			return false;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0)
			step = 1m;

		var stopDistance = StopLossPips * step;
		var takeDistance = TakeProfitPips * step;

		if (Position > 0)
		{
			if ((stopDistance > 0 && candle.LowPrice <= entryPrice - stopDistance) ||
				(takeDistance > 0 && candle.HighPrice >= entryPrice + takeDistance))
			{
				SellMarket(Position);
				_entryPrice = null;
				_cooldownLeft = CooldownBars;
				return true;
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);

			if ((stopDistance > 0 && candle.HighPrice >= entryPrice + stopDistance) ||
				(takeDistance > 0 && candle.LowPrice <= entryPrice - takeDistance))
			{
				BuyMarket(volume);
				_entryPrice = null;
				_cooldownLeft = CooldownBars;
				return true;
			}
		}

		return false;
	}

	private static decimal CalculateEma(IReadOnlyList<decimal> source, int length, List<decimal> target)
	{
		var multiplier = 2m / (length + 1m);
		var value = target is { Count: > 0 }
			? source[^1] * multiplier + target[^1] * (1m - multiplier)
			: source[^1];

		target?.Add(value);
		return value;
	}
}
