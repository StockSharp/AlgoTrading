import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class _kdj_state(object):
    def __init__(self):
        self.highs = []
        self.lows = []
        self.k = None
        self.d = None
        self.smooth_k = None
        self.smooth_d = None
        self.smooth_j = None
        self.count = 0


class adaptive_kdj_mtf_strategy(Strategy):
    """
    Adaptive KDJ (MTF) strategy.
    KDJ is calculated on TimeFrame1, TimeFrame2 and TimeFrame3, each line is smoothed by an EMA of SmoothingLength and the three
    timeframes are blended with the weights selected by WeightOption. An SMA of the blended J over TrendLength bars measures the trend:
    above 50 the oversold/overbought levels are 40/80, otherwise 20/60. A long opens when J is below the oversold level and K crosses
    above D, a short when J is above the overbought level and K crosses below D; the opposite signal reverses the position.
    """

    def __init__(self):
        super(adaptive_kdj_mtf_strategy, self).__init__()
        self._time_frame1 = self.Param("TimeFrame1", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Time Frame 1", "Shortest timeframe, also used for signals", "General")
        self._time_frame2 = self.Param("TimeFrame2", DataType.TimeFrame(TimeSpan.FromMinutes(3))).SetDisplay("Time Frame 2", "Middle timeframe", "General")
        self._time_frame3 = self.Param("TimeFrame3", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Time Frame 3", "Longest timeframe", "General")
        self._kdj_length = self.Param("KdjLength", 9).SetGreaterThanZero().SetDisplay("KDJ Length", "KDJ lookback", "KDJ")
        self._smoothing_length = self.Param("SmoothingLength", 5).SetGreaterThanZero().SetDisplay("Smoothing Length", "EMA length that smooths each timeframe's KDJ", "KDJ")
        self._trend_length = self.Param("TrendLength", 40).SetGreaterThanZero().SetDisplay("Trend Length", "SMA length of the trend strength", "KDJ")
        self._weight_option = self.Param("WeightOption", 1).SetRange(1, 3).SetDisplay("Weight Option", "1 favours the shortest timeframe, 2 is equal, 3 favours the longest", "KDJ")
        self._states = None
        self._trend = None
        self._prev_k = None
        self._prev_d = None

    def GetWorkingSecurities(self):
        result = []
        for dt in (self._time_frame1.Value, self._time_frame2.Value, self._time_frame3.Value):
            if all(not dt.Equals(existing) for _, existing in result):
                result.append((self.Security, dt))
        return result

    def OnReseted(self):
        super(adaptive_kdj_mtf_strategy, self).OnReseted()
        self._states = None
        self._trend = None
        self._prev_k = None
        self._prev_d = None

    def OnStarted2(self, time):
        super(adaptive_kdj_mtf_strategy, self).OnStarted2(time)

        self._states = [_kdj_state(), _kdj_state(), _kdj_state()]
        self._trend = SimpleMovingAverage()
        self._trend.Length = self._trend_length.Value
        self._prev_k = None
        self._prev_d = None

        main = self.SubscribeCandles(self._time_frame1.Value)
        main.Bind(self._process_main).Start()
        self.SubscribeCandles(self._time_frame2.Value).Bind(lambda c: self._update_kdj(self._states[1], c)).Start()
        self.SubscribeCandles(self._time_frame3.Value).Bind(lambda c: self._update_kdj(self._states[2], c)).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, main)
            self.DrawOwnTrades(area)

    def _update_kdj(self, state, candle):
        if candle.State != CandleStates.Finished:
            return

        length = self._kdj_length.Value
        state.highs.append(float(candle.HighPrice))
        state.lows.append(float(candle.LowPrice))

        if len(state.highs) > length:
            state.highs.pop(0)
            state.lows.pop(0)

        if len(state.highs) < length:
            return

        highest = max(state.highs)
        lowest = min(state.lows)
        close = float(candle.ClosePrice)
        rsv = 100.0 * (close - lowest) / (highest - lowest) if highest > lowest else 50.0

        # Classic KDJ: K and D are 1/3 smoothings of RSV and K.
        k = ((state.k if state.k is not None else 50.0) * 2.0 + rsv) / 3.0
        d = ((state.d if state.d is not None else 50.0) * 2.0 + k) / 3.0
        j = 3.0 * k - 2.0 * d
        state.k = k
        state.d = d

        alpha = 2.0 / (self._smoothing_length.Value + 1.0)
        state.smooth_k = k if state.smooth_k is None else state.smooth_k + alpha * (k - state.smooth_k)
        state.smooth_d = d if state.smooth_d is None else state.smooth_d + alpha * (d - state.smooth_d)
        state.smooth_j = j if state.smooth_j is None else state.smooth_j + alpha * (j - state.smooth_j)
        state.count += 1

    def _process_main(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_kdj(self._states[0], candle)

        smoothing = self._smoothing_length.Value
        if any(s.count < smoothing for s in self._states):
            return

        option = self._weight_option.Value
        if option == 2:
            w = (1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0)
        elif option == 3:
            w = (0.2, 0.3, 0.5)
        else:
            w = (0.5, 0.3, 0.2)

        s = self._states
        k = w[0] * s[0].smooth_k + w[1] * s[1].smooth_k + w[2] * s[2].smooth_k
        d = w[0] * s[0].smooth_d + w[1] * s[1].smooth_d + w[2] * s[2].smooth_d
        j = w[0] * s[0].smooth_j + w[1] * s[1].smooth_j + w[2] * s[2].smooth_j

        trend = float(process_float(self._trend, j, candle.ServerTime, True).GetValue[Decimal](None))

        prev_k = self._prev_k
        prev_d = self._prev_d
        self._prev_k = k
        self._prev_d = d

        if not self._trend.IsFormed or prev_k is None or prev_d is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        bullish = trend > 50.0
        buy_level = 40.0 if bullish else 20.0
        sell_level = 80.0 if bullish else 60.0

        cross_up = prev_k <= prev_d and k > d
        cross_down = prev_k >= prev_d and k < d

        if j < buy_level and cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif j > sell_level and cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return adaptive_kdj_mtf_strategy()
