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

using StockSharp.Algo;
using StockSharp.Algo.Candles;

public class EmaCrossTrailingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _slowEma = null!;
	private EMA _fastEma = null!;
	private decimal _pointValue;
	private bool _hasInitialDirection;
	private int _currentDirection;
	private bool _entryPending;

	public EmaCrossTrailingStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Take profit (pips)", "Distance in pips used for the protective take profit.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 200m, 5m);

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Stop loss (pips)", "Distance in pips used for the protective stop loss.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 300m, 10m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 50m)
			.SetNotNegative()
			.SetDisplay("Trailing stop (pips)", "Trailing distance in pips. Zero disables the trailing behaviour.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 10m);

		_orderVolume = Param(nameof(OrderVolume), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Base order size expressed in lots.", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 10m, 0.1m);

		_fastEmaLength = Param(nameof(FastEmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length of the fast exponential moving average.", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 30, 1);

		_slowEmaLength = Param(nameof(SlowEmaLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length of the slow exponential moving average.", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Time frame used to build candles and EMAs.", "General");
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;

		if (security != null)
			yield return (security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_slowEma = null!;
		_fastEma = null!;
		_pointValue = 1m;
		_hasInitialDirection = false;
		_currentDirection = 0;
		_entryPending = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 1m;
		if (_pointValue <= 0m)
			_pointValue = 1m;

		Volume = AlignVolume(OrderVolume);

		var takeProfitUnit = TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pointValue, UnitTypes.Absolute) : null;
		var stopLossUnit = StopLossPips > 0m ? new Unit(StopLossPips * _pointValue, UnitTypes.Absolute) : null;
		var trailingUnit = TrailingStopPips > 0m ? new Unit(TrailingStopPips * _pointValue, UnitTypes.Absolute) : null;

		StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: stopLossUnit,
			trailingStop: trailingUnit,
			useMarketOrders: true);

		_slowEma = new EMA { Length = SlowEmaLength };
		_fastEma = new EMA { Length = FastEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_slowEma, _fastEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _fastEma);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderRegisterFailed(OrderFail fail, bool calcRisk)
	{
		base.OnOrderRegisterFailed(fail, calcRisk);

		_entryPending = false;
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		// Any filled entry or exit frees the strategy for the next signal.
		_entryPending = false;
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowEmaValue, decimal fastEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var newDirection = _currentDirection;

		if (slowEmaValue > fastEmaValue)
			newDirection = 1;
		else if (slowEmaValue < fastEmaValue)
			newDirection = 2;

		if (!_slowEma.IsFormed || !_fastEma.IsFormed)
		{
			_currentDirection = newDirection;
			return;
		}

		if (!_hasInitialDirection)
		{
			_currentDirection = newDirection;
			_hasInitialDirection = true;
			return;
		}

		if (newDirection == 0 || newDirection == _currentDirection)
			return;

		_currentDirection = newDirection;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m || _entryPending)
			return;

		var volume = AlignVolume(OrderVolume);
		if (volume <= 0m)
			return;

		Volume = volume;
		_entryPending = true;

		if (newDirection == 1)
		{
			// Slow EMA moved above the fast EMA, mirroring the MetaTrader rule for bullish entries.
			BuyMarket(volume);
		}
		else if (newDirection == 2)
		{
			// Slow EMA dropped below the fast EMA, mirroring the MetaTrader rule for bearish entries.
			SellMarket(volume);
		}
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume.Value;

		return volume;
	}
}
