import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DeMarker
from StockSharp.Algo.Strategies import Strategy


class color_schaff_de_marker_trend_cycle_strategy(Strategy):
    def __init__(self):
        super(color_schaff_de_marker_trend_cycle_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 23) \
            .SetDisplay("Fast DeMarker", "Fast DeMarker period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Slow DeMarker", "Slow DeMarker period", "Indicator")
        self._cycle = self.Param("Cycle", 10) \
            .SetDisplay("Cycle", "Cycle length", "Indicator")
        self._high_level = self.Param("HighLevel", 60.0) \
            .SetDisplay("High Level", "Upper threshold", "Levels")
        self._low_level = self.Param("LowLevel", -60.0) \
            .SetDisplay("Low Level", "Lower threshold", "Levels")
        self._factor = self.Param("Factor", 0.5) \
            .SetDisplay("Factor", "Smoothing factor", "Indicator")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_st = 0.0
        self._prev_stc = 0.0
        self._st1_pass = False
        self._st2_pass = False
        self._prev_color = 0
        self._macd_buf = []
        self._st_buf = []

    @property
    def fast_period(self):
        return self._fast_period.Value
    @property
    def slow_period(self):
        return self._slow_period.Value
    @property
    def cycle(self):
        return self._cycle.Value
    @property
    def high_level(self):
        return self._high_level.Value
    @property
    def low_level(self):
        return self._low_level.Value
    @property
    def factor(self):
        return self._factor.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_schaff_de_marker_trend_cycle_strategy, self).OnReseted()
        self._prev_st = 0.0
        self._prev_stc = 0.0
        self._st1_pass = False
        self._st2_pass = False
        self._prev_color = 0
        self._macd_buf = []
        self._st_buf = []

    def OnStarted2(self, time):
        super(color_schaff_de_marker_trend_cycle_strategy, self).OnStarted2(time)
        fast = DeMarker()
        fast.Length = self.fast_period
        slow = DeMarker()
        slow.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.on_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast_val)
        slow_val = float(slow_val)
        macd = fast_val - slow_val
        cycle = int(self.cycle)
        fct = float(self.factor)

        self._macd_buf.append(macd)
        if len(self._macd_buf) > cycle:
            self._macd_buf.pop(0)
        if len(self._macd_buf) < cycle:
            return

        macd_high = max(self._macd_buf)
        macd_low = min(self._macd_buf)

        if macd_high == macd_low:
            st = self._prev_st
        else:
            st = (macd - macd_low) / (macd_high - macd_low) * 100.0

        if self._st1_pass:
            st = fct * (st - self._prev_st) + self._prev_st
        self._prev_st = st
        self._st1_pass = True

        self._st_buf.append(st)
        if len(self._st_buf) > cycle:
            self._st_buf.pop(0)
        if len(self._st_buf) < cycle:
            return

        st_high = max(self._st_buf)
        st_low = min(self._st_buf)

        if st_high == st_low:
            stc = self._prev_stc
        else:
            stc = (st - st_low) / (st_high - st_low) * 200.0 - 100.0

        if self._st2_pass:
            stc = fct * (stc - self._prev_stc) + self._prev_stc
        d_stc = stc - self._prev_stc
        self._prev_stc = stc
        self._st2_pass = True

        hl = float(self.high_level)
        ll = float(self.low_level)

        if stc > 0:
            if stc > hl:
                color = 7 if d_stc >= 0 else 6
            else:
                color = 5 if d_stc >= 0 else 4
        else:
            if stc < ll:
                color = 0 if d_stc < 0 else 1
            else:
                color = 2 if d_stc < 0 else 3

        if self._prev_color > 5 and color < 6 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_color < 2 and color > 1 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_color = color

    def CreateClone(self):
        return color_schaff_de_marker_trend_cycle_strategy()
