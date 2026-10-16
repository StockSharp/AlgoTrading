namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades confirmed five-bar fractal signals with optional channel, Williams %R
/// and counter-stop filters. The indicator midpoint is not an entry signal.
/// </summary>
public class GraalFractalChannelStrategy : Strategy
{
    private readonly StrategyParam<decimal> _orderVolume;
    private readonly StrategyParam<decimal> _stopLossPips;
    private readonly StrategyParam<decimal> _takeProfitPips;
    private readonly StrategyParam<decimal> _offsetPips;
    private readonly StrategyParam<int> _channelPeriod;
    private readonly StrategyParam<bool> _useFractalChannel;
    private readonly StrategyParam<decimal> _depthPercent;
    private readonly StrategyParam<bool> _useHighLowChannel;
    private readonly StrategyParam<decimal> _orientationPercent;
    private readonly StrategyParam<bool> _allowFlatTrading;
    private readonly StrategyParam<decimal> _flatThresholdPips;
    private readonly StrategyParam<bool> _useWilliamsExit;
    private readonly StrategyParam<int> _williamsPeriod;
    private readonly StrategyParam<decimal> _williamsThreshold;
    private readonly StrategyParam<bool> _useCounterOrders;
    private readonly StrategyParam<bool> _singlePosition;
    private readonly StrategyParam<int> _signalAgeLimit;
    private readonly StrategyParam<DataType> _candleType;

    private readonly Queue<(decimal High, decimal Low)> _bars = new();
    private readonly Queue<decimal> _closes = new();
    private decimal? _upperFractal;
    private decimal? _lowerFractal;
    private Sides? _pendingSide;
    private int _pendingAge;
    private Order _entryOrder;
    private Order _counterOrder;
    private bool _closing;

    public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
    public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
    public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
    public decimal OffsetPips { get => _offsetPips.Value; set => _offsetPips.Value = value; }
    public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
    public bool UseFractalChannel { get => _useFractalChannel.Value; set => _useFractalChannel.Value = value; }
    public decimal DepthPercent { get => _depthPercent.Value; set => _depthPercent.Value = value; }
    public bool UseHighLowChannel { get => _useHighLowChannel.Value; set => _useHighLowChannel.Value = value; }
    public decimal OrientationPercent { get => _orientationPercent.Value; set => _orientationPercent.Value = value; }
    public bool AllowFlatTrading { get => _allowFlatTrading.Value; set => _allowFlatTrading.Value = value; }
    public decimal FlatThresholdPips { get => _flatThresholdPips.Value; set => _flatThresholdPips.Value = value; }
    public bool UseWilliamsExit { get => _useWilliamsExit.Value; set => _useWilliamsExit.Value = value; }
    public int WilliamsPeriod { get => _williamsPeriod.Value; set => _williamsPeriod.Value = value; }
    public decimal WilliamsThreshold { get => _williamsThreshold.Value; set => _williamsThreshold.Value = value; }
    public bool UseCounterOrders { get => _useCounterOrders.Value; set => _useCounterOrders.Value = value; }
    public bool SinglePosition { get => _singlePosition.Value; set => _singlePosition.Value = value; }
    public int SignalAgeLimit { get => _signalAgeLimit.Value; set => _signalAgeLimit.Value = value; }
    public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

