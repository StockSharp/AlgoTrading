namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// SuperTrend strategy adjusted by Vegas channel with multi-step take profits.
/// </summary>
public class MultiStepVegasSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _vegasWindow;
	private readonly StrategyParam<decimal> _superTrendMultiplier;
	private readonly StrategyParam<decimal> _volatilityAdjustment;
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<TradeDirection> _direction;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent1;
	private readonly StrategyParam<decimal> _takeProfitPercent2;
	private readonly StrategyParam<decimal> _takeProfitPercent3;
	private readonly StrategyParam<decimal> _takeProfitPercent4;
	private readonly StrategyParam<decimal> _takeProfitAmount1;
	private readonly StrategyParam<decimal> _takeProfitAmount2;
	private readonly StrategyParam<decimal> _takeProfitAmount3;
	private readonly StrategyParam<decimal> _takeProfitAmount4;
	private readonly StrategyParam<int> _numberOfSteps;

	private IIndicator _vegasMa = null!;
	private StandardDeviation _std = null!;
	private AverageTrueRange _atr = null!;

	private decimal? _prevUpper;
	private decimal? _prevLower;
	private int _trend = 1;

	public enum MaType
	{
		Simple,
		Jurik
	}

	public MultiStepVegasSuperTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "Parameters");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR length", "Parameters");

		_vegasWindow = Param(nameof(VegasWindow), 100)
			.SetGreaterThanZero()
			.SetDisplay("Vegas Window", "Vegas moving average length", "Parameters");

		_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Base Multiplier", "SuperTrend base multiplier", "Parameters");

		_volatilityAdjustment = Param(nameof(VolatilityAdjustment), 5m)
			.SetDisplay("Volatility Adjustment", "Multiplier adjustment factor", "Parameters");

		_maType = Param(nameof(MaType), MaType.Simple)
			.SetDisplay("MA Type", "Vegas moving average type", "Parameters");

		_direction = Param(nameof(Direction), TradeDirection.Both)
			.SetDisplay("Direction", "Trade direction", "Parameters");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable take profit", "Take Profit");

		_takeProfitPercent1 = Param(nameof(TakeProfitPercent1), 3m)
			.SetDisplay("TP % Step 1", "Take profit percent step 1", "Take Profit");
		_takeProfitPercent2 = Param(nameof(TakeProfitPercent2), 6m)
			.SetDisplay("TP % Step 2", "Take profit percent step 2", "Take Profit");
		_takeProfitPercent3 = Param(nameof(TakeProfitPercent3), 12m)
			.SetDisplay("TP % Step 3", "Take profit percent step 3", "Take Profit");
		_takeProfitPercent4 = Param(nameof(TakeProfitPercent4), 21m)
			.SetDisplay("TP % Step 4", "Take profit percent step 4", "Take Profit");

		_takeProfitAmount1 = Param(nameof(TakeProfitAmount1), 25m)
			.SetDisplay("TP Amount 1", "Qty percent step 1", "Take Profit");
		_takeProfitAmount2 = Param(nameof(TakeProfitAmount2), 20m)
			.SetDisplay("TP Amount 2", "Qty percent step 2", "Take Profit");
		_takeProfitAmount3 = Param(nameof(TakeProfitAmount3), 10m)
			.SetDisplay("TP Amount 3", "Qty percent step 3", "Take Profit");
		_takeProfitAmount4 = Param(nameof(TakeProfitAmount4), 15m)
			.SetDisplay("TP Amount 4", "Qty percent step 4", "Take Profit");

		_numberOfSteps = Param(nameof(NumberOfSteps), 4)
			.SetGreaterThanZero()
			.SetDisplay("Steps", "Number of take profit steps", "Take Profit");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public int VegasWindow { get => _vegasWindow.Value; set => _vegasWindow.Value = value; }
	public decimal SuperTrendMultiplier { get => _superTrendMultiplier.Value; set => _superTrendMultiplier.Value = value; }
	public decimal VolatilityAdjustment { get => _volatilityAdjustment.Value; set => _volatilityAdjustment.Value = value; }
	public MaType MaType { get => _maType.Value; set => _maType.Value = value; }
	public TradeDirection Direction { get => _direction.Value; set => _direction.Value = value; }
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }
	public decimal TakeProfitPercent1 { get => _takeProfitPercent1.Value; set => _takeProfitPercent1.Value = value; }
	public decimal TakeProfitPercent2 { get => _takeProfitPercent2.Value; set => _takeProfitPercent2.Value = value; }
	public decimal TakeProfitPercent3 { get => _takeProfitPercent3.Value; set => _takeProfitPercent3.Value = value; }
	public decimal TakeProfitPercent4 { get => _takeProfitPercent4.Value; set => _takeProfitPercent4.Value = value; }
	public decimal TakeProfitAmount1 { get => _takeProfitAmount1.Value; set => _takeProfitAmount1.Value = value; }
	public decimal TakeProfitAmount2 { get => _takeProfitAmount2.Value; set => _takeProfitAmount2.Value = value; }
	public decimal TakeProfitAmount3 { get => _takeProfitAmount3.Value; set => _takeProfitAmount3.Value = value; }
	public decimal TakeProfitAmount4 { get => _takeProfitAmount4.Value; set => _takeProfitAmount4.Value = value; }
	public int NumberOfSteps { get => _numberOfSteps.Value; set => _numberOfSteps.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_vegasMa = CreateMa(MaType, VegasWindow);
		_std = new StandardDeviation { Length = VegasWindow };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(_vegasMa, _std, _atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stdValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_vegasMa.IsFormed || !_std.IsFormed || !_atr.IsFormed)
			return;

		var upper = maValue + stdValue;
		var lower = maValue - stdValue;
		var width = upper - lower;
		var adjMult = SuperTrendMultiplier + (maValue != 0m ? VolatilityAdjustment * (width / maValue) : 0m);
		var hlc3 = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var stUpper = hlc3 - adjMult * atrValue;
		var stLower = hlc3 + adjMult * atrValue;

		_prevUpper ??= stUpper;
		_prevLower ??= stLower;

		var trend = candle.ClosePrice > _prevLower ? 1 : candle.ClosePrice < _prevUpper ? -1 : _trend;

		stUpper = trend == 1 ? Math.Max(stUpper, _prevUpper.Value) : stUpper;
		stLower = trend == -1 ? Math.Min(stLower, _prevLower.Value) : stLower;

		var prevTrend = _trend;
		_prevUpper = stUpper;
		_prevLower = stLower;
		_trend = trend;

		if (prevTrend == _trend)
			return;

		CancelActiveOrders();

		var volume = Volume + Math.Abs(Position);

		if (_trend == 1)
		{
			if (Direction == TradeDirection.Short)
			{
				if (Position != 0)
					ClosePosition();
			}
			else
			{
				BuyMarket(volume);
				SetTakeProfits(true, candle.ClosePrice, volume);
			}
		}
		else if (_trend == -1)
		{
			if (Direction == TradeDirection.Long)
			{
				if (Position != 0)
					ClosePosition();
			}
			else
			{
				SellMarket(volume);
				SetTakeProfits(false, candle.ClosePrice, volume);
			}
		}
	}

	private void SetTakeProfits(bool isLong, decimal entryPrice, decimal volume)
	{
		if (!UseTakeProfit)
			return;

		void Place(int step, decimal pricePercent, decimal qtyPercent)
		{
			if (NumberOfSteps >= step && qtyPercent > 0m)
			{
				var qty = volume * qtyPercent / 100m;
				var price = isLong ? entryPrice * (1m + pricePercent / 100m) : entryPrice * (1m - pricePercent / 100m);
				if (isLong)
					SellLimit(qty, price);
				else
					BuyLimit(qty, price);
			}
		}

		Place(1, TakeProfitPercent1, TakeProfitAmount1);
		Place(2, TakeProfitPercent2, TakeProfitAmount2);
		Place(3, TakeProfitPercent3, TakeProfitAmount3);
		Place(4, TakeProfitPercent4, TakeProfitAmount4);
	}

	private static IIndicator CreateMa(MaType type, int length)
	{
		return type switch
		{
			MaType.Jurik => new JurikMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}
}
