import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class close_agent_strategy(Strategy):

    def __init__(self):
        super(close_agent_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for indicators", "General")
        self._rsi_length = self.Param("RsiLength", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._bollinger_length = self.Param("BollingerLength", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Length", "Bollinger Bands period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "Overbought threshold", "Signals")
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "Oversold threshold", "Signals")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @RsiLength.setter
    def RsiLength(self, value):
        self._rsi_length.Value = value

    @property
    def BollingerLength(self):
        return self._bollinger_length.Value

    @BollingerLength.setter
    def BollingerLength(self, value):
        self._bollinger_length.Value = value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    @RsiOverbought.setter
    def RsiOverbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    @RsiOversold.setter
    def RsiOversold(self, value):
        self._rsi_oversold.Value = value

    def OnReseted(self):
        super(close_agent_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(close_agent_strategy, self).OnStarted(time)
        self._has_prev = False

        sma_fast = SimpleMovingAverage()
        sma_fast.Length = 10
        sma_slow = SimpleMovingAverage()
        sma_slow.Length = 30

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma_fast, sma_slow, self._process_candle).Start()

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast)
        sv = float(slow)

        if not self._has_prev:
            self._prev_fast = fv
            self._prev_slow = sv
            self._has_prev = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fv > sv
        cross_down = self._prev_fast >= self._prev_slow and fv < sv

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return close_agent_strategy()
