using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Corrected Average breakout.
/// Monitors price relative to a corrected moving average and trades on breakouts.
/// </summary>
public class CorrectedAverageBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _levelPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private decimal _prevCorrected;
	private decimal _prevPrevCorrected;
	private decimal _prevClose;
	private decimal _prevPrevClose;
	private bool _isInitialized;
	private decimal _level;
	private readonly StrategyParam<bool> _enableLong, _enableShort;
	private Order _pending;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public int LevelPoints { get => _levelPoints.Value; set => _levelPoints.Value = value; }
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public int TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

	public CorrectedAverageBreakoutStrategy()
	{
		_enableLong = Param(nameof(EnableLong), true);
		_enableShort = Param(nameof(EnableShort), true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_length = Param(nameof(Length), 12)
			.SetDisplay("Length", "Period of moving average", "Indicator");

		_levelPoints = Param(nameof(LevelPoints), 300)
			.SetDisplay("Level Points", "Breakout distance in price steps", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop Loss Points", "Stop loss in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetDisplay("Take Profit Points", "Take profit in price steps", "Risk");
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
		_prevCorrected = default;
		_prevPrevCorrected = default;
		_prevClose = default;
		_prevPrevClose = default;
		_isInitialized = default;
		_level = default;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_pending = null;

		var step = Security.PriceStep ?? 1m;
		_level = LevelPoints * step;

		var ma = new ExponentialMovingAverage { Length = Length };
		var std = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ma, std, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPoints * step, UnitTypes.Absolute), useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		decimal corrected;

		if (!_isInitialized)
		{
			corrected = maValue;
			_isInitialized = true;
		}
		else
		{
			var v1 = stdValue * stdValue;
			var v2 = (_prevCorrected - maValue) * (_prevCorrected - maValue);
			var k = (v2 < v1 || v2 == 0m) ? 0m : 1m - (v1 / v2);
			corrected = _prevCorrected + k * (maValue - _prevCorrected);
		}

		// Previous *finished* bar confirmed the breakout. The current finished
		// bar must revisit its price before placing the corresponding market order.
		var buyLevel = _prevCorrected + _level;
		var sellLevel = _prevCorrected - _level;
		var buySignal = _prevClose > buyLevel && candle.LowPrice <= buyLevel && candle.ClosePrice >= buyLevel;
		var sellSignal = _prevClose < sellLevel && candle.HighPrice >= sellLevel && candle.ClosePrice <= sellLevel;
		if (_pending is null || _pending.State is OrderStates.Done or OrderStates.Failed)
		{
			if (buySignal && Position <= 0m)
			{
				if (Position < 0m || EnableLong)
					_pending = BuyMarket(Math.Abs(Math.Min(Position, 0m)) + (EnableLong ? Volume : 0m));
			}
			else if (sellSignal && Position >= 0m)
			{
				if (Position > 0m || EnableShort)
					_pending = SellMarket(Math.Max(0m, Position) + (EnableShort ? Volume : 0m));
			}
		}

		_prevPrevCorrected = _prevCorrected;
		_prevPrevClose = _prevClose;
		_prevCorrected = corrected;
		_prevClose = candle.ClosePrice;
	}
}
