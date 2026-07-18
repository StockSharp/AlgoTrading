import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class stellar_lite_ict_ea_strategy(Strategy):
    """Stellar Lite ICT strategy with higher timeframe MA bias and market structure shift entries."""

    def __init__(self):
        super(stellar_lite_ict_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._higher_timeframe_type = self.Param("HigherTimeframeType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Higher Timeframe", "Timeframe used for directional bias", "General")
        self._higher_ma_period = self.Param("HigherMaPeriod", 20) \
            .SetDisplay("Higher MA Period", "Moving average length for higher timeframe bias", "Bias")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Average True Range lookback", "Volatility")

        self._last_htf_ma = None
        self._previous_htf_ma = None
        self._current_bias = None
        self._history = [None] * 20
        self._history_count = 0
        self._latest_atr = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def HigherTimeframeType(self):
        return self._higher_timeframe_type.Value

    @HigherTimeframeType.setter
    def HigherTimeframeType(self, value):
        self._higher_timeframe_type.Value = value

    @property
    def HigherMaPeriod(self):
        return self._higher_ma_period.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    def OnReseted(self):
        super(stellar_lite_ict_ea_strategy, self).OnReseted()
        self._last_htf_ma = None
        self._previous_htf_ma = None
        self._current_bias = None
        self._history = [None] * 20
        self._history_count = 0
        self._latest_atr = 0.0

    def OnStarted2(self, time):
        super(stellar_lite_ict_ea_strategy, self).OnStarted2(time)

        higher_ma = SimpleMovingAverage()
        higher_ma.Length = self.HigherMaPeriod

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        main_subscription = self.SubscribeCandles(self.CandleType)
        main_subscription.Bind(atr, self._process_main_candle).Start()

        higher_subscription = self.SubscribeCandles(self.HigherTimeframeType)
        higher_subscription.Bind(higher_ma, self._process_higher_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

    def _process_higher_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        self._previous_htf_ma = self._last_htf_ma
        self._last_htf_ma = float(ma_value)

        if self._previous_htf_ma is None or self._last_htf_ma is None:
            return

        prev = self._previous_htf_ma
        current = self._last_htf_ma
        close = float(candle.ClosePrice)

        if close > current and current > prev:
            self._current_bias = Sides.Buy
        elif close < current and current < prev:
            self._current_bias = Sides.Sell
        else:
            self._current_bias = None

    def _process_main_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        self._store_candle(candle)
        self._latest_atr = float(atr_value)

        if self.Position != 0:
            return

        if self._current_bias is None:
            return

        if self._history_count < 3:
            return

        prev = self._history[1]
        prev2 = self._history[2]
        if prev is None or prev2 is None:
            return

        bias = self._current_bias

        if bias == Sides.Buy:
            if float(candle.ClosePrice) > float(prev.HighPrice) and float(prev.ClosePrice) < float(prev2.OpenPrice):
                self.BuyMarket()
        else:
            if float(candle.ClosePrice) < float(prev.LowPrice) and float(prev.ClosePrice) > float(prev2.OpenPrice):
                self.SellMarket()

    def _store_candle(self, candle):
        i = len(self._history) - 1
        while i > 0:
            self._history[i] = self._history[i - 1]
            i -= 1
        self._history[0] = candle
        if self._history_count < len(self._history):
            self._history_count += 1

    def CreateClone(self):
        return stellar_lite_ict_ea_strategy()
