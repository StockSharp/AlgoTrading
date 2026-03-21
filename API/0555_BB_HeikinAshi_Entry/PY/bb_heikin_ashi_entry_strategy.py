import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bb_heikin_ashi_entry_strategy(Strategy):
    def __init__(self):
        super(bb_heikin_ashi_entry_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BollingerLength", 20) \
            .SetDisplay("Bollinger Length", "Period of Bollinger Bands", "Bollinger")
        self._bb_width = self.Param("BollingerWidth", 2.0) \
            .SetDisplay("Bollinger Width", "Standard deviation multiplier", "Bollinger")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._ha_open_prev1 = 0.0
        self._ha_close_prev1 = 0.0
        self._ha_high_prev1 = 0.0
        self._ha_low_prev1 = 0.0
        self._ha_open_prev2 = 0.0
        self._ha_close_prev2 = 0.0
        self._ha_high_prev2 = 0.0
        self._ha_low_prev2 = 0.0
        self._upper_bb_prev1 = 0.0
        self._lower_bb_prev1 = 0.0
        self._upper_bb_prev2 = 0.0
        self._lower_bb_prev2 = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(bb_heikin_ashi_entry_strategy, self).OnReseted()
        self._ha_open_prev1 = self._ha_open_prev2 = 0.0
        self._ha_close_prev1 = self._ha_close_prev2 = 0.0
        self._ha_high_prev1 = self._ha_high_prev2 = 0.0
        self._ha_low_prev1 = self._ha_low_prev2 = 0.0
        self._upper_bb_prev1 = self._upper_bb_prev2 = 0.0
        self._lower_bb_prev1 = self._lower_bb_prev2 = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bb_heikin_ashi_entry_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self._bb_length.Value
        bb.Width = self._bb_width.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.OnProcess).Start()
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        ha_close = (o + h + l + c) / 4.0
        if self._ha_open_prev1 == 0:
            ha_open = (o + c) / 2.0
        else:
            ha_open = (self._ha_open_prev1 + self._ha_close_prev1) / 2.0
        ha_high = max(h, ha_open, ha_close)
        ha_low = min(l, ha_open, ha_close)
        bb = bb_value
        upper = bb.UpBand
        lower = bb.LowBand
        if upper is None or lower is None:
            self._shift(ha_open, ha_close, ha_high, ha_low, 0.0, 0.0)
            return
        upper_v = float(upper)
        lower_v = float(lower)
        if self._ha_open_prev1 != 0:
            red1 = self._ha_close_prev1 < self._ha_open_prev1 and self._ha_low_prev1 <= self._lower_bb_prev1
            red2 = self._ha_close_prev2 < self._ha_open_prev2 and self._ha_low_prev2 <= self._lower_bb_prev2
            green_confirm = ha_close > ha_open
            buy_signal = (red1 or red2) and green_confirm
            green1 = self._ha_close_prev1 > self._ha_open_prev1 and self._ha_high_prev1 >= self._upper_bb_prev1
            green2 = self._ha_close_prev2 > self._ha_open_prev2 and self._ha_high_prev2 >= self._upper_bb_prev2
            red_confirm = ha_close < ha_open
            sell_signal = (green1 or green2) and red_confirm
            if buy_signal and self.Position == 0:
                self.BuyMarket()
            elif sell_signal and self.Position == 0:
                self.SellMarket()
        self._shift(ha_open, ha_close, ha_high, ha_low, upper_v, lower_v)

    def _shift(self, ha_open, ha_close, ha_high, ha_low, upper, lower):
        self._ha_open_prev2 = self._ha_open_prev1
        self._ha_open_prev1 = ha_open
        self._ha_close_prev2 = self._ha_close_prev1
        self._ha_close_prev1 = ha_close
        self._ha_high_prev2 = self._ha_high_prev1
        self._ha_high_prev1 = ha_high
        self._ha_low_prev2 = self._ha_low_prev1
        self._ha_low_prev1 = ha_low
        self._upper_bb_prev2 = self._upper_bb_prev1
        self._upper_bb_prev1 = upper
        self._lower_bb_prev2 = self._lower_bb_prev1
        self._lower_bb_prev1 = lower

    def CreateClone(self):
        return bb_heikin_ashi_entry_strategy()
