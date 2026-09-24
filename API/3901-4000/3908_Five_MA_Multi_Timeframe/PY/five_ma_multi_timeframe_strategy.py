import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class five_ma_multi_timeframe_strategy(Strategy):
    def __init__(self):
        super(five_ma_multi_timeframe_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._higher1 = self.Param("HigherTimeframe1", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._higher2 = self.Param("HigherTimeframe2", DataType.TimeFrame(TimeSpan.FromMinutes(240)))
        self._p1 = self.Param("FirstPeriod", 5).SetGreaterThanZero()
        self._p2 = self.Param("SecondPeriod", 8).SetGreaterThanZero()
        self._p3 = self.Param("ThirdPeriod", 13).SetGreaterThanZero()
        self._p4 = self.Param("FourthPeriod", 21).SetGreaterThanZero()
        self._p5 = self.Param("FifthPeriod", 34).SetGreaterThanZero()
        self._open_level = self.Param("OpenLevel", 0).SetNotNegative()
        self._close_level = self.Param("CloseLevel", 1).SetNotNegative()
        self._frames = {}

    def GetWorkingSecurities(self):
        return [
            (self.Security, self._candle_type.Value),
            (self.Security, self._higher1.Value),
            (self.Security, self._higher2.Value),
        ]

    def OnReseted(self):
        super(five_ma_multi_timeframe_strategy, self).OnReseted()
        self._frames = {}

    def OnStarted2(self, time):
        super(five_ma_multi_timeframe_strategy, self).OnStarted2(time)
        self._start_frame("primary", self._candle_type.Value)
        self._start_frame("higher1", self._higher1.Value)
        self._start_frame("higher2", self._higher2.Value)

    def _start_frame(self, key, candle_type):
        periods = [int(self._p1.Value), int(self._p2.Value), int(self._p3.Value), int(self._p4.Value), int(self._p5.Value)]
        state = {
            "periods": periods, "closes": [], "medians": [], "ao": [], "ac": [],
            "previous_mas": None, "bull": 0.0, "bear": 0.0, "ready": False
        }
        self._frames[key] = state

        def on_candle(candle):
            if candle.State != CandleStates.Finished:
                return
            self._process_frame(state, candle)
            self._evaluate()

        self.SubscribeCandles(candle_type).Bind(on_candle).Start()

    def _process_frame(self, state, candle):
        closes = state["closes"]
        medians = state["medians"]
        ao_values = state["ao"]
        ac_values = state["ac"]

        closes.append(float(candle.ClosePrice))
        medians.append((float(candle.HighPrice) + float(candle.LowPrice)) / 2.0)

        if len(medians) >= 34:
            ao = self._avg_tail(medians, 5) - self._avg_tail(medians, 34)
            ao_values.append(ao)
            if len(ao_values) >= 5:
                ac_values.append(ao - self._avg_tail(ao_values, 5))

        if len(closes) < max(state["periods"]):
            return

        mas = [self._avg_tail(closes, p) for p in state["periods"]]
        previous = state["previous_mas"]
        if previous is None:
            state["previous_mas"] = mas
            return

        bull = sum(1 for i in range(5) if mas[i] > previous[i])
        bear = sum(1 for i in range(5) if mas[i] < previous[i])

        if len(ac_values) >= 4:
            last4 = ac_values[-4:]
            if last4[0] < last4[1] < last4[2] < last4[3]:
                bull += 1
            elif last4[0] > last4[1] > last4[2] > last4[3]:
                bear += 1

        state["bull"] = bull / 6.0 * 100.0
        state["bear"] = bear / 6.0 * 100.0
        state["ready"] = True
        state["previous_mas"] = mas

        keep = max(max(state["periods"]), 40)
        if len(closes) > keep:
            del closes[:-keep]
        if len(medians) > 40:
            del medians[:-40]
        if len(ao_values) > 10:
            del ao_values[:-10]
        if len(ac_values) > 10:
            del ac_values[:-10]

    def _evaluate(self):
        if len(self._frames) != 3 or not all(f["ready"] for f in self._frames.values()):
            return

        bull_grade = min(self._grade(f["bull"]) for f in self._frames.values())
        bear_grade = min(self._grade(f["bear"]) for f in self._frames.values())
        close_level = int(self._close_level.Value)
        open_level = int(self._open_level.Value)

        if self.Position > 0:
            if bear_grade >= close_level:
                self.SellMarket(Math.Abs(self.Position))
            return

        if self.Position < 0:
            if bull_grade >= close_level:
                self.BuyMarket(Math.Abs(self.Position))
            return

        if bull_grade > open_level and bull_grade > bear_grade:
            self.BuyMarket()
        elif bear_grade > open_level and bear_grade > bull_grade:
            self.SellMarket()

    @staticmethod
    def _grade(score):
        return 2 if score > 75.0 else (1 if score > 50.0 else 0)

    @staticmethod
    def _avg_tail(values, count):
        return sum(values[-count:]) / float(count)

    def CreateClone(self):
        return five_ma_multi_timeframe_strategy()
