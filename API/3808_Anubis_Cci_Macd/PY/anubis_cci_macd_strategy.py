import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class anubis_cci_macd_strategy(Strategy):
    """Anubis CCI + MACD strategy.
    Buys when CCI crosses above 0 and MACD histogram is positive.
    Sells when CCI crosses below 0 and MACD histogram is negative."""

    def __init__(self):
        super(anubis_cci_macd_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI lookback period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_cci = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    def OnReseted(self):
        super(anubis_cci_macd_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(anubis_cci_macd_strategy, self).OnStarted2(time)

        self._has_prev = False

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod
        macd = MovingAverageConvergenceDivergenceSignal()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(cci, macd, self._process_candle).Start()

    def _process_candle(self, candle, cci_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not cci_value.IsFinal or not macd_value.IsFinal:
            return

        cci = float(cci_value)

        macd_raw = macd_value.Macd if hasattr(macd_value, 'Macd') else None
        signal_raw = macd_value.Signal if hasattr(macd_value, 'Signal') else None
        if macd_raw is None or signal_raw is None:
            return

        histogram = float(macd_raw) - float(signal_raw)

        if not self._has_prev:
            self._prev_cci = cci
            self._has_prev = True
            return

        # CCI crosses above 0 with bullish MACD
        if self._prev_cci <= 0 and cci > 0 and histogram > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # CCI crosses below 0 with bearish MACD
        elif self._prev_cci >= 0 and cci < 0 and histogram < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_cci = cci

    def CreateClone(self):
        return anubis_cci_macd_strategy()
