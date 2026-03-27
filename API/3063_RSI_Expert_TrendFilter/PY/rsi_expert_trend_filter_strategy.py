import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class rsi_expert_trend_filter_strategy(Strategy):
    def __init__(self):
        super(rsi_expert_trend_filter_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe for generating signals", "General")
        self._rsi_period = self.Param("RsiPeriod", 21) \
            .SetDisplay("RSI Period", "Averaging period for RSI", "Indicators")
        self._fast_ma_period = self.Param("FastMaPeriod", 50) \
            .SetDisplay("Fast MA Period", "Period of the fast moving average", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 200) \
            .SetDisplay("Slow MA Period", "Period of the slow moving average", "Indicators")
        self._rsi_level_up = self.Param("RsiLevelUp", 70.0) \
            .SetDisplay("RSI Level Up", "Upper RSI threshold for shorts", "Indicators")
        self._rsi_level_down = self.Param("RsiLevelDown", 30.0) \
            .SetDisplay("RSI Level Down", "Lower RSI threshold for longs", "Indicators")

        self._previous_rsi = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def RsiPeriod(self):
        return self._rsi_period.Value
    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value
    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value
    @property
    def RsiLevelUp(self):
        return self._rsi_level_up.Value
    @property
    def RsiLevelDown(self):
        return self._rsi_level_down.Value

    def OnReseted(self):
        super(rsi_expert_trend_filter_strategy, self).OnReseted()
        self._previous_rsi = None

    def OnStarted(self, time):
        super(rsi_expert_trend_filter_strategy, self).OnStarted(time)
        self._previous_rsi = None
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.SlowMaPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, fast_ma, slow_ma, self._on_process).Start()
        self.StartProtection(Unit(2, UnitTypes.Percent), Unit(1, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_value)
        fv = float(fast_value)
        sv = float(slow_value)

        if self.Position != 0:
            self._previous_rsi = rv
            return

        rsi_signal = 0
        if self._previous_rsi is not None:
            if rv > float(self.RsiLevelDown) and self._previous_rsi < float(self.RsiLevelDown):
                rsi_signal = 1
            elif rv < float(self.RsiLevelUp) and self._previous_rsi > float(self.RsiLevelUp):
                rsi_signal = -1

        ma_signal = 0
        if fv > sv:
            ma_signal = 1
        elif fv < sv:
            ma_signal = -1

        final_signal = 0
        if rsi_signal == 1 and ma_signal == 1:
            final_signal = 1
        elif rsi_signal == -1 and ma_signal == -1:
            final_signal = -1

        if final_signal > 0:
            self.BuyMarket()
        elif final_signal < 0:
            self.SellMarket()

        self._previous_rsi = rv

    def CreateClone(self):
        return rsi_expert_trend_filter_strategy()
