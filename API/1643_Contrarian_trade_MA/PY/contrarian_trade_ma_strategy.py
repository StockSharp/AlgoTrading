import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class contrarian_trade_ma_strategy(Strategy):
    def __init__(self):
        super(contrarian_trade_ma_strategy, self).__init__()
        self._calc_period = self.Param("CalcPeriod", 10) \
            .SetDisplay("Calc Period", "Lookback period for extremes", "General")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._prev_close = 0.0
        self._bars_in_position = 0
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    @property
    def calc_period(self):
        return self._calc_period.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(contrarian_trade_ma_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._bars_in_position = 0
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(contrarian_trade_ma_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.calc_period
        lowest = Lowest()
        lowest.Length = self.calc_period
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, highest, lowest, sma):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_close = candle.ClosePrice
            self._prev_highest = highest
            self._prev_lowest = lowest
            self._prev_sma = sma
            self._has_prev = True
            return
        if self.Position == 0:
            if self._prev_highest < self._prev_close and self.Position <= 0:
                self.BuyMarket()
                self._bars_in_position = 0
            elif self._prev_lowest > self._prev_close and self.Position >= 0:
                self.SellMarket()
                self._bars_in_position = 0
            elif self._prev_sma > candle.OpenPrice and self.Position <= 0:
                self.BuyMarket()
                self._bars_in_position = 0
            elif self._prev_sma < candle.OpenPrice and self.Position >= 0:
                self.SellMarket()
                self._bars_in_position = 0
        else:
            self._bars_in_position += 1
            if self._bars_in_position >= self.calc_period:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._bars_in_position = 0
        self._prev_close = candle.ClosePrice
        self._prev_highest = highest
        self._prev_lowest = lowest
        self._prev_sma = sma

    def CreateClone(self):
        return contrarian_trade_ma_strategy()
