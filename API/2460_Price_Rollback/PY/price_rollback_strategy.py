import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class price_rollback_strategy(Strategy):
    def __init__(self):
        super(price_rollback_strategy, self).__init__()

        self._corridor = self.Param("Corridor", 5000.0)
        self._stop_loss = self.Param("StopLoss", 500.0)
        self._take_profit = self.Param("TakeProfit", 400.0)
        self._trailing_stop = self.Param("TrailingStop", 300.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._prev_close = 0.0
        self._entry_price = 0.0
        self._trail_price = 0.0
        self._has_prev = False

    @property
    def Corridor(self):
        return self._corridor.Value

    @Corridor.setter
    def Corridor(self, value):
        self._corridor.Value = value

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
    def TrailingStop(self):
        return self._trailing_stop.Value

    @TrailingStop.setter
    def TrailingStop(self, value):
        self._trailing_stop.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(price_rollback_strategy, self).OnStarted(time)

        self._has_prev = False
        self._entry_price = 0.0
        self._trail_price = 0.0
        self._prev_close = 0.0

        atr = AverageTrueRange()
        atr.Length = 14

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2.0, UnitTypes.Percent),
            Unit(1.0, UnitTypes.Percent))

    def ProcessCandle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        atr_val = float(atr_value)

        if self._has_prev and self.Position == 0:
            body = close - open_price
            body_size = abs(body)

            if body_size > atr_val * 1.5:
                if body > 0.0:
                    self.SellMarket()
                elif body < 0.0:
                    self.BuyMarket()

        self._prev_close = close
        self._has_prev = True

    def OnReseted(self):
        super(price_rollback_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._entry_price = 0.0
        self._trail_price = 0.0
        self._has_prev = False

    def CreateClone(self):
        return price_rollback_strategy()
