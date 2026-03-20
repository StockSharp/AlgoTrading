import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AwesomeOscillator

class n7_s_ao772012_strategy(Strategy):
    def __init__(self):
        super(n7_s_ao772012_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._ao_period = self.Param("AoPeriod", 5) \
            .SetDisplay("AO Period", "Period for the Awesome Oscillator", "Indicators")
        self._lookback = self.Param("Lookback", 3) \
            .SetDisplay("Lookback", "Number of AO values to look back for signal", "Indicators")

        self._ao_history = []
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AoPeriod(self):
        return self._ao_period.Value

    @property
    def Lookback(self):
        return self._lookback.Value

    def OnStarted(self, time):
        super(n7_s_ao772012_strategy, self).OnStarted(time)

        self._ao_history = []
        self._entry_price = 0.0

        self._ao = AwesomeOscillator()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ao, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, ao_value):
        if candle.State != CandleStates.Finished:
            return

        ao_val = float(ao_value)
        self._ao_history.append(ao_val)
        if len(self._ao_history) > 50:
            self._ao_history.pop(0)

        lookback = self.Lookback
        if len(self._ao_history) < lookback + 1:
            return

        close = float(candle.ClosePrice)
        current = self._ao_history[-1]
        prev = self._ao_history[-1 - lookback]

        rising = current > prev and current > 0
        falling = current < prev and current < 0

        # Manage positions
        if self.Position > 0:
            if current < 0 or (self._entry_price > 0 and close < self._entry_price * 0.98):
                self.SellMarket()
        elif self.Position < 0:
            if current > 0 or (self._entry_price > 0 and close > self._entry_price * 1.02):
                self.BuyMarket()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Entry
        if self.Position == 0:
            if rising:
                self._entry_price = close
                self.BuyMarket()
            elif falling:
                self._entry_price = close
                self.SellMarket()

    def OnReseted(self):
        super(n7_s_ao772012_strategy, self).OnReseted()
        self._ao_history = []
        self._entry_price = 0.0

    def CreateClone(self):
        return n7_s_ao772012_strategy()
