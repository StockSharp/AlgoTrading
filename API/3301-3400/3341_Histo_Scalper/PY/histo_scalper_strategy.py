import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, RelativeStrengthIndex, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class histo_scalper_strategy(Strategy):
    def __init__(self):
        super(histo_scalper_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI period", "Indicators")

        self._macd = None
        self._rsi = None
        self._cci = None
        self._prev_macd = None

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def cci_period(self):
        return self._cci_period.Value

    def OnReseted(self):
        super(histo_scalper_strategy, self).OnReseted()
        self._macd = None
        self._rsi = None
        self._cci = None
        self._prev_macd = None

    def OnStarted2(self, time):
        super(histo_scalper_strategy, self).OnStarted2(time)

        self._macd = MovingAverageConvergenceDivergence()
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._macd, self._rsi, self._cci, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, macd_value, rsi_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._macd.IsFormed or not self._rsi.IsFormed or not self._cci.IsFormed:
            return

        macd_line = float(macd_value)
        rsi_val = float(rsi_value)
        cci_val = float(cci_value)

        if self._prev_macd is not None:
            if self._prev_macd <= 0.0 and macd_line > 0.0 and rsi_val < 70.0 and cci_val > -100.0 and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_macd >= 0.0 and macd_line < 0.0 and rsi_val > 30.0 and cci_val < 100.0 and self.Position >= 0:
                self.SellMarket()

        self._prev_macd = macd_line

    def CreateClone(self):
        return histo_scalper_strategy()
