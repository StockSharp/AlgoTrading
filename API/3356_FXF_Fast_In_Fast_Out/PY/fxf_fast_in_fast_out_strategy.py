import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class fxf_fast_in_fast_out_strategy(Strategy):
    """
    FXF Fast In Fast Out: quick entry/exit using EMA and RSI.
    Buys when RSI crosses above 50 and close > EMA.
    Sells when RSI crosses below 50 and close < EMA.
    """

    def __init__(self):
        super(fxf_fast_in_fast_out_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ema_period = self.Param("EmaPeriod", 10) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 7) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")

        self._prev_rsi = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fxf_fast_in_fast_out_strategy, self).OnReseted()
        self._prev_rsi = None

    def OnStarted(self, time):
        super(fxf_fast_in_fast_out_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        ema = float(ema_val)
        rsi = float(rsi_val)

        if self._prev_rsi is not None:
            if self._prev_rsi <= 50 and rsi > 50 and close > ema and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_rsi >= 50 and rsi < 50 and close < ema and self.Position >= 0:
                self.SellMarket()

        self._prev_rsi = rsi

    def CreateClone(self):
        return fxf_fast_in_fast_out_strategy()
