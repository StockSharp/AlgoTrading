namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class FlexibleMovingAverageStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _maMethod;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _sellPercentage;
	private readonly StrategyParam<bool> _allowInitialBuy;

	private decimal? _previousClose;
	private decimal? _previousMa;
	private decimal? _priorClose;
	private decimal? _priorMa;
	private decimal _currentTargetPercent = 100m;
	private bool _isFirstBar = true;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public string MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public decimal SellPercentage
	{
		get => _sellPercentage.Value;
		set => _sellPercentage.Value = value;
	}

	public bool AllowInitialBuy
	{
		get => _allowInitialBuy.Value;
		set => _allowInitialBuy.Value = value;
	}

	public FlexibleMovingAverageStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maMethod = Param(nameof(MaMethod), "SMA")
			.SetDisplay("MA Method", "Moving average calculation method", "MA");

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period length", "MA");

		_sellPercentage = Param(nameof(SellPercentage), 100m)
			.SetRange(0m, 100m)
			.SetDisplay("Sell %", "Percentage of position to sell on cross down", "Risk");

		_allowInitialBuy = Param(nameof(AllowInitialBuy), true)
			.SetDisplay("Allow Initial Buy", "Enter initial position without MA data", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousClose = null;
		_previousMa = null;
		_priorClose = null;
		_priorMa = null;
		_currentTargetPercent = 100m;
		_isFirstBar = true;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		IIndicator ma = MaMethod switch
		{
			"EMA" => new ExponentialMovingAverage { Length = MaLength },
			"WMA" => new WeightedMovingAverage { Length = MaLength },
			"HMA" => new HullMovingAverage { Length = MaLength },
			"SMMA" => new ModifiedMovingAverage { Length = MaLength },
			_ => new SimpleMovingAverage { Length = MaLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ma, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirstBar)
		{
			if (AllowInitialBuy)
				BuyMarket();

			_previousClose = candle.ClosePrice;
			_previousMa = maValue;
			_isFirstBar = false;
			return;
		}

		if (_priorClose is null || _priorMa is null)
		{
			_priorClose = _previousClose;
			_priorMa = _previousMa;
			_previousClose = candle.ClosePrice;
			_previousMa = maValue;
			return;
		}

		var sellSignal = _priorClose >= _priorMa && _previousClose < _previousMa;
		var buySignal = _priorClose <= _priorMa && _previousClose > _previousMa;

		var targetPercent = _currentTargetPercent;

		if (sellSignal)
			targetPercent = 100m - SellPercentage;
		else if (buySignal)
			targetPercent = 100m;

		if (targetPercent != _currentTargetPercent)
		{
			var targetVolume = targetPercent / 100m;
			var diff = targetVolume - Position;

			if (diff > 0)
				BuyMarket(diff);
			else if (diff < 0)
				SellMarket(-diff);

			_currentTargetPercent = targetPercent;
		}

		_priorClose = _previousClose;
		_priorMa = _previousMa;
		_previousClose = candle.ClosePrice;
		_previousMa = maValue;
	}
}

