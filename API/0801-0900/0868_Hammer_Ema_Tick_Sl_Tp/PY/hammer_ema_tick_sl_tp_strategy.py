import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hammer_ema_tick_sl_tp_strategy(Strategy):
    """
    Hammer + EMA strategy with tick-based stop loss and take profit.
    A hammer (lower wick at least twice the body, upper wick no longer than the body) closing above the EMA buys; an inverted
    hammer (upper wick at least twice the body, lower wick no longer than the body) closing below the EMA sells. An opposite
    signal reverses the position. Stop loss and take profit are set in price steps.
    """

    def __init__(self):
        super(hammer_ema_tick_sl_tp_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 50).SetGreaterThanZero().SetDisplay("EMA Length", "EMA trend filter length", "Indicators")
        self._stop_loss_ticks = self.Param("StopLossTicks", 1).SetNotNegative().SetDisplay("Stop Loss Ticks", "Stop loss in price steps", "Risk")
        self._take_profit_ticks = self.Param("TakeProfitTicks", 10).SetNotNegative().SetDisplay("Take Profit Ticks", "Take profit in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(hammer_ema_tick_sl_tp_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        step = Decimal(1)
        if self.Security is not None and self.Security.PriceStep is not None:
            step = self.Security.PriceStep
        tp = self._take_profit_ticks.Value
        sl = self._stop_loss_ticks.Value
        self.StartProtection(
            Unit(Decimal(tp) * step, UnitTypes.Absolute) if tp > 0 else Unit(),
            Unit(Decimal(sl) * step, UnitTypes.Absolute) if sl > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        ema = float(ema_value)

        body = abs(close - open_price)
        if body <= 0:
            return

        upper_wick = high - max(open_price, close)
        lower_wick = min(open_price, close) - low

        is_hammer = lower_wick >= 2 * body and upper_wick <= body
        is_inverted_hammer = upper_wick >= 2 * body and lower_wick <= body

        if is_hammer and close > ema and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif is_inverted_hammer and close < ema and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return hammer_ema_tick_sl_tp_strategy()