    public GraalFractalChannelStrategy()
    {
        _orderVolume = Param(nameof(OrderVolume), 0.1m).SetGreaterThanZero().SetDisplay("Volume", "Entry volume", "Trading");
        _stopLossPips = Param(nameof(StopLossPips), 500m).SetNotNegative().SetDisplay("Stop Loss", "Pip distance", "Protection");
        _takeProfitPips = Param(nameof(TakeProfitPips), 500m).SetNotNegative().SetDisplay("Take Profit", "Pip distance", "Protection");
        _offsetPips = Param(nameof(OffsetPips), 5m).SetNotNegative().SetDisplay("Offset", "Counter stop offset in pips", "Protection");
        _channelPeriod = Param(nameof(ChannelPeriod), 14).SetGreaterThanZero().SetDisplay("Channel Period", "Completed closes", "Indicators");
        _useFractalChannel = Param(nameof(UseFractalChannel), false);
        _depthPercent = Param(nameof(DepthPercent), 25m);
        _useHighLowChannel = Param(nameof(UseHighLowChannel), false);
        _orientationPercent = Param(nameof(OrientationPercent), 20m);
        _allowFlatTrading = Param(nameof(AllowFlatTrading), true);
        _flatThresholdPips = Param(nameof(FlatThresholdPips), 20m);
        _useWilliamsExit = Param(nameof(UseWilliamsExit), false);
        _williamsPeriod = Param(nameof(WilliamsPeriod), 14).SetGreaterThanZero();
        _williamsThreshold = Param(nameof(WilliamsThreshold), 30m);
        _useCounterOrders = Param(nameof(UseCounterOrders), false);
        _singlePosition = Param(nameof(SinglePosition), false);
        _signalAgeLimit = Param(nameof(SignalAgeLimit), 3).SetNotNegative();
        _candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame());
    }

    public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        => [(Security, CandleType)];

    protected override void OnReseted()
    {
        base.OnReseted();
        _bars.Clear();
        _closes.Clear();
        _upperFractal = null;
        _lowerFractal = null;
        _pendingSide = null;
        _pendingAge = 0;
        _entryOrder = null;
        _counterOrder = null;
        _closing = false;
    }

    protected override void OnStarted2(DateTime time)
    {
        base.OnStarted2(time);
        _bars.Clear();
        _closes.Clear();
        _upperFractal = null;
        _lowerFractal = null;
        _pendingSide = null;
        _pendingAge = 0;
        _entryOrder = null;
        _counterOrder = null;
        _closing = false;
        Volume = OrderVolume;

        var pip = PipSize();
        StartProtection(
            takeProfit: TakeProfitPips > 0m ? new Unit(TakeProfitPips * pip, UnitTypes.Absolute) : new Unit(),
            stopLoss: StopLossPips > 0m ? new Unit(StopLossPips * pip, UnitTypes.Absolute) : new Unit(),
            useMarketOrders: true, isLocalStop: true);

        var williams = new WilliamsR { Length = WilliamsPeriod };
        SubscribeCandles(CandleType).Bind(williams, ProcessCandle).Start();
    }

    private decimal PipSize()
    {
        var step = Security?.PriceStep ?? 0m;
        if (step <= 0m)
            throw new InvalidOperationException("Graal Fractal Channel requires a positive instrument price step.");
        return Security?.Decimals is 3 or 5 ? step * 10m : step;
    }

    private decimal NormalizeVolume(decimal volume)
    {
        if (Security?.VolumeStep is decimal step && step > 0m)
            volume = decimal.Floor(volume / step) * step;
        if (Security?.MinVolume is decimal min && min > 0m)
            volume = Math.Max(volume, min);
        if (Security?.MaxVolume is decimal max && max > 0m)
            volume = Math.Min(volume, max);
        return volume;
    }

    private static bool Pending(Order order) => order is not null &&
        order.State is not (OrderStates.Done or OrderStates.Failed);

    private void CancelCounter()
    {
        if (_counterOrder is not null && _counterOrder.State == OrderStates.Active)
            CancelOrder(_counterOrder);
        _counterOrder = null;
    }

    private void ProcessCandle(ICandleMessage candle, decimal williams)
    {
        if (candle.State != CandleStates.Finished)
            return;

        if (_pendingSide.HasValue && ++_pendingAge > SignalAgeLimit)
            _pendingSide = null;

        _bars.Enqueue((candle.HighPrice, candle.LowPrice));
        if (_bars.Count > 5)
            _bars.Dequeue();
        _closes.Enqueue(candle.ClosePrice);
        if (_closes.Count > ChannelPeriod)
            _closes.Dequeue();

        if (_bars.Count == 5)
        {
            var bars = _bars.ToArray();
            var upper = bars.Where((_, i) => i != 2).All(b => bars[2].High > b.High);
            var lower = bars.Where((_, i) => i != 2).All(b => bars[2].Low < b.Low);
            if (upper) _upperFractal = bars[2].High;
            if (lower) _lowerFractal = bars[2].Low;
            // A bar can be both a high and a low fractal: it is not a directional signal.
            if (upper != lower)
            {
                _pendingSide = upper ? Sides.Sell : Sides.Buy;
                _pendingAge = 0;
            }
            else if (upper)
                _pendingSide = null;
        }

        if (!UseCounterOrders || Position == 0m)
            CancelCounter();

        if (_closing)
        {
            if (Position != 0m)
                return;
            _closing = false;
        }

        if (UseWilliamsExit && Position > 0m && williams > -WilliamsThreshold ||
            UseWilliamsExit && Position < 0m && williams < -100m + WilliamsThreshold)
        {
            CancelCounter();
            _pendingSide = null;
            _closing = true;
            ClosePosition();
            return;
        }

        if (!_pendingSide.HasValue || Pending(_entryOrder) || !PassesFilters(candle.ClosePrice, _pendingSide.Value))
            return;

        var side = _pendingSide.Value;
        if (SinglePosition && (side == Sides.Buy ? Position > 0m : Position < 0m))
            return;

        var volume = NormalizeVolume(OrderVolume);
        if (volume <= 0m)
            return;

        // Reverse the existing net position in one order, rather than submitting
        // two uncoordinated default-volume orders.
        if (side == Sides.Buy && Position < 0m || side == Sides.Sell && Position > 0m)
            volume += Math.Abs(Position);

        CancelCounter();
        _entryOrder = new Order
        {
            Security = Security,
            Portfolio = Portfolio,
            Type = OrderTypes.Market,
            Side = side,
            Volume = volume,
            Comment = "Graal fractal entry",
        };
        RegisterOrder(_entryOrder);
        _pendingSide = null;
    }

    private bool PassesFilters(decimal close, Sides side)
    {
        if (_closes.Count < ChannelPeriod)
            return false;

        var highest = _closes.Max();
        var lowest = _closes.Min();
        var width = highest - lowest;
        if (!AllowFlatTrading && width < FlatThresholdPips * PipSize())
            return false;

        if (UseFractalChannel)
        {
            if (_upperFractal is not decimal high || _lowerFractal is not decimal low || high <= low)
                return false;
            var inner = (high - low) * DepthPercent / 100m;
            if (close < low + inner || close > high - inner)
                return false;
        }

        if (UseHighLowChannel)
        {
            if (width <= 0m)
                return false;
            var allowed = width * OrientationPercent / 100m;
            if (side == Sides.Buy && close < highest - allowed ||
                side == Sides.Sell && close > lowest + allowed)
                return false;
        }
        return true;
    }

    protected override void OnOwnTradeReceived(MyTrade trade)
    {
        base.OnOwnTradeReceived(trade);

        if (Position == 0m || !UseCounterOrders)
        {
            CancelCounter();
            return;
        }

        if (trade?.Order != _entryOrder || trade.Trade is null)
            return;

        CancelCounter();
        var side = Position > 0m ? Sides.Sell : Sides.Buy;
        var level = side == Sides.Sell ? _lowerFractal - OffsetPips * PipSize()
                                       : _upperFractal + OffsetPips * PipSize();
        if (level is not decimal price || price <= 0m)
            return;

        _counterOrder = new Order
        {
            Security = Security,
            Portfolio = Portfolio,
            Type = OrderTypes.Conditional,
            Side = side,
            Volume = Math.Abs(Position),
            Condition = new StopOrderCondition { ActivationPrice = Security.ShrinkPrice(price, ShrinkRules.Auto) },
            Comment = "Graal counter stop",
        };
        RegisterOrder(_counterOrder);
    }
}
