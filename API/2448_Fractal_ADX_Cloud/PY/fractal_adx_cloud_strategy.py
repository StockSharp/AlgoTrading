import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class fractal_adx_cloud_strategy(Strategy):
    def __init__(self):
        super(fractal_adx_cloud_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 30)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)
        self._buy_pos_open = self.Param("BuyPosOpen", True)
        self._sell_pos_open = self.Param("SellPosOpen", True)
        self._buy_pos_close = self.Param("BuyPosClose", True)
        self._sell_pos_close = self.Param("SellPosClose", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._prev_plus_di = None
        self._prev_minus_di = None

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

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

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(fractal_adx_cloud_strategy, self).OnStarted(time)

        self._prev_plus_di = None
        self._prev_minus_di = None

        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(adx, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(float(self.TakeProfit), UnitTypes.Absolute),
            Unit(float(self.StopLoss), UnitTypes.Absolute))

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        plus_di_val = adx_value.Dx.Plus
        minus_di_val = adx_value.Dx.Minus

        if plus_di_val is None or minus_di_val is None:
            return

        plus_di = float(plus_di_val)
        minus_di = float(minus_di_val)

        if self._prev_plus_di is not None and self._prev_minus_di is not None:
            if plus_di > minus_di:
                if self.SellPosClose and self.Position < 0:
                    self.BuyMarket()

                if self.BuyPosOpen and self._prev_plus_di <= self._prev_minus_di and self.Position <= 0:
                    self.BuyMarket()
            elif minus_di > plus_di:
                if self.BuyPosClose and self.Position > 0:
                    self.SellMarket()

                if self.SellPosOpen and self._prev_plus_di >= self._prev_minus_di and self.Position >= 0:
                    self.SellMarket()

        self._prev_plus_di = plus_di
        self._prev_minus_di = minus_di

    def OnReseted(self):
        super(fractal_adx_cloud_strategy, self).OnReseted()
        self._prev_plus_di = None
        self._prev_minus_di = None

    def CreateClone(self):
        return fractal_adx_cloud_strategy()
