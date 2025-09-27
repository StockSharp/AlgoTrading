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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manages an existing position by trailing the protective stop order using ATR or a fixed distance.
/// </summary>
public class MoveStopLossStrategy : Strategy
{
	private readonly StrategyParam<bool> _autoTrail;
	private readonly StrategyParam<decimal> _manualDistance;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrLookback;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private Highest _atrHighest = null!;
	private Order _stopOrder;
	private decimal _stopPrice;
	private decimal _entryPrice;
	private int _lastPosition;
	private decimal _point;

	/// <summary>
	/// Enables ATR based trailing when set to true.
	/// </summary>
	public bool AutoTrail { get => _autoTrail.Value; set => _autoTrail.Value = value; }

	/// <summary>
	/// Manual trailing distance in price steps used when <see cref="AutoTrail"/> is false.
	/// </summary>
	public decimal ManualDistance { get => _manualDistance.Value; set => _manualDistance.Value = value; }

	/// <summary>
	/// Multiplier applied to the highest ATR value from the lookback window.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Number of ATR values included in the rolling maximum.
	/// </summary>
	public int AtrLookback { get => _atrLookback.Value; set => _atrLookback.Value = value; }

	/// <summary>
	/// Candle type used for trailing calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="MoveStopLossStrategy"/>.
	/// </summary>
	public MoveStopLossStrategy()
	{
		_autoTrail = Param(nameof(AutoTrail), true)
			.SetDisplay("Use ATR", "Enable ATR based trailing", "Trailing");

		_manualDistance = Param(nameof(ManualDistance), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Manual Distance", "Trailing distance in price steps", "Trailing");

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.85m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier applied to the ATR maximum", "Trailing");

		_atrPeriod = Param(nameof(AtrPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Number of candles used by ATR", "Trailing");

		_atrLookback = Param(nameof(AtrLookback), 30)
			.SetGreaterThanZero()
			.SetDisplay("ATR Lookback", "Candles included in the ATR maximum", "Trailing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles processed for trailing logic", "General");
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

		_stopOrder = null;
		_stopPrice = 0m;
		_entryPrice = 0m;
		_lastPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_point = Security?.PriceStep ?? 1m;

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_atrHighest = new Highest { Length = Math.Max(1, AtrLookback) };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_atr, _atrHighest, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue atrHighestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trailDistance = GetTrailDistance(atrValue, atrHighestValue);

		if (trailDistance <= 0m)
			return;

		var price = candle.ClosePrice;

		if (Position > 0)
		{
			HandleLongPosition(price, trailDistance);
		}
		else if (Position < 0)
		{
			HandleShortPosition(price, trailDistance);
		}
		else
		{
			_entryPrice = 0m;
			CancelStop();
		}

		_lastPosition = Position;
	}

	private decimal GetTrailDistance(IIndicatorValue atrValue, IIndicatorValue atrHighestValue)
	{
		if (AutoTrail)
		{
			if (!atrValue.IsFormed || !atrHighestValue.IsFormed)
				return 0m;

			var atrMax = atrHighestValue.ToDecimal();

			if (atrMax <= 0m)
				return 0m;

			return AtrMultiplier * atrMax;
		}

		return ManualDistance > 0m ? ManualDistance * _point : 0m;
	}

	private void HandleLongPosition(decimal price, decimal trailDistance)
	{
		if (_lastPosition <= 0)
		{
			_entryPrice = price;
			CancelStop();
		}

		if (price <= _entryPrice)
			return;

		var newStop = price - trailDistance;

		if (_stopOrder == null || newStop > _stopPrice)
		{
			MoveStop(Sides.Sell, newStop);
		}
	}

	private void HandleShortPosition(decimal price, decimal trailDistance)
	{
		if (_lastPosition >= 0)
		{
			_entryPrice = price;
			CancelStop();
		}

		if (price >= _entryPrice)
			return;

		var newStop = price + trailDistance;

		if (_stopOrder == null || newStop < _stopPrice || _stopPrice == 0m)
		{
			MoveStop(Sides.Buy, newStop);
		}
	}

	private void MoveStop(Sides side, decimal price)
	{
		CancelStop();

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		_stopOrder = side == Sides.Sell
			? SellStop(volume, price)
			: BuyStop(volume, price);

		_stopPrice = price;
	}

	private void CancelStop()
	{
		if (_stopOrder == null)
			return;

		CancelOrder(_stopOrder);
		_stopOrder = null;
	}
}

