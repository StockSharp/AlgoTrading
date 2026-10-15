import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy

class candle_strategy(Strategy):
    """
    Candle color reversal strategy with pip-based protection and trade cooldown.
    """

    def __init__(self):
        super(candle_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for candle evaluation", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0) \
            .SetDisplay("Take Profit (pips)", "Distance to take profit in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 30.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
        self._min_bars = self.Param("MinBars", 26) \
            .SetDisplay("Minimum Bars", "History length required before trading", "General")
        self._trade_cooldown = self.Param("TradeCooldown", TimeSpan.FromSeconds(10)) \
            .SetDisplay("Trade Cooldown", "Waiting time after each trade", "Risk")

        self._pip_size = 0.0
        self._finished_candles = 0
        self._next_allowed_time = DateTimeOffset.MinValue

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def take_profit_pips(self):
        return self._take_profit_pips.Value

    @take_profit_pips.setter
    def take_profit_pips(self, value):
        self._take_profit_pips.Value = value

    @property
    def trailing_stop_pips(self):
        return self._trailing_stop_pips.Value

    @trailing_stop_pips.setter
    def trailing_stop_pips(self, value):
        self._trailing_stop_pips.Value = value

    @property
    def min_bars(self):
        return self._min_bars.Value

    @min_bars.setter
    def min_bars(self, value):
        self._min_bars.Value = value

    @property
    def trade_cooldown(self):
        return self._trade_cooldown.Value

    @trade_cooldown.setter
    def trade_cooldown(self, value):
        self._trade_cooldown.Value = value

    def OnReseted(self):
        super(candle_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._finished_candles = 0
        self._next_allowed_time = DateTimeOffset.MinValue

    def OnStarted2(self, time):
        super(candle_strategy, self).OnStarted2(time)

        self._pip_size = (self.Security.PriceStep if self.Security.PriceStep is not None else 1.0) * 10.0

        tp = None
        trailing = None

        if self.take_profit_pips > 0 and self._pip_size > 0:
            tp = Unit(self.take_profit_pips * self._pip_size, UnitTypes.Absolute)
        if self.trailing_stop_pips > 0 and self._pip_size > 0:
            trailing = Unit(self.trailing_stop_pips * self._pip_size, UnitTypes.Absolute)

        if trailing is not None or tp is not None:
            self.StartProtection(tp, trailing)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._finished_candles += 1

        if self._finished_candles < self.min_bars * 2:
            return

        close_time = candle.CloseTime
        if self._next_allowed_time != DateTimeOffset.MinValue:
            try:
                if close_time < self._next_allowed_time:
                    return
            except:
                if DateTimeOffset(close_time) < self._next_allowed_time:
                    return

        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice
        trade_executed = False

        if self.Position > 0 and is_bearish:
            self.SellMarket()
            trade_executed = True
        elif self.Position < 0 and is_bullish:
            self.BuyMarket()
            trade_executed = True
        elif self.Position == 0:
            if is_bullish:
                self.BuyMarket()
                trade_executed = True
            elif is_bearish:
                self.SellMarket()
                trade_executed = True

        if trade_executed:
            self._next_allowed_time = close_time + self.trade_cooldown

    def CreateClone(self):
        return candle_strategy()
