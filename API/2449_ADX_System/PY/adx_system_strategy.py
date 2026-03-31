import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class adx_system_strategy(Strategy):
    def __init__(self):
        super(adx_system_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 10)
        self._take_profit = self.Param("TakeProfit", 150.0)
        self._stop_loss = self.Param("StopLoss", 250.0)
        self._trailing_stop = self.Param("TrailingStop", 120.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._prev_adx = 0.0
        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

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

    def OnStarted2(self, time):
        super(adx_system_strategy, self).OnStarted2(time)

        self._prev_adx = 0.0
        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(adx, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        ma_val = adx_value.MovingAverage
        if ma_val is None:
            return

        current_adx = float(ma_val)
        dx = adx_value.Dx

        plus_val = dx.Plus
        minus_val = dx.Minus
        if plus_val is None or minus_val is None:
            return

        current_plus_di = float(plus_val)
        current_minus_di = float(minus_val)

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        tp = float(self.TakeProfit)
        sl = float(self.StopLoss)
        trail = float(self.TrailingStop)

        if self._prev_adx != 0.0:
            if self.Position == 0:
                if current_adx >= 20.0 and current_adx > self._prev_adx and self._prev_plus_di <= self._prev_minus_di and current_plus_di > current_minus_di:
                    self.BuyMarket()
                    self._entry_price = close
                    self._stop_price = close - sl
                    self._take_price = close + tp
                elif current_adx >= 20.0 and current_adx > self._prev_adx and self._prev_minus_di <= self._prev_plus_di and current_minus_di > current_plus_di:
                    self.SellMarket()
                    self._entry_price = close
                    self._stop_price = close + sl
                    self._take_price = close - tp
            elif self.Position > 0:
                if self._prev_plus_di >= self._prev_minus_di and current_plus_di < current_minus_di:
                    self.SellMarket()
                    self._entry_price = None
                    self._stop_price = None
                    self._take_price = None
                elif self._entry_price is not None:
                    entry = self._entry_price
                    if close - entry > trail:
                        new_stop = close - trail
                        if self._stop_price is not None and new_stop > self._stop_price:
                            self._stop_price = new_stop

                    if self._take_price is not None and high >= self._take_price:
                        self.SellMarket()
                        self._entry_price = None
                        self._stop_price = None
                        self._take_price = None
                    elif self._stop_price is not None and low <= self._stop_price:
                        self.SellMarket()
                        self._entry_price = None
                        self._stop_price = None
                        self._take_price = None
            elif self.Position < 0:
                if self._prev_minus_di >= self._prev_plus_di and current_minus_di < current_plus_di:
                    self.BuyMarket()
                    self._entry_price = None
                    self._stop_price = None
                    self._take_price = None
                elif self._entry_price is not None:
                    entry = self._entry_price
                    if entry - close > trail:
                        new_stop = close + trail
                        if self._stop_price is not None and new_stop < self._stop_price:
                            self._stop_price = new_stop

                    if self._take_price is not None and low <= self._take_price:
                        self.BuyMarket()
                        self._entry_price = None
                        self._stop_price = None
                        self._take_price = None
                    elif self._stop_price is not None and high >= self._stop_price:
                        self.BuyMarket()
                        self._entry_price = None
                        self._stop_price = None
                        self._take_price = None

        self._prev_adx = current_adx
        self._prev_plus_di = current_plus_di
        self._prev_minus_di = current_minus_di

    def OnReseted(self):
        super(adx_system_strategy, self).OnReseted()
        self._prev_adx = 0.0
        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return adx_system_strategy()
