import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AwesomeOscillator, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class zonal_trading_strategy(Strategy):
    def __init__(self):
        super(zonal_trading_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ao_prev1 = 0.0
        self._ao_prev2 = 0.0
        self._ac_prev1 = 0.0
        self._ac_prev2 = 0.0
        self._history_count = 0
        self._ao_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zonal_trading_strategy, self).OnReseted()
        self._ao_prev1 = 0.0
        self._ao_prev2 = 0.0
        self._ac_prev1 = 0.0
        self._ac_prev2 = 0.0
        self._history_count = 0
        self._ao_ema = None

    def OnStarted2(self, time):
        super(zonal_trading_strategy, self).OnStarted2(time)
        self._ao_prev1 = 0.0
        self._ao_prev2 = 0.0
        self._ac_prev1 = 0.0
        self._ac_prev2 = 0.0
        self._history_count = 0

        ao = AwesomeOscillator()
        self._ao_ema = ExponentialMovingAverage()
        self._ao_ema.Length = 5

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ao, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ao)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ao_value):
        if candle.State != CandleStates.Finished:
            return

        ao_val = float(ao_value)
        sma_result = process_float(self._ao_ema, ao_val, candle.OpenTime, True)
        if not self._ao_ema.IsFormed:
            self._ao_prev2 = self._ao_prev1
            self._ao_prev1 = ao_val
            return

        sma_value = float(sma_result)
        ac_value = ao_val - sma_value

        if self._history_count < 2:
            self._ao_prev2 = self._ao_prev1
            self._ao_prev1 = ao_val
            self._ac_prev2 = self._ac_prev1
            self._ac_prev1 = ac_value
            self._history_count += 1
            return

        buy_signal = (ao_val > self._ao_prev1 and ac_value > self._ac_prev1 and
                      (self._ac_prev1 < self._ac_prev2 or self._ao_prev1 < self._ao_prev2) and
                      ao_val > 0 and ac_value > 0)

        sell_signal = (ao_val < self._ao_prev1 and ac_value < self._ac_prev1 and
                       (self._ac_prev1 > self._ac_prev2 or self._ao_prev1 > self._ao_prev2) and
                       ao_val < 0 and ac_value < 0)

        if buy_signal and self.Position <= 0:
            self.BuyMarket()

        if sell_signal and self.Position >= 0:
            self.SellMarket()

        # Exit conditions
        if self.Position > 0 and ao_val < self._ao_prev1 and ac_value < self._ac_prev1:
            self.SellMarket()

        if self.Position < 0 and ao_val > self._ao_prev1 and ac_value > self._ac_prev1:
            self.BuyMarket()

        self._ao_prev2 = self._ao_prev1
        self._ao_prev1 = ao_val
        self._ac_prev2 = self._ac_prev1
        self._ac_prev1 = ac_value

    def CreateClone(self):
        return zonal_trading_strategy()
