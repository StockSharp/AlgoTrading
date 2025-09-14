using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DiNapoli Stochastic cross strategy.
/// Opens a long position when the %K line crosses below %D and
/// a short position when %K crosses above %D.
/// </summary>
public class DiNapoliStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _fastK;
	private readonly StrategyParam<int> _slowK;
	private readonly StrategyParam<int> _slowD;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;

	/// <summary>
	/// Base period for %K calculation.
	/// </summary>
	public int FastK
	{
		get => _fastK.Value;
		set => _fastK.Value = value;
	}

	/// <summary>
	/// Smoothing period for %K.
	/// </summary>
	public int SlowK
	{
		get => _slowK.Value;
		set => _slowK.Value = value;
	}

	/// <summary>
	/// Smoothing period for %D.
	/// </summary>
	public int SlowD
	{
		get => _slowD.Value;
		set => _slowD.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DiNapoliStochasticStrategy"/>.
	/// </summary>
	public DiNapoliStochasticStrategy()
	{
		_fastK = Param(nameof(FastK), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast %K", "Base period for %K", "DiNapoli");

		_slowK = Param(nameof(SlowK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slow %K", "%K smoothing period", "DiNapoli");

		_slowD = Param(nameof(SlowD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slow %D", "%D smoothing period", "DiNapoli");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "General");
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
		_prevK = 0m;
		_prevD = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var stochastic = new StochasticOscillator
		{
			Length = FastK,
			K = { Length = SlowK },
			D = { Length = SlowD },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K;
		var d = stoch.D;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevK = k;
			_prevD = d;
			return;
		}

		if (_prevK > _prevD)
		{
			if (SellClose && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (BuyOpen && k <= d && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevK < _prevD)
		{
			if (BuyClose && Position > 0)
				SellMarket(Math.Abs(Position));

			if (SellOpen && k >= d && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevK = k;
		_prevD = d;
	}
}
