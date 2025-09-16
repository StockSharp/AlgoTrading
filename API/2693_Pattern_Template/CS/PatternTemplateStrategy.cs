using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pattern template strategy that mirrors the abstract MQL5 example.
/// The strategy demonstrates how to separate responsibilities across
/// money management, signal generation, trade approval, and position support.
/// </summary>
public class PatternTemplateStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _lotVolume;

	private IMoneyManagement _moneyManagement = default!;
	private ISignalGenerator _signalGenerator = default!;
	private ITradeRequestHandler _tradeRequestHandler = default!;
	private IPositionSupport _positionSupport = default!;

	/// <summary>
	/// Parameter exposing the candle type used to drive the template workflow.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Parameter that defines the base volume returned by the money management block.
	/// </summary>
	public decimal LotVolume
	{
		get => _lotVolume.Value;
		set => _lotVolume.Value = value;
	}

	/// <summary>
	/// Initializes parameters that can be tuned from the user interface or optimizer.
	/// </summary>
	public PatternTemplateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for driving the template", "General");

		_lotVolume = Param(nameof(LotVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Volume", "Volume returned by the money management component", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);
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

		LogInfo("Pattern template strategy reset.");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		LogInfo("Pattern template strategy initialized.");

		InitializeComponents();

		Volume = LotVolume;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		LogInfo("Pattern template strategy deinitialized.");

		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		LogInfo("Trade event propagated through the template.");

		_positionSupport.MaintainPosition();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		LogInfo($"Candle completed. Time={candle.OpenTime:O}, Close={candle.ClosePrice}.");

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = _moneyManagement.GetVolume();
		var direction = _signalGenerator.GenerateSignal();
		var approved = _tradeRequestHandler.TryHandle(volume, direction);

		LogInfo($"Template iteration -> Approved={approved}, Volume={volume}, Buy={direction.Buy}, Sell={direction.Sell}.");

		_positionSupport.MaintainPosition();
	}

	private void InitializeComponents()
	{
		_moneyManagement = new FixedVolumeMoneyManagement(this);
		_signalGenerator = new TemplateSignalGenerator(this);
		_tradeRequestHandler = new LoggingTradeRequestHandler(this);
		_positionSupport = new LoggingPositionSupport(this);
	}

	/// <summary>
	/// Simple container describing a direction suggestion from the signal generator.
	/// </summary>
	private readonly struct TradeDirection
	{
		public TradeDirection(bool buy, bool sell)
		{
			Buy = buy;
			Sell = sell;
		}

		public bool Buy { get; }
		public bool Sell { get; }
	}

	/// <summary>
	/// Contract for money management components.
	/// </summary>
	private interface IMoneyManagement
	{
		decimal GetVolume();
	}

	/// <summary>
	/// Contract for generating trade direction suggestions.
	/// </summary>
	private interface ISignalGenerator
	{
		TradeDirection GenerateSignal();
	}

	/// <summary>
	/// Contract for trade request validation.
	/// </summary>
	private interface ITradeRequestHandler
	{
		bool TryHandle(decimal volume, TradeDirection direction);
	}

	/// <summary>
	/// Contract for position maintenance logic.
	/// </summary>
	private interface IPositionSupport
	{
		void MaintainPosition();
	}

	/// <summary>
	/// Basic money management block that returns a fixed volume.
	/// </summary>
	private sealed class FixedVolumeMoneyManagement : IMoneyManagement
	{
		private readonly PatternTemplateStrategy _owner;

		public FixedVolumeMoneyManagement(PatternTemplateStrategy owner)
		{
			_owner = owner;
			_owner.LogInfo("Money management component created.");
		}

		public decimal GetVolume()
		{
			_owner.LogInfo("Money management returning configured volume.");
			return _owner.LotVolume;
		}
	}

	/// <summary>
	/// Signal generator that alternates between buy and sell suggestions.
	/// </summary>
	private sealed class TemplateSignalGenerator : ISignalGenerator
	{
		private readonly PatternTemplateStrategy _owner;
		private bool _suggestBuy = true;

		public TemplateSignalGenerator(PatternTemplateStrategy owner)
		{
			_owner = owner;
			_owner.LogInfo("Signal generator component created.");
		}

		public TradeDirection GenerateSignal()
		{
			_owner.LogInfo("Signal generator evaluating template state.");
			var direction = new TradeDirection(_suggestBuy, !_suggestBuy);
			_suggestBuy = !_suggestBuy;
			return direction;
		}
	}

	/// <summary>
	/// Trade request handler that accepts every request and logs the activity.
	/// </summary>
	private sealed class LoggingTradeRequestHandler : ITradeRequestHandler
	{
		private readonly PatternTemplateStrategy _owner;

		public LoggingTradeRequestHandler(PatternTemplateStrategy owner)
		{
			_owner = owner;
			_owner.LogInfo("Trade request component created.");
		}

		public bool TryHandle(decimal volume, TradeDirection direction)
		{
			_owner.LogInfo($"Trade request received. Volume={volume}, Buy={direction.Buy}, Sell={direction.Sell}.");
			return true;
		}
	}

	/// <summary>
	/// Position support block that only logs the maintenance calls.
	/// </summary>
	private sealed class LoggingPositionSupport : IPositionSupport
	{
		private readonly PatternTemplateStrategy _owner;

		public LoggingPositionSupport(PatternTemplateStrategy owner)
		{
			_owner = owner;
			_owner.LogInfo("Position support component created.");
		}

		public void MaintainPosition()
		{
			_owner.LogInfo("Position support executed maintenance step.");
		}
	}
}
