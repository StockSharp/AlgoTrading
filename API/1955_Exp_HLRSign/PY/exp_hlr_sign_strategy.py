import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy


# Mode constants
MODE_IN = 0
MODE_OUT = 1


class exp_hlr_sign_strategy(Strategy):

    def __init__(self):
        super(exp_hlr_sign_strategy, self).__init__()

        self._mode = self.Param("Mode", MODE_IN) \
            .SetDisplay("Mode", "Indicator operation mode", "General")
        self._range = self.Param("Range", 40) \
            .SetDisplay("Range", "Lookback period for HLR", "Indicator")
        self._up_level = self.Param("UpLevel", 80.0) \
            .SetDisplay("Up Level", "Upper level for HLR", "Indicator")
        self._dn_level = self.Param("DnLevel", 20.0) \
            .SetDisplay("Down Level", "Lower level for HLR", "Indicator")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._buy_open = self.Param("BuyOpen", True) \
            .SetDisplay("Buy Open", "Allow opening long positions", "Trading")
        self._sell_open = self.Param("SellOpen", True) \
            .SetDisplay("Sell Open", "Allow opening short positions", "Trading")
        self._buy_close = self.Param("BuyClose", True) \
            .SetDisplay("Buy Close", "Allow closing long positions", "Trading")
        self._sell_close = self.Param("SellClose", True) \
            .SetDisplay("Sell Close", "Allow closing short positions", "Trading")

        self._previous_hlr = 0.0
        self._is_first = True
        self._bars_since_trade = 0

    @property
    def Mode(self):
        return self._mode.Value

    @Mode.setter
    def Mode(self, value):
        self._mode.Value = value

    @property
    def Range(self):
        return self._range.Value

    @Range.setter
    def Range(self, value):
        self._range.Value = value

    @property
    def UpLevel(self):
        return self._up_level.Value

    @UpLevel.setter
    def UpLevel(self, value):
        self._up_level.Value = value

    @property
    def DnLevel(self):
        return self._dn_level.Value

    @DnLevel.setter
    def DnLevel(self, value):
        self._dn_level.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BuyOpen(self):
        return self._buy_open.Value

    @BuyOpen.setter
    def BuyOpen(self, value):
        self._buy_open.Value = value

    @property
    def SellOpen(self):
        return self._sell_open.Value

    @SellOpen.setter
    def SellOpen(self, value):
        self._sell_open.Value = value

    @property
    def BuyClose(self):
        return self._buy_close.Value

    @BuyClose.setter
    def BuyClose(self, value):
        self._buy_close.Value = value

    @property
    def SellClose(self):
        return self._sell_close.Value

    @SellClose.setter
    def SellClose(self, value):
        self._sell_close.Value = value

    def OnStarted2(self, time):
        super(exp_hlr_sign_strategy, self).OnStarted2(time)

        donchian = DonchianChannels()
        donchian.Length = self.Range

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(donchian, self.ProcessCandle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        upper_raw = value.UpperBand
        lower_raw = value.LowerBand

        if upper_raw is None or lower_raw is None:
            return

        upper = float(upper_raw)
        lower = float(lower_raw)

        mid = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        range_val = upper - lower
        if range_val != 0.0:
            hlr = 100.0 * (mid - lower) / range_val
        else:
            hlr = 0.0

        buy_signal = False
        sell_signal = False

        if self._is_first:
            self._previous_hlr = hlr
            self._is_first = False
            return

        up_level = float(self.UpLevel)
        dn_level = float(self.DnLevel)

        if self.Mode == MODE_IN:
            if hlr > up_level and self._previous_hlr <= up_level:
                buy_signal = True
            if hlr < dn_level and self._previous_hlr >= dn_level:
                sell_signal = True
        else:
            if hlr < up_level and self._previous_hlr >= up_level:
                sell_signal = True
            if hlr > dn_level and self._previous_hlr <= dn_level:
                buy_signal = True

        if self._bars_since_trade >= self.CooldownBars and buy_signal:
            if self.BuyOpen and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
                self._bars_since_trade = 0

        if self._bars_since_trade >= self.CooldownBars and sell_signal:
            if self.SellOpen and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
                self._bars_since_trade = 0

        self._previous_hlr = hlr

    def OnReseted(self):
        super(exp_hlr_sign_strategy, self).OnReseted()
        self._previous_hlr = 0.0
        self._is_first = True
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return exp_hlr_sign_strategy()
