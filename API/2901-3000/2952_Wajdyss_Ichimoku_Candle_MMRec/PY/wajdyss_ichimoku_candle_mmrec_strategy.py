import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class wajdyss_ichimoku_candle_mmrec_strategy(Strategy):
    def __init__(self):
        super(wajdyss_ichimoku_candle_mmrec_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._period = self.Param("Period", 26) \
            .SetDisplay("Period", "Kijun-Sen lookback period", "Indicators")

        self._prev_mid = None
        self._prev_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def Period(self):
        return self._period.Value

    def OnReseted(self):
        super(wajdyss_ichimoku_candle_mmrec_strategy, self).OnReseted()
        self._prev_mid = None
        self._prev_close = None

    def OnStarted2(self, time):
        super(wajdyss_ichimoku_candle_mmrec_strategy, self).OnStarted2(time)
        self._prev_mid = None
        self._prev_close = None

        middle = SimpleMovingAverage()
        middle.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(middle, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, middle)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, mid_value):
        if candle.State != CandleStates.Finished:
            return

        mv = float(mid_value)
        close = float(candle.ClosePrice)

        if self._prev_mid is None or self._prev_close is None:
            self._prev_mid = mv
            self._prev_close = close
            return

        # Price crosses above midline
        if self._prev_close <= self._prev_mid and close > mv and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Price crosses below midline
        elif self._prev_close >= self._prev_mid and close < mv and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_mid = mv
        self._prev_close = close

    def CreateClone(self):
        return wajdyss_ichimoku_candle_mmrec_strategy()
