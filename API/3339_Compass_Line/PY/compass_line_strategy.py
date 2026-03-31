import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class compass_line_strategy(Strategy):
    def __init__(self):
        super(compass_line_strategy, self).__init__()

        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")

        self._bb = None
        self._rsi = None
        self._prev_close = None
        self._prev_lower = None
        self._prev_upper = None

    @property
    def bb_period(self):
        return self._bb_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    def OnReseted(self):
        super(compass_line_strategy, self).OnReseted()
        self._bb = None
        self._rsi = None
        self._prev_close = None
        self._prev_lower = None
        self._prev_upper = None

    def OnStarted2(self, time):
        super(compass_line_strategy, self).OnStarted2(time)

        self._bb = BollingerBands()
        self._bb.Length = self.bb_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.BindEx(self._bb, self._rsi, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, bb_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bb.IsFormed or not self._rsi.IsFormed:
            return

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        rsi = float(rsi_value)
        close = float(candle.ClosePrice)

        if self._prev_close is not None and self._prev_lower is not None and self._prev_upper is not None:
            cross_below_lower = self._prev_close > self._prev_lower and close <= lower
            cross_above_upper = self._prev_close < self._prev_upper and close >= upper

            if cross_below_lower and rsi < 45.0 and self.Position <= 0:
                self.BuyMarket()
            elif cross_above_upper and rsi > 55.0 and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_lower = lower
        self._prev_upper = upper

    def CreateClone(self):
        return compass_line_strategy()
