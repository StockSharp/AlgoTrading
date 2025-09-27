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

public class EnvelopeMaShortStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _envelopePeriod;
	private readonly StrategyParam<decimal> _envelopeDeviation;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _liquidityThreshold;

	private ExponentialMovingAverage _envelopeMa = null!;
	private ExponentialMovingAverage _fastMa = null!;
	private ExponentialMovingAverage _slowMa = null!;
	private ParabolicSar _sarYellow = null!;
	private ParabolicSar _sarGreen = null!;
	private ParabolicSar _sarBlue = null!;

	private decimal? _previousSlowValue;
	private decimal? _shortEntryPrice;
	private DateTimeOffset? _shortEntryTime;
	private decimal? _shortStopLossPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _lowestHighSinceEntry;
	private Order _sellStopOrder;
	private DateTimeOffset? _sellStopExpiration;

	private decimal _pipSize;
	private decimal _pointValue;
	private TimeSpan? _candleTimeFrame;

	public EnvelopeMaShortStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Timeframe used to evaluate signals.", "General");

		_envelopePeriod = Param(nameof(EnvelopePeriod), 280)
			.SetGreaterThanZero()
			.SetDisplay("Envelope period", "EMA length used as the envelope base.", "Indicator");

		_envelopeDeviation = Param(nameof(EnvelopeDeviation), 0.08m)
			.SetNotNegative()
			.SetDisplay("Envelope deviation", "Width of the envelope expressed in percent.", "Indicator");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA period", "Length of the fast EMA calculated on lows.", "Indicator");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA period", "Length of the slow EMA used with a one-bar delay.", "Indicator");

		_stopLossPips = Param(nameof(StopLossPips), 25)
			.SetNotNegative()
			.SetDisplay("Stop-loss (pips)", "Distance from entry to the protective stop in pips.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 25)
			.SetNotNegative()
			.SetDisplay("Take-profit (pips)", "Distance from entry to the profit target in pips.", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Volume used for pending and market orders.", "Trading");

		_liquidityThreshold = Param(nameof(LiquidityThreshold), 0.58m)
			.SetGreaterThanZero()
			.SetDisplay("Equity ratio", "Close all shorts when equity to balance ratio drops below this value.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EnvelopePeriod
	{
		get => _envelopePeriod.Value;
		set => _envelopePeriod.Value = value;
	}

	public decimal EnvelopeDeviation
	{
		get => _envelopeDeviation.Value;
		set => _envelopeDeviation.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	public decimal LiquidityThreshold
	{
		get => _liquidityThreshold.Value;
		set => _liquidityThreshold.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousSlowValue = null;
		_sellStopOrder = null;
		_sellStopExpiration = null;
		ResetShortState();
		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_envelopeMa = new ExponentialMovingAverage { Length = EnvelopePeriod };
		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };

		_sarYellow = new ParabolicSar
		{
			Acceleration = 0.03m,
			AccelerationStep = 0.03m,
			AccelerationMax = 0.5m
		};

		_sarGreen = new ParabolicSar
		{
			Acceleration = 0.015m,
			AccelerationStep = 0.015m,
			AccelerationMax = 0.6m
		};

		_sarBlue = new ParabolicSar
		{
			Acceleration = 0.02m,
			AccelerationStep = 0.02m,
			AccelerationMax = 0.2m
		};

		_previousSlowValue = null;
		ResetShortState();

		_pipSize = CalculatePipSize();
		_pointValue = GetPointValue();
		_candleTimeFrame = CandleType.Arg is TimeSpan span ? span : null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sarYellow, _sarGreen, _sarBlue, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _envelopeMa);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _sarYellow);
			DrawIndicator(area, _sarGreen);
			DrawIndicator(area, _sarBlue);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarYellow, decimal sarGreen, decimal sarBlue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var envelopeValue = _envelopeMa.Process(new CandleIndicatorValue(candle, candle.HighPrice));
		var fastValue = _fastMa.Process(new CandleIndicatorValue(candle, candle.LowPrice));
		var slowValue = _slowMa.Process(new CandleIndicatorValue(candle, candle.LowPrice));

		if (!envelopeValue.IsFinal || !fastValue.IsFinal || !slowValue.IsFinal)
		{
			if (slowValue.IsFinal)
				_previousSlowValue = slowValue.GetValue<decimal>();

			CancelExpiredSellStop(candle.CloseTime);
			return;
		}

		var envelopeBase = envelopeValue.GetValue<decimal>();
		var fast = fastValue.GetValue<decimal>();
		var slowCurrent = slowValue.GetValue<decimal>();
		var slowShifted = _previousSlowValue ?? slowCurrent;

		_previousSlowValue = slowCurrent;

		var deviationFactor = EnvelopeDeviation / 100m;
		var envelopeUpper = envelopeBase * (1m + deviationFactor);
		var envelopeLower = envelopeBase * (1m - deviationFactor);

		ManageShortPosition(candle, fast, slowShifted, sarYellow, sarGreen, sarBlue, envelopeLower);

		CancelExpiredSellStop(candle.CloseTime);

		if (Position < 0m)
			CancelIfActive(ref _sellStopOrder);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ShouldForceExit())
		{
			CancelIfActive(ref _sellStopOrder);
			CloseShortPosition();
			return;
		}

		if (Position < 0m || IsOrderActive(_sellStopOrder))
			return;

		var closePrice = candle.ClosePrice;

		if (fast > envelopeLower && slowShifted > envelopeLower && closePrice > envelopeLower &&
			fast <= envelopeUpper && slowShifted <= envelopeUpper && closePrice <= envelopeUpper)
		{
			PlaceSellStop(envelopeLower, candle.CloseTime);
		}
	}

	private void ManageShortPosition(ICandleMessage candle, decimal fast, decimal slowShifted, decimal sarYellow, decimal sarGreen, decimal sarBlue, decimal envelopeLower)
	{
		if (Position >= 0m)
		{
			_lowestHighSinceEntry = null;
			return;
		}

		var closePrice = candle.ClosePrice;

		if (_shortStopLossPrice is decimal stop && candle.HighPrice >= stop)
		{
			CloseShortPosition();
			return;
		}

		if (_shortTakeProfitPrice is decimal take && candle.LowPrice <= take)
		{
			CloseShortPosition();
			return;
		}

		if (_shortEntryPrice is not decimal entryPrice)
			return;

		_lowestHighSinceEntry = _lowestHighSinceEntry is decimal lowest
			? Math.Min(lowest, candle.HighPrice)
			: candle.HighPrice;

		var sarBelowPrice = sarYellow < closePrice && sarGreen < closePrice && sarBlue < closePrice;
		var averagesBelowEntry = slowShifted < entryPrice && fast < entryPrice && closePrice < entryPrice;

		if (averagesBelowEntry && sarBelowPrice && fast > slowShifted)
		{
			CloseShortPosition();
			return;
		}

		if (_shortEntryTime is not DateTimeOffset entryTime || _candleTimeFrame is not TimeSpan frame || frame <= TimeSpan.Zero)
			return;

		if (_lowestHighSinceEntry is not decimal lowestHigh)
			return;

		var elapsed = candle.CloseTime - entryTime;
		if (elapsed < TimeSpan.Zero)
			return;

		var frameSeconds = frame.TotalSeconds;
		if (frameSeconds <= 0)
			return;

		var candlesElapsed = (int)Math.Ceiling(elapsed.TotalSeconds / frameSeconds);
		if (candlesElapsed < 4)
			return;

		if (_pointValue <= 0m)
			return;

		var threshold = entryPrice - _pointValue * 3m;
		if (lowestHigh < threshold && closePrice < envelopeLower)
		{
			var newStop = RoundPrice(envelopeLower);
			if (_shortStopLossPrice is not decimal currentStop || currentStop != newStop)
				_shortStopLossPrice = newStop;
		}
	}

	private void PlaceSellStop(decimal envelopeLower, DateTimeOffset closeTime)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		var price = RoundPrice(envelopeLower);
		if (price <= 0m)
			return;

		var order = SellStop(volume, price);
		if (order == null)
			return;

		_sellStopOrder = order;

		if (_candleTimeFrame is TimeSpan frame && frame > TimeSpan.Zero)
			_sellStopExpiration = closeTime + TimeSpan.FromTicks(frame.Ticks * 5);
		else
			_sellStopExpiration = null;
	}

	private void CancelExpiredSellStop(DateTimeOffset currentTime)
	{
		if (_sellStopOrder == null || _sellStopExpiration is not DateTimeOffset expiration)
			return;

		if (currentTime < expiration)
			return;

		CancelIfActive(ref _sellStopOrder);
		_sellStopExpiration = null;
	}

	private static bool IsOrderActive(Order order)
	{
		if (order == null)
			return false;

		return order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active;
	}

	private void CancelIfActive(ref Order order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.Pending or OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private void CloseShortPosition()
	{
		var position = Position;
		if (position < 0m)
			BuyMarket(Math.Abs(position));

		ResetShortState();
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortEntryTime = null;
		_shortStopLossPrice = null;
		_shortTakeProfitPrice = null;
		_lowestHighSinceEntry = null;
	}

	private bool ShouldForceExit()
	{
		var threshold = LiquidityThreshold;
		if (threshold <= 0m)
			return false;

		var portfolio = Portfolio;
		if (portfolio == null)
			return false;

		var balance = portfolio.BeginValue ?? 0m;
		if (balance <= 0m)
			return false;

		var equity = portfolio.CurrentValue ?? balance;

		return equity / balance < threshold;
	}

	private decimal ConvertPipsToPrice(int pips)
	{
		if (pips <= 0)
			return 0m;

		return _pipSize > 0m ? _pipSize * pips : pips;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? security.MinStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var bits = decimal.GetBits(step);
		var scale = (bits[3] >> 16) & 0x7F;

		return scale is 3 or 5 ? step * 10m : step;
	}

	private decimal GetPointValue()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? security.MinStep ?? 0m;
		return step > 0m ? step : 0m;
	}

	private decimal RoundPrice(decimal price)
	{
		var security = Security;
		if (security == null)
			return price;

		var step = security.PriceStep;
		if (step == null || step.Value <= 0m)
			return price;

		return Math.Round(price / step.Value, 0, MidpointRounding.AwayFromZero) * step.Value;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_sellStopOrder != null && order == _sellStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_sellStopOrder = null;
			_sellStopExpiration = null;
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null)
			return;

		var direction = order.Direction;

		if (direction == Sides.Sell)
		{
			var entryPrice = trade.Trade.Price;
			_shortEntryPrice = entryPrice;
			_shortEntryTime = trade.Trade.ServerTime;
			_lowestHighSinceEntry = trade.Trade.Price;

			var stopDistance = ConvertPipsToPrice(StopLossPips);
			_shortStopLossPrice = stopDistance > 0m ? RoundPrice(entryPrice + stopDistance) : null;

			var takeDistance = ConvertPipsToPrice(TakeProfitPips);
			_shortTakeProfitPrice = takeDistance > 0m ? RoundPrice(entryPrice - takeDistance) : null;
		}
		else if (direction == Sides.Buy && Position >= 0m)
		{
			ResetShortState();
		}
	}
}
