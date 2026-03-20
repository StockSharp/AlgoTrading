import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class one_hour_eur_usd_strategy(Strategy):
    def __init__(self):
        super(one_hour_eur_usd_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Trend EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")

        self._prev_rsi = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    def OnReseted(self):
        super(one_hour_eur_usd_strategy, self).OnReseted()
        self._prev_rsi = None

    def OnStarted(self, time):
        super(one_hour_eur_usd_strategy, self).OnStarted(time)
        self._prev_rsi = None

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rv = float(rsi_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            return
        if self._prev_rsi is None:
            self._prev_rsi = rv
            return

        close = float(candle.ClosePrice)
        ev = float(ema_value)

        # Price above EMA + RSI crosses 50 up
        if close > ev and self._prev_rsi <= 50 and rv > 50 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Price below EMA + RSI crosses 50 down
        elif close < ev and self._prev_rsi >= 50 and rv < 50 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_rsi = rv

    def CreateClone(self):
        return one_hour_eur_usd_strategy()
