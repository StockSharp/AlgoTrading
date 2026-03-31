import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class fifty_five_ma_bar_comparison_strategy(Strategy):
    def __init__(self):
        super(fifty_five_ma_bar_comparison_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 55) \
            .SetDisplay("MA Period", "Moving average period", "Indicators")

        self._prev_ma = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    def OnReseted(self):
        super(fifty_five_ma_bar_comparison_strategy, self).OnReseted()
        self._prev_ma = None

    def OnStarted2(self, time):
        super(fifty_five_ma_bar_comparison_strategy, self).OnStarted2(time)
        self._prev_ma = None

        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return
        mv = float(ma_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ma = mv
            return
        if self._prev_ma is None:
            self._prev_ma = mv
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        bullish_bar = close > open_price
        bearish_bar = close < open_price
        ma_rising = mv > self._prev_ma
        ma_falling = mv < self._prev_ma

        self._prev_ma = mv

        # Bullish bar + rising MA + close above MA
        if bullish_bar and ma_rising and close > mv and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Bearish bar + falling MA + close below MA
        elif bearish_bar and ma_falling and close < mv and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return fifty_five_ma_bar_comparison_strategy()
