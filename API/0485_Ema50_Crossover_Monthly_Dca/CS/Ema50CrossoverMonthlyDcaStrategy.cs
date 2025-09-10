namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 50 EMA crossover strategy with monthly DCA.
/// </summary>
public class Ema50CrossoverMonthlyDcaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _dcaAmount;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;

	private decimal _cashReserve;
	private DateTimeOffset? _lastInvestmentTime;

	private static readonly TimeSpan MonthInterval = TimeSpan.FromDays(30);

	/// <summary>
	/// Monthly DCA investment amount.
	/// </summary>
	public decimal DcaAmount
	{
		get => _dcaAmount.Value;
		set => _dcaAmount.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Start date for investing.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Ema50CrossoverMonthlyDcaStrategy"/>.
	/// </summary>
	public Ema50CrossoverMonthlyDcaStrategy()
	{
		_dcaAmount = Param(nameof(DcaAmount), 100000m)
			.SetGreaterThanZero()
			.SetDisplay("DCA Amount", "Monthly DCA investment amount", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(7).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Date to start investing", "General");
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
		_cashReserve = 0;
		_lastInvestmentTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = 50 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isAfterStart = candle.OpenTime >= StartDate;

		var sinceLast = _lastInvestmentTime == null
			? MonthInterval
			: candle.OpenTime - _lastInvestmentTime.Value;

		var price = candle.ClosePrice;

		var longCondition = price > emaValue && isAfterStart;

		if (longCondition)
		{
			if (Position <= 0)
			{
				var volume = Volume + Math.Abs(Position) + (_cashReserve / price);
				BuyMarket(volume);
				_cashReserve = 0;
			}

			if (sinceLast >= MonthInterval)
			{
				var volume = DcaAmount / price;
				BuyMarket(volume);
				_lastInvestmentTime = candle.OpenTime;
				sinceLast = TimeSpan.Zero;
			}
		}

		if (sinceLast >= MonthInterval && isAfterStart)
		{
			_cashReserve += DcaAmount;
			_lastInvestmentTime = candle.OpenTime;
		}

		if (price < emaValue && Position > 0)
			SellMarket(Math.Abs(Position));
	}
}
