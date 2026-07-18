import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class percentage_crossover_channel_system_strategy(Strategy):
    """Percentage Crossover Channel breakout system with SL/TP."""

    def __init__(self):
        super(percentage_crossover_channel_system_strategy, self).__init__()

        self._percent = self.Param("Percent", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Channel Percent", "Percentage width of the channel", "Indicator")
        self._shift = self.Param("Shift", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift", "Number of bars used for crossover comparison", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Bar", "Bars back to evaluate indicator colors", "Trading Rules")
        self._buy_open = self.Param("BuyPositionsOpen", True) \
            .SetDisplay("Enable Long Entries", "Allow long position openings", "Trading Rules")
        self._sell_open = self.Param("SellPositionsOpen", True) \
            .SetDisplay("Enable Short Entries", "Allow short position openings", "Trading Rules")
        self._buy_close = self.Param("BuyPositionsClose", True) \
            .SetDisplay("Allow Long Exits", "Permit closing long trades on bearish signals", "Trading Rules")
        self._sell_close = self.Param("SellPositionsClose", True) \
            .SetDisplay("Allow Short Exits", "Permit closing short trades on bullish signals", "Trading Rules")
        self._stop_loss = self.Param("StopLoss", 1000) \
            .SetDisplay("Stop Loss (steps)", "Protective stop loss distance in price steps", "Risk Management")
        self._take_profit = self.Param("TakeProfit", 2000) \
            .SetDisplay("Take Profit (steps)", "Target profit distance in price steps", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Primary timeframe for analysis", "General")

        self._color_history = []
        self._upper_history = []
        self._lower_history = []
        self._prev_middle = 0.0
        self._has_middle = False
        self._entry_price = None

    @property
    def Percent(self):
        return self._percent.Value
    @property
    def Shift(self):
        return self._shift.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value
    @property
    def BuyPositionsOpen(self):
        return self._buy_open.Value
    @property
    def SellPositionsOpen(self):
        return self._sell_open.Value
    @property
    def BuyPositionsClose(self):
        return self._buy_close.Value
    @property
    def SellPositionsClose(self):
        return self._sell_close.Value
    @property
    def StopLoss(self):
        return self._stop_loss.Value
    @property
    def TakeProfit(self):
        return self._take_profit.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(percentage_crossover_channel_system_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        stop_triggered = self._handle_risk(candle)

        if len(self._color_history) > self.SignalBar:
            ri = len(self._color_history) - self.SignalBar
            oi = ri - 1
            if oi >= 0:
                rc = self._color_history[ri]
                oc = self._color_history[oi]

                should_close_short = self.SellPositionsClose and oc > 2
                should_close_long = self.BuyPositionsClose and oc < 2
                should_buy = self.BuyPositionsOpen and oc > 2 and rc < 3
                should_sell = self.SellPositionsOpen and oc < 2 and rc > 1

                if should_close_long and self.Position > 0:
                    self.SellMarket()
                    self._entry_price = None
                if should_close_short and self.Position < 0:
                    self.BuyMarket()
                    self._entry_price = None

                if not stop_triggered and self.Position == 0:
                    if should_buy:
                        self.BuyMarket()
                        self._entry_price = float(candle.ClosePrice)
                    elif should_sell:
                        self.SellMarket()
                        self._entry_price = float(candle.ClosePrice)

        color = self._calc_color(candle)
        self._color_history.append(color)
        self._trim()

    def _handle_risk(self, candle):
        if self._entry_price is None:
            return False
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return False
        step = float(sec.PriceStep)
        triggered = False

        if self.Position > 0:
            if self.StopLoss > 0:
                sl = self._entry_price - self.StopLoss * step
                if float(candle.LowPrice) <= sl:
                    self.SellMarket()
                    self._entry_price = None
                    triggered = True
            if not triggered and self.TakeProfit > 0:
                tp = self._entry_price + self.TakeProfit * step
                if float(candle.HighPrice) >= tp:
                    self.SellMarket()
                    self._entry_price = None
                    triggered = True
        elif self.Position < 0:
            if self.StopLoss > 0:
                sl = self._entry_price + self.StopLoss * step
                if float(candle.HighPrice) >= sl:
                    self.BuyMarket()
                    self._entry_price = None
                    triggered = True
            if not triggered and self.TakeProfit > 0:
                tp = self._entry_price - self.TakeProfit * step
                if float(candle.LowPrice) <= tp:
                    self.BuyMarket()
                    self._entry_price = None
                    triggered = True

        if self.Position == 0:
            self._entry_price = None
        return triggered

    def _calc_color(self, candle):
        pf = float(self.Percent) / 100.0
        plus_var = 1.0 + pf
        minus_var = 1.0 - pf
        close = float(candle.ClosePrice)

        if not self._has_middle:
            self._prev_middle = close
            self._has_middle = True

        middle = self._prev_middle
        lower_c = close * minus_var
        upper_c = close * plus_var

        if lower_c > self._prev_middle:
            middle = lower_c
        elif upper_c < self._prev_middle:
            middle = upper_c

        upper = middle + middle * pf
        lower = middle - middle * pf
        self._prev_middle = middle

        color = 2
        if len(self._upper_history) >= self.Shift:
            ref_idx = len(self._upper_history) - self.Shift
            ref_upper = self._upper_history[ref_idx]
            ref_lower = self._lower_history[ref_idx]
            if close > ref_upper:
                color = 4 if float(candle.OpenPrice) <= close else 3
            elif close < ref_lower:
                color = 0 if float(candle.OpenPrice) > close else 1

        self._upper_history.append(upper)
        self._lower_history.append(lower)
        return color

    def _trim(self):
        max_cap = max(self.Shift + self.SignalBar + 5, 16)
        if len(self._color_history) <= max_cap:
            return
        remove = len(self._color_history) - max_cap
        self._color_history = self._color_history[remove:]
        self._upper_history = self._upper_history[remove:]
        self._lower_history = self._lower_history[remove:]

    def OnReseted(self):
        super(percentage_crossover_channel_system_strategy, self).OnReseted()
        self._color_history = []
        self._upper_history = []
        self._lower_history = []
        self._has_middle = False
        self._prev_middle = 0.0
        self._entry_price = None

    def CreateClone(self):
        return percentage_crossover_channel_system_strategy()
