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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Fractured Fractals" MetaTrader strategy using high-level StockSharp API.
/// Places stop orders on newly confirmed fractals and trails the stop with the opposite fractal.
/// </summary>
public class FracturedFractalsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maximumRiskPercent;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _expirationHours;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highBuffer = new();
	private readonly Queue<decimal> _lowBuffer = new();

	private decimal? _lastUpFractal;
	private decimal? _lastDownFractal;
	private decimal? _upYoungest;
	private decimal? _upMiddle;
	private decimal? _upOld;
	private decimal? _downYoungest;
	private decimal? _downMiddle;
	private decimal? _downOld;

	private decimal? _buyStopLevel;
	private decimal? _sellStopLevel;
	private decimal? _longStopLevel;
	private decimal? _shortStopLevel;
	private DateTimeOffset? _buyStopExpiry;
	private DateTimeOffset? _sellStopExpiry;
	private decimal _buyStopVolume;
	private decimal _sellStopVolume;

	private decimal _entryPrice;
	private int _consecutiveLosses;

	/// <summary>
	/// Maximum risk per trade expressed as percentage of portfolio value.
	/// </summary>
	public decimal MaximumRiskPercent
	{
		get => _maximumRiskPercent.Value;
		set => _maximumRiskPercent.Value = value;
	}

	/// <summary>
	/// Factor that reduces position size after consecutive losing trades.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Pending order lifetime in hours.
	/// </summary>
	public int ExpirationHours
	{
		get => _expirationHours.Value;
		set => _expirationHours.Value = value;
	}

	/// <summary>
	/// Candle type used for fractal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FracturedFractalsStrategy"/> with default parameters.
	/// </summary>
	public FracturedFractalsStrategy()
	{
		_maximumRiskPercent = Param(nameof(MaximumRiskPercent), 2m)
		.SetRange(0.0001m, 100m)
		.SetDisplay("Max Risk %", "Maximum risk per trade", "Risk");

		_decreaseFactor = Param(nameof(DecreaseFactor), 10m)
		.SetRange(0m, 1000m)
		.SetDisplay("Decrease Factor", "Loss streak position size dampener", "Risk");

		_expirationHours = Param(nameof(ExpirationHours), 1)
		.SetRange(0, 240)
		.SetDisplay("Expiration", "Pending order lifetime (hours)", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");
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

		_highBuffer.Clear();
		_lowBuffer.Clear();

		_lastUpFractal = null;
		_lastDownFractal = null;
		_upYoungest = null;
		_upMiddle = null;
		_upOld = null;
		_downYoungest = null;
		_downMiddle = null;
		_downOld = null;

		_buyStopLevel = null;
		_sellStopLevel = null;
		_longStopLevel = null;
		_shortStopLevel = null;
		_buyStopExpiry = null;
		_sellStopExpiry = null;
		_buyStopVolume = 0m;
		_sellStopVolume = 0m;

		_entryPrice = 0m;
		_consecutiveLosses = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highBuffer.Enqueue(candle.HighPrice);
		_lowBuffer.Enqueue(candle.LowPrice);

		if (_highBuffer.Count > 5)
			_highBuffer.Dequeue();
		if (_lowBuffer.Count > 5)
			_lowBuffer.Dequeue();

		if (_highBuffer.Count < 5 || _lowBuffer.Count < 5)
			return;

		DetectFractals();

		// Check protective stop levels
		CheckProtectiveStops(candle);

		// Validate pending levels
		ValidatePendingLevels(candle.CloseTime);

		// Check if pending buy/sell stop levels are triggered
		CheckPendingTriggers(candle);

		// Update trailing stops
		UpdateTrailingStops();

		// Try to set new pending levels
		if (Position == 0)
		{
			if (!TrySetBuyStopLevel(candle.CloseTime))
				TrySetSellStopLevel(candle.CloseTime);
		}
	}

	private void DetectFractals()
	{
		var highs = _highBuffer.ToArray();
		var lows = _lowBuffer.ToArray();

		decimal? upFractal = null;
		decimal? downFractal = null;

		if (highs[2] > highs[0] && highs[2] > highs[1] && highs[2] > highs[3] && highs[2] > highs[4])
			upFractal = highs[2];

		if (lows[2] < lows[0] && lows[2] < lows[1] && lows[2] < lows[3] && lows[2] < lows[4])
			downFractal = lows[2];

		if (upFractal is decimal up && !AreEqual(_lastUpFractal, up))
		{
			_lastUpFractal = up;
			_upOld = _upMiddle;
			_upMiddle = _upYoungest;
			_upYoungest = up;
		}

		if (downFractal is decimal down && !AreEqual(_lastDownFractal, down))
		{
			_lastDownFractal = down;
			_downOld = _downMiddle;
			_downMiddle = _downYoungest;
			_downYoungest = down;
		}
	}

	private void CheckProtectiveStops(ICandleMessage candle)
	{
		if (Position > 0 && _longStopLevel.HasValue)
		{
			if (candle.LowPrice <= _longStopLevel.Value)
			{
				SellMarket(Math.Abs(Position));
				_longStopLevel = null;
				_consecutiveLosses++;
				return;
			}
		}

		if (Position < 0 && _shortStopLevel.HasValue)
		{
			if (candle.HighPrice >= _shortStopLevel.Value)
			{
				BuyMarket(Math.Abs(Position));
				_shortStopLevel = null;
				_consecutiveLosses++;
				return;
			}
		}
	}

	private void UpdateTrailingStops()
	{
		if (Position > 0 && _downYoungest.HasValue)
		{
			if (!_longStopLevel.HasValue || _downYoungest.Value > _longStopLevel.Value)
				_longStopLevel = _downYoungest.Value;
		}
		else if (Position <= 0)
		{
			_longStopLevel = null;
		}

		if (Position < 0 && _upYoungest.HasValue)
		{
			if (!_shortStopLevel.HasValue || _upYoungest.Value < _shortStopLevel.Value)
				_shortStopLevel = _upYoungest.Value;
		}
		else if (Position >= 0)
		{
			_shortStopLevel = null;
		}
	}

	private void ValidatePendingLevels(DateTimeOffset currentTime)
	{
		if (_buyStopLevel.HasValue && _upYoungest.HasValue)
		{
			if (_upYoungest.Value < _buyStopLevel.Value && !AreEqual(_upYoungest, _buyStopLevel.Value))
			{
				_buyStopLevel = null;
				_buyStopExpiry = null;
			}
		}

		if (_sellStopLevel.HasValue && _downYoungest.HasValue)
		{
			if (_downYoungest.Value > _sellStopLevel.Value && !AreEqual(_downYoungest, _sellStopLevel.Value))
			{
				_sellStopLevel = null;
				_sellStopExpiry = null;
			}
		}

		if (_buyStopLevel.HasValue && _buyStopExpiry.HasValue && currentTime >= _buyStopExpiry.Value)
		{
			_buyStopLevel = null;
			_buyStopExpiry = null;
		}

		if (_sellStopLevel.HasValue && _sellStopExpiry.HasValue && currentTime >= _sellStopExpiry.Value)
		{
			_sellStopLevel = null;
			_sellStopExpiry = null;
		}

		if (Position != 0)
		{
			_buyStopLevel = null;
			_sellStopLevel = null;
			_buyStopExpiry = null;
			_sellStopExpiry = null;
		}
	}

	private void CheckPendingTriggers(ICandleMessage candle)
	{
		if (_buyStopLevel.HasValue && candle.HighPrice >= _buyStopLevel.Value && Position <= 0)
		{
			var vol = _buyStopVolume > 0m ? _buyStopVolume : Volume;
			if (vol > 0m)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(vol);
				_entryPrice = _buyStopLevel.Value;
				_longStopLevel = _downYoungest;
			}
			_buyStopLevel = null;
			_buyStopExpiry = null;
		}

		if (_sellStopLevel.HasValue && candle.LowPrice <= _sellStopLevel.Value && Position >= 0)
		{
			var vol = _sellStopVolume > 0m ? _sellStopVolume : Volume;
			if (vol > 0m)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(vol);
				_entryPrice = _sellStopLevel.Value;
				_shortStopLevel = _upYoungest;
			}
			_sellStopLevel = null;
			_sellStopExpiry = null;
		}
	}

	private bool TrySetBuyStopLevel(DateTimeOffset time)
	{
		if (Position > 0 || _buyStopLevel.HasValue)
			return false;

		if (_upYoungest is not decimal up || _upMiddle is not decimal middle || _downYoungest is not decimal stop)
			return false;

		if (up <= middle || stop >= up)
			return false;

		var volume = CalculateOrderVolume(up, stop, Sides.Buy);
		if (volume <= 0m)
			return false;

		_buyStopLevel = up;
		_buyStopVolume = volume;
		_buyStopExpiry = ExpirationHours > 0 ? time + TimeSpan.FromHours(ExpirationHours) : null;
		return true;
	}

	private void TrySetSellStopLevel(DateTimeOffset time)
	{
		if (Position < 0 || _sellStopLevel.HasValue)
			return;

		if (_downYoungest is not decimal down || _downMiddle is not decimal middle || _upYoungest is not decimal stop)
			return;

		if (down >= middle || stop <= down)
			return;

		var volume = CalculateOrderVolume(down, stop, Sides.Sell);
		if (volume <= 0m)
			return;

		_sellStopLevel = down;
		_sellStopVolume = volume;
		_sellStopExpiry = ExpirationHours > 0 ? time + TimeSpan.FromHours(ExpirationHours) : null;
	}

	private decimal CalculateOrderVolume(decimal entryPrice, decimal stopPrice, Sides direction)
	{
		var riskPerUnit = direction == Sides.Buy ? entryPrice - stopPrice : stopPrice - entryPrice;
		if (riskPerUnit <= 0m)
			return 0m;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		if (portfolioValue <= 0m)
			portfolioValue = Volume > 0m ? Volume * entryPrice : 0m;

		var riskAmount = portfolioValue * (MaximumRiskPercent / 100m);
		if (riskAmount <= 0m)
			return 0m;

		var volume = riskAmount / riskPerUnit;

		if (DecreaseFactor > 0m && _consecutiveLosses > 1)
			volume -= volume * (_consecutiveLosses / DecreaseFactor);

		if (volume <= 0m)
			return 0m;

		return Math.Max(volume, Volume > 0 ? Volume : 1m);
	}

	private bool AreEqual(decimal? first, decimal second)
	{
		if (first is not decimal value)
			return false;

		var step = Security?.PriceStep ?? 0.00000001m;
		return Math.Abs(value - second) <= step / 2m;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade?.Trade == null) return;
		if (Position != 0m && _entryPrice == 0m)
			_entryPrice = trade.Trade.Price;
		if (Position == 0m)
			_entryPrice = 0m;
	}
}