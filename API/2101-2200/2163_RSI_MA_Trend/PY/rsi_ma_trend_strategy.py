import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class rsi_ma_trend_strategy(Strategy):
    def __init__(self):
        super(rsi_ma_trend_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 21) \
            .SetDisplay("RSI Period", "Length of RSI indicator", "Indicators")
        self._rsi_buy_level = self.Param("RsiBuyLevel", 45.0) \
            .SetDisplay("RSI Buy Level", "Value below which long is opened", "Indicators")
        self._rsi_sell_level = self.Param("RsiSellLevel", 55.0) \
            .SetDisplay("RSI Sell Level", "Value above which short is opened", "Indicators")
        self._fast_ma_period = self.Param("FastMaPeriod", 10) \
            .SetDisplay("Fast MA Period", "Length of fast moving average", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 50) \
            .SetDisplay("Slow MA Period", "Length of slow moving average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_buy_level(self):
        return self._rsi_buy_level.Value

    @property
    def rsi_sell_level(self):
        return self._rsi_sell_level.Value

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value

    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_ma_trend_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(rsi_ma_trend_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_ma_period
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, fast_ma, slow_ma, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value, fast_ma_value, slow_ma_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        fast_val = float(fast_ma_value)
        slow_val = float(slow_ma_value)
        is_up_trend = fast_val > slow_val

        if rsi_val < float(self.rsi_buy_level) and is_up_trend and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif rsi_val > float(self.rsi_sell_level) and not is_up_trend and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return rsi_ma_trend_strategy()
