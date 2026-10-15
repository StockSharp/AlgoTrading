import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class daily_breakpoint_strategy(Strategy):
    def __init__(self):
        super(daily_breakpoint_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._break_point_pct = self.Param("BreakPointPct", 0.3) \
            .SetDisplay("Break Point %", "Breakout offset as % of price", "General")
        self._last_bar_min_pct = self.Param("LastBarMinPct", 0.05) \
            .SetDisplay("Min Bar %", "Minimal bar size as % of price", "Filter")
        self._last_bar_max_pct = self.Param("LastBarMaxPct", 1.0) \
            .SetDisplay("Max Bar %", "Maximum bar size as % of price", "Filter")
        self._take_profit_pct = self.Param("TakeProfitPct", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_pct = self.Param("StopLossPct", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_time = None
        self._has_prev = False
        self._day_open = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def break_point_pct(self):
        return self._break_point_pct.Value

    @property
    def last_bar_min_pct(self):
        return self._last_bar_min_pct.Value

    @property
    def last_bar_max_pct(self):
        return self._last_bar_max_pct.Value

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    def OnReseted(self):
        super(daily_breakpoint_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_time = None
        self._has_prev = False
        self._day_open = 0.0

    def OnStarted2(self, time):
        super(daily_breakpoint_strategy, self).OnStarted2(time)
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            isStopTrailing=True,
            useMarketOrders=True)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process).Start()

    def process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self._has_prev or candle.OpenTime.Date != self._prev_time.Date:
            self._day_open = float(candle.OpenPrice)

        if self.Position == 0 and self._has_prev:
            price = float(candle.ClosePrice)
            last_size = abs(self._prev_close - self._prev_open)
            min_size = float(self.last_bar_min_pct) / 100.0 * price
            max_size = float(self.last_bar_max_pct) / 100.0 * price
            offset = float(self.break_point_pct) / 100.0 * price
            break_buy = self._day_open + offset
            break_sell = self._day_open - offset

            if (self._prev_close > self._prev_open and price - self._day_open >= offset and
                    last_size >= min_size and last_size <= max_size and
                    break_buy >= self._prev_open and break_buy <= self._prev_close):
                self.BuyMarket()
            elif (self._prev_close < self._prev_open and self._day_open - price >= offset and
                    last_size >= min_size and last_size <= max_size and
                    break_sell <= self._prev_open and break_sell >= self._prev_close):
                self.SellMarket()

        self._prev_open = float(candle.OpenPrice)
        self._prev_close = float(candle.ClosePrice)
        self._prev_time = candle.OpenTime
        self._has_prev = True

    def CreateClone(self):
        return daily_breakpoint_strategy()
