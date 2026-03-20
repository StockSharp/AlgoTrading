import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ma_rsi_ea_strategy(Strategy):
    def __init__(self):
        super(ma_rsi_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "EMA period for trend", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 65.0) \
            .SetDisplay("Overbought", "RSI overbought level", "Levels")
        self._rsi_oversold = self.Param("RsiOversold", 35.0) \
            .SetDisplay("Oversold", "RSI oversold level", "Levels")

        self._prev_rsi = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    def OnReseted(self):
        super(ma_rsi_ea_strategy, self).OnReseted()
        self._prev_rsi = None

    def OnStarted(self, time):
        super(ma_rsi_ea_strategy, self).OnStarted(time)
        self._prev_rsi = None

        ema = ExponentialMovingAverage()
        ema.Length = self.MaPeriod
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

        close = float(candle.ClosePrice)
        ev = float(ema_value)

        if self._prev_rsi is None:
            self._prev_rsi = rv
            return

        ob = float(self.RsiOverbought)
        os_level = float(self.RsiOversold)

        # Buy: price above EMA and RSI crosses above oversold
        if close > ev and self._prev_rsi <= os_level and rv > os_level and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell: price below EMA and RSI crosses below overbought
        elif close < ev and self._prev_rsi >= ob and rv < ob and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_rsi = rv

    def CreateClone(self):
        return ma_rsi_ea_strategy()
