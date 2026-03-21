import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class double_channel_ea_strategy(Strategy):
    """
    Double Channel EA strategy: BB + EMA trend.
    Buys when close crosses above EMA. Sells when close crosses below EMA.
    """

    def __init__(self):
        super(double_channel_ea_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA period for trend", "Indicators")

        self._prev_close = None
        self._prev_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(double_channel_ea_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self._bb_period.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if ema_val.IsEmpty:
            return
        ema_decimal = float(ema_val.GetValue[float]())
        close = float(candle.ClosePrice)

        if self._prev_close is not None and self._prev_ema is not None:
            cross_above = self._prev_close <= self._prev_ema and close > ema_decimal
            cross_below = self._prev_close >= self._prev_ema and close < ema_decimal

            if cross_above and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif cross_below and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema_decimal

    def CreateClone(self):
        return double_channel_ea_strategy()
