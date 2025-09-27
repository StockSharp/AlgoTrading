namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class FirebirdMaEnvelopeExhaustionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _percent;
	private readonly StrategyParam<bool> _tradeOnFriday;
	private readonly StrategyParam<bool> _useHighLow;
	private readonly StrategyParam<int> _pipStep;
	private readonly StrategyParam<decimal> _increasementPower;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _volume;

	private SimpleMovingAverage _sma;

	private readonly List<decimal> _longEntries = new();
	private readonly List<decimal> _shortEntries = new();

	private bool _blockLong;
	private bool _blockShort;

	public FirebirdMaEnvelopeExhaustionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type");
		_maLength = Param(nameof(MaLength), 10).SetDisplay("MA Length");
		_percent = Param(nameof(Percent), 0.3m).SetDisplay("Percent Envelope");
		_tradeOnFriday = Param(nameof(TradeOnFriday), true).SetDisplay("Trade On Friday");
		_useHighLow = Param(nameof(UseHighLow), false).SetDisplay("Use High/Low Source");
		_pipStep = Param(nameof(PipStep), 30).SetDisplay("Pip Step");
		_increasementPower = Param(nameof(IncreasementPower), 0m).SetDisplay("Increasement Power");
		_takeProfit = Param(nameof(TakeProfit), 30m).SetDisplay("Take Profit (pips)");
		_stopLoss = Param(nameof(StopLoss), 200m).SetDisplay("Stop Loss (pips)");
		_volume = Param(nameof(Volume), 1m).SetDisplay("Trade Volume");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public decimal Percent
	{
		get => _percent.Value;
		set => _percent.Value = value;
	}

	public bool TradeOnFriday
	{
		get => _tradeOnFriday.Value;
		set => _tradeOnFriday.Value = value;
	}

	public bool UseHighLow
	{
		get => _useHighLow.Value;
		set => _useHighLow.Value = value;
	}

	public int PipStep
	{
		get => _pipStep.Value;
		set => _pipStep.Value = value;
	}

	public decimal IncreasementPower
	{
		get => _increasementPower.Value;
		set => _increasementPower.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TradeVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new() { Length = MaLength };

		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.WhenCandlesFinished(ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var point = GetPoint();

		if (point == 0m)
		return;

		var time = candle.CloseTime.UtcDateTime;

		if (!TradeOnFriday && time.DayOfWeek == DayOfWeek.Friday)
		return;

		var priceSource = UseHighLow ? (candle.HighPrice + candle.LowPrice) / 2m : candle.OpenPrice;
		var smaValue = _sma.Process(new DecimalIndicatorValue(_sma, priceSource));

		if (!smaValue.IsFinal)
		return;

		var sma = smaValue.GetValue<decimal>();
		var upperBand = sma * (1m + Percent / 100m);
		var lowerBand = sma * (1m - Percent / 100m);

		var close = candle.ClosePrice;

		var longCount = _longEntries.Count;
		var shortCount = _shortEntries.Count;

		var currentPipStep = GetCurrentPipStep(longCount, shortCount);
		var stepDistance = currentPipStep * point;

		if (close <= lowerBand)
		TryOpenLong(close, stepDistance);
		else if (close >= upperBand)
		TryOpenShort(close, stepDistance);

		if (_longEntries.Count > 0)
		CheckLongExit(close, point);
		else if (_shortEntries.Count > 0)
		CheckShortExit(close, point);
	}

	private decimal GetCurrentPipStep(int longCount, int shortCount)
	{
		var activeCount = Math.Max(longCount, shortCount);

		var baseStep = PipStep;

		if (IncreasementPower > 0m && activeCount > 0)
		{
			var power = (decimal)Math.Pow(activeCount, (double)IncreasementPower);
			return baseStep * power;
		}

		return baseStep;
	}

	private void TryOpenLong(decimal price, decimal stepDistance)
	{
		if (_blockLong)
		return;

		if (Position < 0)
		{
			ClosePosition();
			ClearShortState();
			_blockShort = false;
		}

		if (_longEntries.Count > 0 && price > _longEntries[^1] - stepDistance)
		return;

		BuyMarket();
		_longEntries.Add(price);
		_blockShort = false;
	}

	private void TryOpenShort(decimal price, decimal stepDistance)
	{
		if (_blockShort)
		return;

		if (Position > 0)
		{
			ClosePosition();
			ClearLongState();
			_blockLong = false;
		}

		if (_shortEntries.Count > 0 && price < _shortEntries[^1] + stepDistance)
		return;

		SellMarket();
		_shortEntries.Add(price);
		_blockLong = false;
	}

	private void CheckLongExit(decimal price, decimal point)
	{
		if (_longEntries.Count == 0)
		return;

		var averagePrice = _longEntries.Average();
		var takeProfitPrice = averagePrice + TakeProfit * point;
		var stopLossPrice = averagePrice - (StopLoss * point) / _longEntries.Count;

		if (price >= takeProfitPrice)
		{
			ClosePosition();
			ClearLongState();
			return;
		}

		if (price <= stopLossPrice)
		{
			ClosePosition();
			ClearLongState();
			_blockLong = true;
		}
	}

	private void CheckShortExit(decimal price, decimal point)
	{
		if (_shortEntries.Count == 0)
		return;

		var averagePrice = _shortEntries.Average();
		var takeProfitPrice = averagePrice - TakeProfit * point;
		var stopLossPrice = averagePrice + (StopLoss * point) / _shortEntries.Count;

		if (price <= takeProfitPrice)
		{
			ClosePosition();
			ClearShortState();
			return;
		}

		if (price >= stopLossPrice)
		{
			ClosePosition();
			ClearShortState();
			_blockShort = true;
		}
	}

	private decimal GetPoint()
	{
		var step = Security?.PriceStep;

		if (step is null || step == 0m)
		return 0.0001m;

		return step.Value;
	}

	private void ClearLongState()
	{
		_longEntries.Clear();
	}

	private void ClearShortState()
	{
		_shortEntries.Clear();
	}
}

