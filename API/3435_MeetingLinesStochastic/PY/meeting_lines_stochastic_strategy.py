import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class meeting_lines_stochastic_strategy(Strategy):
    def __init__(self):
        super(meeting_lines_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._stoch_period = self.Param("StochPeriod", 14)
        self._stoch_low = self.Param("StochLow", 30.0)
        self._stoch_high = self.Param("StochHigh", 70.0)

        self._prev_candle = None
        self._prev_prev_candle = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StochPeriod(self):
        return self._stoch_period.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stoch_period.Value = value

    @property
    def StochLow(self):
        return self._stoch_low.Value

    @StochLow.setter
    def StochLow(self, value):
        self._stoch_low.Value = value

    @property
    def StochHigh(self):
        return self._stoch_high.Value

    @StochHigh.setter
    def StochHigh(self, value):
        self._stoch_high.Value = value

    def OnReseted(self):
        super(meeting_lines_stochastic_strategy, self).OnReseted()
        self._prev_candle = None
        self._prev_prev_candle = None

    def OnStarted(self, time):
        super(meeting_lines_stochastic_strategy, self).OnStarted(time)
        self._prev_candle = None
        self._prev_prev_candle = None

        stoch = StochasticOscillator()
        stoch.K.Length = self.StochPeriod
        stoch.D.Length = 3

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stoch, self._process_candle).Start()

    def _process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        k_val = stoch_value.K
        if k_val is None:
            self._update_state(candle)
            return
        k_value = float(k_val)

        if self._prev_candle is not None and self._prev_prev_candle is not None:
            avg_body = (abs(float(self._prev_candle.ClosePrice) - float(self._prev_candle.OpenPrice))
                + abs(float(self._prev_prev_candle.ClosePrice) - float(self._prev_prev_candle.OpenPrice))) / 2.0

            if avg_body > 0:
                # Bullish meeting lines
                prev_bearish = (float(self._prev_candle.OpenPrice) > float(self._prev_candle.ClosePrice)
                    and (float(self._prev_candle.OpenPrice) - float(self._prev_candle.ClosePrice)) > avg_body * 0.5)
                curr_bullish = (float(candle.ClosePrice) > float(candle.OpenPrice)
                    and (float(candle.ClosePrice) - float(candle.OpenPrice)) > avg_body * 0.5)
                closes_near = abs(float(candle.ClosePrice) - float(self._prev_candle.ClosePrice)) < avg_body * 0.3

                if prev_bearish and curr_bullish and closes_near and k_value < self.StochLow and self.Position <= 0:
                    self.BuyMarket()

                # Bearish meeting lines
                prev_bullish = (float(self._prev_candle.ClosePrice) > float(self._prev_candle.OpenPrice)
                    and (float(self._prev_candle.ClosePrice) - float(self._prev_candle.OpenPrice)) > avg_body * 0.5)
                curr_bearish = (float(candle.OpenPrice) > float(candle.ClosePrice)
                    and (float(candle.OpenPrice) - float(candle.ClosePrice)) > avg_body * 0.5)
                closes_near2 = abs(float(candle.ClosePrice) - float(self._prev_candle.ClosePrice)) < avg_body * 0.3

                if prev_bullish and curr_bearish and closes_near2 and k_value > self.StochHigh and self.Position >= 0:
                    self.SellMarket()

        self._update_state(candle)

    def _update_state(self, candle):
        self._prev_prev_candle = self._prev_candle
        self._prev_candle = candle

    def CreateClone(self):
        return meeting_lines_stochastic_strategy()
