import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy

DIRECT = 0
AGAINST = 1


class fractal_mfi_strategy(Strategy):
    def __init__(self):
        super(fractal_mfi_strategy, self).__init__()

        self._mfi_period = self.Param("MfiPeriod", 30)
        self._high_level = self.Param("HighLevel", 70.0)
        self._low_level = self.Param("LowLevel", 30.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._trend = self.Param("Trend", DIRECT)
        self._buy_pos_open = self.Param("BuyPosOpen", True)
        self._sell_pos_open = self.Param("SellPosOpen", True)
        self._buy_pos_close = self.Param("BuyPosClose", True)
        self._sell_pos_close = self.Param("SellPosClose", True)

        self._prev_mfi = 0.0
        self._is_prev_set = False

    @property
    def MfiPeriod(self):
        return self._mfi_period.Value

    @MfiPeriod.setter
    def MfiPeriod(self, value):
        self._mfi_period.Value = value

    @property
    def HighLevel(self):
        return self._high_level.Value

    @HighLevel.setter
    def HighLevel(self, value):
        self._high_level.Value = value

    @property
    def LowLevel(self):
        return self._low_level.Value

    @LowLevel.setter
    def LowLevel(self, value):
        self._low_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Trend(self):
        return self._trend.Value

    @Trend.setter
    def Trend(self, value):
        self._trend.Value = value

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
        super(fractal_mfi_strategy, self).OnStarted(time)

        self._prev_mfi = 0.0
        self._is_prev_set = False

        mfi = MoneyFlowIndex()
        mfi.Length = self.MfiPeriod
        self._mfi = mfi

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(mfi, self.ProcessCandle).Start()

        self.StartProtection(None, None)

    def ProcessCandle(self, candle, current_mfi):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        mfi_val = float(current_mfi)

        if not self._mfi.IsFormed:
            self._prev_mfi = mfi_val
            return

        if not self._is_prev_set:
            self._prev_mfi = mfi_val
            self._is_prev_set = True
            return

        self._process_signal(float(candle.ClosePrice), self._prev_mfi, mfi_val)
        self._prev_mfi = mfi_val

    def _process_signal(self, price, prev, current):
        high = float(self.HighLevel)
        low = float(self.LowLevel)
        vol = float(self.Volume)

        if int(self.Trend) == DIRECT:
            if prev > low and current <= low:
                pos = float(self.Position)
                if self.SellPosClose and pos < 0:
                    self.BuyMarket(abs(pos))
                pos = float(self.Position)
                if self.BuyPosOpen and pos <= 0:
                    self.BuyMarket(vol + abs(pos))

            if prev < high and current >= high:
                pos = float(self.Position)
                if self.BuyPosClose and pos > 0:
                    self.SellMarket(abs(pos))
                pos = float(self.Position)
                if self.SellPosOpen and pos >= 0:
                    self.SellMarket(vol + abs(pos))
        else:
            if prev > low and current <= low:
                pos = float(self.Position)
                if self.BuyPosClose and pos > 0:
                    self.SellMarket(abs(pos))
                pos = float(self.Position)
                if self.SellPosOpen and pos >= 0:
                    self.SellMarket(vol + abs(pos))

            if prev < high and current >= high:
                pos = float(self.Position)
                if self.SellPosClose and pos < 0:
                    self.BuyMarket(abs(pos))
                pos = float(self.Position)
                if self.BuyPosOpen and pos <= 0:
                    self.BuyMarket(vol + abs(pos))

    def OnReseted(self):
        super(fractal_mfi_strategy, self).OnReseted()
        self._prev_mfi = 0.0
        self._is_prev_set = False

    def CreateClone(self):
        return fractal_mfi_strategy()
