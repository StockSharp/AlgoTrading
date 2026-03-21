import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_cci_divergence_strategy(Strategy):
    def __init__(self):
        super(rsi_cci_divergence_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")

        self._prev_rsi = None
        self._prev_cci = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def RsiPeriod(self):
        return self._rsi_period.Value
    @property
    def CciPeriod(self):
        return self._cci_period.Value

    def OnReseted(self):
        super(rsi_cci_divergence_strategy, self).OnReseted()
        self._prev_rsi = None
        self._prev_cci = None

    def OnStarted(self, time):
        super(rsi_cci_divergence_strategy, self).OnStarted(time)
        self._prev_rsi = None
        self._prev_cci = None
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, cci, self._on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value, cci_value):
        if candle.State != CandleStates.Finished:
            return
        rv = float(rsi_value)
        cv = float(cci_value)
        if self._prev_rsi is None or self._prev_cci is None:
            self._prev_rsi = rv
            self._prev_cci = cv
            return
        buy_signal = (self._prev_rsi < 30.0 and rv >= 30.0) or (self._prev_cci < -100.0 and cv >= -100.0)
        sell_signal = (self._prev_rsi > 70.0 and rv <= 70.0) or (self._prev_cci > 100.0 and cv <= 100.0)
        self._prev_rsi = rv
        self._prev_cci = cv
        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return rsi_cci_divergence_strategy()
