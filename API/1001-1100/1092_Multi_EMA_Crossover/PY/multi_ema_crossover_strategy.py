import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class multi_ema_crossover_strategy(Strategy):
    """
    Multi EMA crossover strategy.
    Four EMA pairs (EMA1/EMA5, EMA3/EMA10, EMA5/EMA20, EMA10/EMA40) trade independently: each pair adds one long lot when its
    fast EMA crosses above its slow EMA and removes that lot when the fast EMA falls below the slow EMA. Long only, no stops.
    """

    def __init__(self):
        super(multi_ema_crossover_strategy, self).__init__()
        self._ema1 = self.Param("EMA1", 1).SetGreaterThanZero().SetDisplay("EMA1", "Fast EMA of the 1/5 pair", "Indicators")
        self._ema3 = self.Param("EMA3", 3).SetGreaterThanZero().SetDisplay("EMA3", "Fast EMA of the 3/10 pair", "Indicators")
        self._ema5 = self.Param("EMA5", 5).SetGreaterThanZero().SetDisplay("EMA5", "Slow EMA of the 1/5 pair and fast EMA of the 5/20 pair", "Indicators")
        self._ema10 = self.Param("EMA10", 10).SetGreaterThanZero().SetDisplay("EMA10", "Slow EMA of the 3/10 pair and fast EMA of the 10/40 pair", "Indicators")
        self._ema20 = self.Param("EMA20", 20).SetGreaterThanZero().SetDisplay("EMA20", "Slow EMA of the 5/20 pair", "Indicators")
        self._ema40 = self.Param("EMA40", 40).SetGreaterThanZero().SetDisplay("EMA40", "Slow EMA of the 10/40 pair", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = [None] * 4
        self._prev_slow = [None] * 4
        self._is_open = [False] * 4

    def OnReseted(self):
        super(multi_ema_crossover_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(multi_ema_crossover_strategy, self).OnStarted2(time)

        self._reset_state()

        emas = []
        for p in (self._ema1, self._ema3, self._ema5, self._ema10, self._ema20, self._ema40):
            ema = ExponentialMovingAverage()
            ema.Length = p.Value
            emas.append(ema)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(emas[0], emas[1], emas[2], emas[3], emas[4], emas[5], self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            for ema in emas:
                self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema1, ema3, ema5, ema10, ema20, ema40):
        if candle.State != CandleStates.Finished:
            return

        fast = (ema1, ema3, ema5, ema10)
        slow = (ema5, ema10, ema20, ema40)

        can_trade = self.IsFormedAndOnlineAndAllowTrading()

        for i in range(4):
            prev_fast = self._prev_fast[i]
            prev_slow = self._prev_slow[i]

            self._prev_fast[i] = fast[i]
            self._prev_slow[i] = slow[i]

            if not can_trade:
                continue

            # Each pair owns one lot of the combined long position.
            if self._is_open[i]:
                if fast[i] < slow[i]:
                    self.SellMarket(self.Volume)
                    self._is_open[i] = False
            elif prev_fast is not None and prev_slow is not None and prev_fast <= prev_slow and fast[i] > slow[i]:
                self.BuyMarket(self.Volume)
                self._is_open[i] = True

    def CreateClone(self):
        return multi_ema_crossover_strategy()
