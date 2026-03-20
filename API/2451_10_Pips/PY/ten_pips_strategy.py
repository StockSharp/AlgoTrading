import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange
from StockSharp.Algo.Strategies import Strategy


class ten_pips_strategy(Strategy):
    def __init__(self):
        super(ten_pips_strategy, self).__init__()

        self._take_profit = self.Param("TakeProfit", 500.0)
        self._stop_loss = self.Param("StopLoss", 300.0)
        self._lookback = self.Param("Lookback", 30)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._entry_price = 0.0
        self._previous_roc = 0.0
        self._has_previous_roc = False

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
    def Lookback(self):
        return self._lookback.Value

    @Lookback.setter
    def Lookback(self, value):
        self._lookback.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ten_pips_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._previous_roc = 0.0
        self._has_previous_roc = False

        roc = RateOfChange()
        roc.Length = self.Lookback

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(roc, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, roc_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        roc_val = float(roc_value)

        buy_signal = self._has_previous_roc and self._previous_roc <= 4.0 and roc_val > 4.0
        sell_signal = self._has_previous_roc and self._previous_roc >= -4.0 and roc_val < -4.0

        tp = float(self.TakeProfit)
        sl = float(self.StopLoss)

        if self.Position == 0:
            if buy_signal:
                self.BuyMarket()
                self._entry_price = close
            elif sell_signal:
                self.SellMarket()
                self._entry_price = close
        elif self.Position > 0:
            if close >= self._entry_price + tp or close <= self._entry_price - sl:
                self.SellMarket()
        elif self.Position < 0:
            if close <= self._entry_price - tp or close >= self._entry_price + sl:
                self.BuyMarket()

        self._previous_roc = roc_val
        self._has_previous_roc = True

    def OnReseted(self):
        super(ten_pips_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._previous_roc = 0.0
        self._has_previous_roc = False

    def CreateClone(self):
        return ten_pips_strategy()
