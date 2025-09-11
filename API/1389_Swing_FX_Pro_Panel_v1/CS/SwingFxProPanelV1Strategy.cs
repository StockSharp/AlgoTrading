using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with basic performance statistics.
/// </summary>
public class SwingFxProPanelV1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _initialCapital;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<int> _analysisPeriod;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaFast = null!;
	private EMA _emaSlow = null!;
	private decimal _entryPrice;
	private int _totalTrades;
	private int _winTrades;
	private decimal _maxEquity;
	private decimal _maxDrawdown;

	/// <summary>
	/// Initial capital for statistics.
	/// </summary>
	public decimal InitialCapital { get => _initialCapital.Value; set => _initialCapital.Value = value; }

	/// <summary>
	/// Risk percentage per trade.
	/// </summary>
	public decimal RiskPerTrade { get => _riskPerTrade.Value; set => _riskPerTrade.Value = value; }

	/// <summary>
	/// Analysis period in months.
	/// </summary>
	public int AnalysisPeriod { get => _analysisPeriod.Value; set => _analysisPeriod.Value = value; }

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Profit target in price units.
	/// </summary>
	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Candle type used for trading.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="SwingFxProPanelV1Strategy"/>.
	/// </summary>
	public SwingFxProPanelV1Strategy()
	{
		_initialCapital = Param(nameof(InitialCapital), 1000m)
			.SetDisplay("Initial Capital", "Initial capital for statistics", "General");

		_riskPerTrade = Param(nameof(RiskPerTrade), 2m)
			.SetDisplay("Risk Per Trade %", "Risk percentage per trade", "General");

		_analysisPeriod = Param(nameof(AnalysisPeriod), 6)
			.SetDisplay("Analysis Period", "Months for analysis", "General");

		_fastLength = Param(nameof(FastLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Fast EMA period", "EMA");

		_slowLength = Param(nameof(SlowLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Slow EMA period", "EMA");

		_profitTarget = Param(nameof(ProfitTarget), 300m)
			.SetDisplay("Profit Target", "Profit target in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 150m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_entryPrice = 0m;
		_totalTrades = _winTrades = 0;
		_maxEquity = InitialCapital;
		_maxDrawdown = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_emaFast = new EMA { Length = FastLength };
		_emaSlow = new EMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_emaFast, _emaSlow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (emaFast > emaSlow && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}
		else if (emaFast < emaSlow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}

		if (Position > 0)
		{
			if (candle.HighPrice - _entryPrice >= ProfitTarget || _entryPrice - candle.LowPrice >= StopLoss)
			{
				var isWin = candle.HighPrice - _entryPrice >= ProfitTarget;
				SellMarket(Math.Abs(Position));
				_totalTrades++;
				if (isWin)
					_winTrades++;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - candle.LowPrice >= ProfitTarget || candle.HighPrice - _entryPrice >= StopLoss)
			{
				var isWin = _entryPrice - candle.LowPrice >= ProfitTarget;
				BuyMarket(Math.Abs(Position));
				_totalTrades++;
				if (isWin)
					_winTrades++;
			}
		}

		var equity = InitialCapital + PnL;
		if (equity > _maxEquity)
			_maxEquity = equity;
		var drawdown = _maxEquity - equity;
		if (drawdown > _maxDrawdown)
			_maxDrawdown = drawdown;

		var roi = InitialCapital == 0m ? 0m : PnL / InitialCapital * 100m;
		var winRatio = _totalTrades == 0 ? 0m : (decimal)_winTrades / _totalTrades * 100m;

		LogInfo($"ROI={roi:F2}% Trades={_totalTrades} WinRatio={winRatio:F2}% MaxDD={_maxDrawdown:F2}");
	}
}
