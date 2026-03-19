import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import LinearReg, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class m_trend_line_strategy(Strategy):
    """
    Trend line strategy using linear regression slope for direction.
    """

    def __init__(self):
        super(m_trend_line_strategy, self).__init__()
        self._regression_length = self.Param("RegressionLength", 20) \
            .SetDisplay("Regression Length", "Linear regression length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_slope = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(m_trend_line_strategy, self).OnReseted()
        self._prev_slope = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(m_trend_line_strategy, self).OnStarted(time)
        lr = LinearReg()
        lr.Length = self._regression_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._regression_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(lr, ema, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, lr)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, lr_val, ema_val):
        if candle.State != CandleStates.Finished:
            return
        lr = float(lr_val)
        ema = float(ema_val)
        slope = lr - ema
        if not self._has_prev:
            self._prev_slope = slope
            self._has_prev = True
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_slope = slope
            return
        close = float(candle.ClosePrice)
        if self.Position > 0 and slope < 0 and self._prev_slope >= 0:
            self.SellMarket()
        elif self.Position < 0 and slope > 0 and self._prev_slope <= 0:
            self.BuyMarket()
        if self.Position == 0:
            if slope > 0 and self._prev_slope <= 0 and close > lr:
                self.BuyMarket()
            elif slope < 0 and self._prev_slope >= 0 and close < lr:
                self.SellMarket()
        self._prev_slope = slope

    def CreateClone(self):
        return m_trend_line_strategy()
