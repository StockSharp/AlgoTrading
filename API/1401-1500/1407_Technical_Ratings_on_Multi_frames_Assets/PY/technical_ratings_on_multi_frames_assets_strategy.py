import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class technical_ratings_on_multi_frames_assets_strategy(Strategy):
    def __init__(self):
        super(technical_ratings_on_multi_frames_assets_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 20).SetGreaterThanZero()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero()
        self._bull_rsi = self.Param("BullRsiThreshold", 55.0)
        self._bear_rsi = self.Param("BearRsiThreshold", 45.0)
        self._hourly = self.Param("HourlyCandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._four_hourly = self.Param("FourHourCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._daily = self.Param("DailyCandleType", DataType.TimeFrame(TimeSpan.FromDays(1)))
        self._ratings = {}

    def GetWorkingSecurities(self):
        return [
            (self.Security, self._hourly.Value),
            (self.Security, self._four_hourly.Value),
            (self.Security, self._daily.Value),
        ]

    def OnReseted(self):
        super(technical_ratings_on_multi_frames_assets_strategy, self).OnReseted()
        self._ratings = {}

    def OnStarted2(self, time):
        super(technical_ratings_on_multi_frames_assets_strategy, self).OnStarted2(time)
        self._start_rating("1h", self._hourly.Value)
        self._start_rating("4h", self._four_hourly.Value)
        self._start_rating("1d", self._daily.Value)

    def _start_rating(self, key, candle_type):
        sma = SimpleMovingAverage()
        sma.Length = int(self._sma_period.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = int(self._rsi_period.Value)

        def on_candle(candle, sma_value, rsi_value):
            if candle.State != CandleStates.Finished or not sma.IsFormed or not rsi.IsFormed:
                return

            close = float(candle.ClosePrice)
            sma_v = float(sma_value)
            rsi_v = float(rsi_value)
            ma_vote = 1.0 if close > sma_v else (-1.0 if close < sma_v else 0.0)
            bull = float(self._bull_rsi.Value)
            bear = float(self._bear_rsi.Value)
            rsi_vote = 1.0 if rsi_v >= bull else (-1.0 if rsi_v <= bear else 0.0)
            self._ratings[key] = (ma_vote + rsi_vote) / 2.0
            self._evaluate()

        self.SubscribeCandles(candle_type).Bind(sma, rsi, on_candle).Start()

    def _evaluate(self):
        if len(self._ratings) < 3:
            return

        average = (self._ratings["1h"] + self._ratings["4h"] + self._ratings["1d"]) / 3.0

        if average > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif average < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return technical_ratings_on_multi_frames_assets_strategy()
