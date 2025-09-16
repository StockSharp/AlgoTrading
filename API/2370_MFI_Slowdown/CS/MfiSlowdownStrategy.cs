using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Money Flow Index strategy that reacts to extreme values and optional slowdown.
/// Closes opposite positions and optionally opens new ones on signals.
/// Uses StartProtection for stop-loss and take-profit.
/// </summary>
public class MfiSlowdownStrategy : Strategy
{
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<bool> _seekSlowdown;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMfi;

	/// <summary>
	/// Period for MFI calculation.
	/// </summary>
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }

	/// <summary>
	/// Upper threshold for MFI.
	/// </summary>
	public decimal UpperThreshold { get => _upperThreshold.Value; set => _upperThreshold.Value = value; }

	/// <summary>
	/// Lower threshold for MFI.
	/// </summary>
	public decimal LowerThreshold { get => _lowerThreshold.Value; set => _lowerThreshold.Value = value; }

	/// <summary>
	/// Require MFI slowdown before signaling.
	/// </summary>
	public bool SeekSlowdown { get => _seekSlowdown.Value; set => _seekSlowdown.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	/// <summary>
	/// Take-profit percentage.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MfiSlowdownStrategy"/> class.
	/// </summary>
	public MfiSlowdownStrategy()
	{
		_mfiPeriod = Param(nameof(MfiPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Period for the MFI indicator", "Indicator");
		_upperThreshold = Param(nameof(UpperThreshold), 90m)
			.SetRange(0m, 100m)
			.SetDisplay("Upper Threshold", "MFI upper level", "Signal");
		_lowerThreshold = Param(nameof(LowerThreshold), 10m)
			.SetRange(0m, 100m)
			.SetDisplay("Lower Threshold", "MFI lower level", "Signal");
		_seekSlowdown = Param(nameof(SeekSlowdown), true)
			.SetDisplay("Seek Slowdown", "Require MFI to slow down", "Signal");
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Open Long", "Allow opening long positions", "Trading");
		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Long", "Allow closing long positions", "Trading");
		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Open Short", "Allow opening short positions", "Trading");
		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Short", "Allow closing short positions", "Trading");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetRange(0m, 10m)
			.SetDisplay("Take Profit %", "Take-profit percentage", "Risk");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetRange(0m, 10m)
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevMfi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var mfi = new MoneyFlowIndex { Length = MfiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(mfi, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mfi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var slowdown = _prevMfi.HasValue && Math.Abs(mfiValue - _prevMfi.Value) < 1m;

		var upSignal = mfiValue >= UpperThreshold && (!SeekSlowdown || slowdown);
		var downSignal = mfiValue <= LowerThreshold && (!SeekSlowdown || slowdown);

		if (upSignal)
		{
			if (SellPosClose && Position < 0)
				BuyMarket();
			if (BuyPosOpen && Position <= 0)
				BuyMarket();
		}
		else if (downSignal)
		{
			if (BuyPosClose && Position > 0)
				SellMarket();
			if (SellPosOpen && Position >= 0)
				SellMarket();
		}

		_prevMfi = mfiValue;
	}
}
