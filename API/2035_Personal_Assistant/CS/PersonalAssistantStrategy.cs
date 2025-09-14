namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Manual trading helper that displays basic account info.
/// Provides methods for manual buy/sell operations and volume adjustments.
/// </summary>
public class PersonalAssistantStrategy : Strategy
{
	private readonly StrategyParam<int> _id;
	private readonly StrategyParam<bool> _displayLegend;
	private readonly StrategyParam<decimal> _lotVolume;
	private readonly StrategyParam<int> _slippage;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Unique identifier used as a magic number.
	/// </summary>
	public int Id
	{
		get => _id.Value;
		set => _id.Value = value;
	}

	/// <summary>
	/// Enables informational logging.
	/// </summary>
	public bool DisplayLegend
	{
		get => _displayLegend.Value;
		set => _displayLegend.Value = value;
	}

	/// <summary>
	/// Trading volume for manual orders.
	/// </summary>
	public decimal LotVolume
	{
		get => _lotVolume.Value;
		set => _lotVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed slippage in ticks.
	/// </summary>
	public int Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <summary>
	/// Type of candles used for updates.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref=\"PersonalAssistantStrategy\"/>.
	/// </summary>
	public PersonalAssistantStrategy()
	{
		_id = Param(nameof(Id), 3900)
		.SetDisplay(\"ID\", \"Magic number of the strategy\", \"General\");

		_displayLegend = Param(nameof(DisplayLegend), true)
		.SetDisplay(\"Display Legend\", \"Show informational lines in log\", \"General\");

		_lotVolume = Param(nameof(LotVolume), 0.01m)
		.SetDisplay(\"Lot Size\", \"Volume for manual orders\", \"Trading\");

		_slippage = Param(nameof(Slippage), 2)
		.SetDisplay(\"Slippage\", \"Maximum allowed slippage in ticks\", \"Trading\");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay(\"Candle Type\", \"Type of candles to subscribe\", \"Data\");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		Volume = LotVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(OnProcess).Start();
	}

	private void OnProcess(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (DisplayLegend)
		LogInfo(GetStatusLine());
	}

	private string GetStatusLine()
	{
		var spread = Security?.BestAskPrice is decimal ask && Security?.BestBidPrice is decimal bid
		? ask - bid
		: 0m;

		var tickValue = Security?.StepPrice * LotVolume ?? 0m;
		var openPositionCounter = Position != 0 ? 1 : 0;

		return $\"ID={Id}; Symbol={Security?.Id}; OpenPositions={openPositionCounter}; Profit={PnL}; Volume={LotVolume}; TickValue={tickValue}; Spread={spread}\";
	}

	/// <summary>
	/// Open a long position with current volume.
	/// </summary>
	public void ManualBuy()
	{
		BuyMarket(Volume + Math.Abs(Position));
	}

	/// <summary>
	/// Open a short position with current volume.
	/// </summary>
	public void ManualSell()
	{
		SellMarket(Volume + Math.Abs(Position));
	}

	/// <summary>
	/// Close any open position.
	/// </summary>
	public void CloseAllPositions()
	{
		if (Position > 0)
		SellMarket(Position);
		else if (Position < 0)
		BuyMarket(-Position);
	}

	/// <summary>
	/// Increase trading volume by 0.01.
	/// </summary>
	public void IncreaseLot()
	{
		LotVolume += 0.01m;
		Volume = LotVolume;
	}

	/// <summary>
	/// Decrease trading volume by 0.01 if possible.
	/// </summary>
	public void DecreaseLot()
	{
		if (LotVolume <= 0.01m)
		return;

		LotVolume -= 0.01m;
		Volume = LotVolume;
	}
}
