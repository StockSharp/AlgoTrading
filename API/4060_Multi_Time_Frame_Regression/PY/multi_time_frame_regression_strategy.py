import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import LinearReg, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class multi_time_frame_regression_strategy(Strategy):
    """
    Multi Time Frame Regression: linear regression channel with highest/lowest boundaries.
    """

    def __init__(self):
        super(multi_time_frame_regression_strategy, self).__init__()
        self._regression_length = self.Param("RegressionLength", 20).SetDisplay("Regression", "LinReg period", "Indicators")
        self._channel_length = self.Param("ChannelLength", 20).SetDisplay("Channel", "Channel period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_lr = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_time_frame_regression_strategy, self).OnReseted()
        self._prev_lr = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(multi_time_frame_regression_strategy, self).OnStarted(time)
        lr = LinearReg()
        lr.Length = self._regression_length.Value
        highest = Highest()
        highest.Length = self._channel_length.Value
        lowest = Lowest()
        lowest.Length = self._channel_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(lr, highest, lowest, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, lr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, lr_val, high_val, low_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        lr = float(lr_val)
        h = float(high_val)
        l = float(low_val)
        close = float(candle.ClosePrice)
        slope = lr - self._prev_lr if self._has_prev else 0.0
        ch_mid = (h + l) / 2.0
        ch_width = h - l
        if ch_width <= 0:
            self._prev_lr = lr
            self._has_prev = True
            return
        upper = ch_mid + ch_width * 0.4
        lower = ch_mid - ch_width * 0.4
        if self.Position > 0 and (close >= upper or slope < 0):
            self.SellMarket()
        elif self.Position < 0 and (close <= lower or slope > 0):
            self.BuyMarket()
        if self.Position == 0:
            if close <= lower and slope >= 0:
                self.BuyMarket()
            elif close >= upper and slope <= 0:
                self.SellMarket()
        self._prev_lr = lr
        self._has_prev = True

    def CreateClone(self):
        return multi_time_frame_regression_strategy()
