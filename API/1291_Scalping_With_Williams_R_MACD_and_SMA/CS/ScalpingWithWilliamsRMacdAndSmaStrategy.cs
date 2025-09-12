using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scalping strategy using Williams %R, MACD histogram and SMA trend filter.
/// </summary>
public class ScalpingWithWilliamsRMacdAndSmaStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsLength;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _buyActivation;
	private readonly StrategyParam<decimal> _buyDeactivation;
	private readonly StrategyParam<decimal> _sellActivation;
	private readonly StrategyParam<decimal> _sellDeactivation;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevWilliams;
	private decimal _prevHist;
	private bool _buyActive;
	private bool _sellActive;
	private bool _isFirst = true;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WilliamsLength
	{
		get => _williamsLength.Value;
		set => _williamsLength.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Williams %R level to activate buying.
	/// </summary>
	public decimal BuyActivation
	{
		get => _buyActivation.Value;
		set => _buyActivation.Value = value;
	}

	/// <summary>
	/// Williams %R level to deactivate buying.
	/// </summary>
	public decimal BuyDeactivation
	{
		get => _buyDeactivation.Value;
		set => _buyDeactivation.Value = value;
	}

	/// <summary>
	/// Williams %R level to activate selling.
	/// </summary>
	public decimal SellActivation
	{
		get => _sellActivation.Value;
		set => _sellActivation.Value = value;
	}

	/// <summary>
	/// Williams %R level to deactivate selling.
	/// </summary>
	public decimal SellDeactivation
	{
		get => _sellDeactivation.Value;
		set => _sellDeactivation.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ScalpingWithWilliamsRMacdAndSmaStrategy"/>.
	/// </summary>
	public ScalpingWithWilliamsRMacdAndSmaStrategy()
	{
		_williamsLength = Param(nameof(WilliamsLength), 140)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Lookback for Williams %R", "Indicators");

		_macdFast = Param(nameof(MacdFast), 24)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 52)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period", "Indicators");

		_smaLength = Param(nameof(SmaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period for trend SMA", "Indicators");

		_buyActivation = Param(nameof(BuyActivation), -94m)
			.SetDisplay("Buy Activation", "Williams %R cross level to activate buys", "Williams %R");

		_buyDeactivation = Param(nameof(BuyDeactivation), -40m)
			.SetDisplay("Buy Deactivation", "Williams %R cross level to deactivate buys", "Williams %R");

		_sellActivation = Param(nameof(SellActivation), -6m)
			.SetDisplay("Sell Activation", "Williams %R cross level to activate sells", "Williams %R");

		_sellDeactivation = Param(nameof(SellDeactivation), -60m)
			.SetDisplay("Sell Deactivation", "Williams %R cross level to deactivate sells", "Williams %R");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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
		_prevWilliams = 0m;
		_prevHist = 0m;
		_buyActive = false;
		_sellActive = false;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var williams = new WilliamsR { Length = WilliamsLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		var sma = new SMA { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, williams, sma, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd, decimal signal, decimal hist, decimal williams, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isFirst)
		{
			_prevHist = hist;
			_prevWilliams = williams;
			_isFirst = false;
			return;
		}

		var isBull = candle.ClosePrice > sma;
		var isBear = candle.ClosePrice < sma;

		if (_prevWilliams < BuyActivation && williams >= BuyActivation && isBull)
			_buyActive = true;

		if (_prevWilliams < BuyDeactivation && williams >= BuyDeactivation)
			_buyActive = false;

		if (_prevWilliams > SellActivation && williams <= SellActivation && isBear)
			_sellActive = true;

		if (_prevWilliams > SellDeactivation && williams <= SellDeactivation)
			_sellActive = false;

		var histPosAfterNeg = _prevHist < 0m && hist > 0m;
		var histNegAfterPos = _prevHist > 0m && hist < 0m;

		if (_buyActive && histPosAfterNeg && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}

		if (_sellActive && histNegAfterPos && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		if (Position > 0 && hist < _prevHist)
			SellMarket(Position);
		else if (Position < 0 && hist > _prevHist)
			BuyMarket(Math.Abs(Position));

		_prevHist = hist;
		_prevWilliams = williams;
	}
}
