import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class exp_extremum_strategy(Strategy):

    def __init__(self):
        super(exp_extremum_strategy, self).__init__()

        self._length = self.Param("Length", 40) \
            .SetDisplay("Period", "Indicator period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame of the Extremum indicator", "General")
        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Signals")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Entry", "Permission to buy", "Signals")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Entry", "Permission to sell", "Signals")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Close Long", "Permission to exit long positions", "Signals")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Close Short", "Permission to exit short positions", "Signals")

        self._min_high = Lowest()
        self._max_low = Highest()
        self._up_prev1 = False
        self._dn_prev1 = False
        self._up_prev2 = False
        self._dn_prev2 = False
        self._bars_since_trade = 0

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def BuyPosOpen(self):
        return self._buy_pos_open.Value

    @BuyPosOpen.setter
    def BuyPosOpen(self, value):
        self._buy_pos_open.Value = value

    @property
    def SellPosOpen(self):
        return self._sell_pos_open.Value

    @SellPosOpen.setter
    def SellPosOpen(self, value):
        self._sell_pos_open.Value = value

    @property
    def BuyPosClose(self):
        return self._buy_pos_close.Value

    @BuyPosClose.setter
    def BuyPosClose(self, value):
        self._buy_pos_close.Value = value

    @property
    def SellPosClose(self):
        return self._sell_pos_close.Value

    @SellPosClose.setter
    def SellPosClose(self, value):
        self._sell_pos_close.Value = value

    def OnStarted(self, time):
        super(exp_extremum_strategy, self).OnStarted(time)

        self._min_high.Length = self.Length
        self._max_low.Length = self.Length

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        mhi = DecimalIndicatorValue(self._min_high, candle.HighPrice, candle.OpenTime)
        mhi.IsFinal = True
        min_high_value = float(self._min_high.Process(mhi))
        mli = DecimalIndicatorValue(self._max_low, candle.LowPrice, candle.OpenTime)
        mli.IsFinal = True
        max_low_value = float(self._max_low.Process(mli))

        if not self._min_high.IsFormed or not self._max_low.IsFormed:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        pressure = (float(candle.HighPrice) - min_high_value) + (float(candle.LowPrice) - max_low_value)
        up = pressure > 0.0
        dn = pressure < 0.0
        bullish_reversal = self._dn_prev2 and self._up_prev1 and up and candle.ClosePrice > candle.OpenPrice
        bearish_reversal = self._up_prev2 and self._dn_prev1 and dn and candle.ClosePrice < candle.OpenPrice

        pos = self.Position

        if self.BuyPosClose and bearish_reversal and pos > 0:
            self.SellMarket(pos)
            self._bars_since_trade = 0

        if self.SellPosClose and bullish_reversal and pos < 0:
            self.BuyMarket(-pos)
            self._bars_since_trade = 0

        pos = self.Position

        if self._bars_since_trade >= self.CooldownBars:
            if self.BuyPosOpen and bullish_reversal and pos <= 0:
                self.BuyMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0

            if self.SellPosOpen and bearish_reversal and pos >= 0:
                self.SellMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0

        self._up_prev2 = self._up_prev1
        self._dn_prev2 = self._dn_prev1
        self._up_prev1 = up
        self._dn_prev1 = dn

    def OnReseted(self):
        super(exp_extremum_strategy, self).OnReseted()
        self._min_high.Length = self.Length
        self._max_low.Length = self.Length
        self._min_high.Reset()
        self._max_low.Reset()
        self._up_prev1 = False
        self._dn_prev1 = False
        self._up_prev2 = False
        self._dn_prev2 = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return exp_extremum_strategy()
