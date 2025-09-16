using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alternating long/short strategy converted from the original MT45 MQL expert.
/// </summary>
public class MT45Strategy : Strategy
{
	private readonly StrategyParam<decimal> _stopPoints;
	private readonly StrategyParam<decimal> _takePoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<DataType> _candleType;

	private Sides _nextSide;
	private Sides? _pendingSide;
	private bool _entryPending;
	private decimal _entryPrice;
	private decimal _lastTradeVolume;
	private decimal _nextVolume;
	private decimal _prevPosition;
	private decimal _pointValue;

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopPoints
	{
		get => _stopPoints.Value;
		set => _stopPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakePoints
	{
		get => _takePoints.Value;
		set => _takePoints.Value = value;
	}

	/// <summary>
	/// Base trading volume used after profitable trades.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the next volume after a losing trade.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Maximum allowed trading volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Candle type used to detect new bars.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MT45Strategy()
	{
		_stopPoints = Param(nameof(StopPoints), 600m)
			.SetDisplay("Stop Points", "Distance to stop loss measured in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1500m, 50m);

		_takePoints = Param(nameof(TakePoints), 700m)
			.SetDisplay("Take Points", "Distance to take profit measured in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 2000m, 50m);

		_baseVolume = Param(nameof(BaseVolume), 0.01m)
			.SetDisplay("Base Volume", "Initial trade volume used by the strategy", "Trading");

		_multiplier = Param(nameof(MartingaleMultiplier), 2m)
			.SetDisplay("Martingale Multiplier", "Volume multiplier applied after a losing trade", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_maxVolume = Param(nameof(MaxVolume), 10m)
			.SetDisplay("Max Volume", "Upper limit for martingale scaling", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used to trigger new trades", "General");

		_nextSide = Sides.Buy;
		_nextVolume = BaseVolume;
		_lastTradeVolume = BaseVolume;
		Volume = BaseVolume;
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

		_nextSide = Sides.Buy;
		_pendingSide = null;
		_entryPending = false;
		_entryPrice = 0m;
		_lastTradeVolume = BaseVolume;
		_nextVolume = BaseVolume;
		_prevPosition = 0m;
		_pointValue = 0m;
		Volume = BaseVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 1m;
		Volume = BaseVolume;

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

		StartProtection(
			takeProfit: CreatePriceUnit(TakePoints),
			stopLoss: CreatePriceUnit(StopPoints));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_entryPending || Position != 0)
			return;

		var volume = _nextVolume;
		if (volume <= 0m)
			return;

		var side = _nextSide;
		_pendingSide = side;

		// Alternate between long and short trades every finished bar.
		if (side == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_entryPending = true;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade == null)
			return;

		var newPosition = Position;
		var previousPosition = _prevPosition;
		_prevPosition = newPosition;

		if (previousPosition == 0m && newPosition != 0m)
		{
			// Store entry price and volume for later profit calculation.
			_entryPrice = trade.Trade.Price;
			_lastTradeVolume = Math.Abs(newPosition);
			_entryPending = false;

			if (_pendingSide.HasValue)
			{
				_nextSide = Opposite(_pendingSide.Value);
				_pendingSide = null;
			}
		}
		else if (previousPosition != 0m && newPosition == 0m)
		{
			// Position closed: evaluate the result and adjust the next volume.
			var direction = previousPosition > 0m ? Sides.Buy : Sides.Sell;
			UpdateNextVolume(direction, trade.Trade.Price, Math.Abs(previousPosition));
			_entryPrice = 0m;
			_entryPending = false;
		}
	}

	private void UpdateNextVolume(Sides direction, decimal exitPrice, decimal volume)
	{
		if (volume <= 0m)
			return;

		var profit = direction == Sides.Buy
			? (exitPrice - _entryPrice) * volume
			: (_entryPrice - exitPrice) * volume;

		if (profit < 0m)
		{
			var scaled = _lastTradeVolume * MartingaleMultiplier;
			_nextVolume = scaled > MaxVolume ? BaseVolume : scaled;
		}
		else
		{
			_nextVolume = BaseVolume;
		}

		Volume = _nextVolume;
	}

	private Unit CreatePriceUnit(decimal points)
	{
		if (points <= 0m || _pointValue <= 0m)
			return default;

		return new Unit(points * _pointValue, UnitTypes.Price);
	}

	private static Sides Opposite(Sides side)
	{
		return side == Sides.Buy ? Sides.Sell : Sides.Buy;
	}
}
