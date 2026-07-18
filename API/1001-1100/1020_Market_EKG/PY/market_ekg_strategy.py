import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class market_ekg_strategy(Strategy):
    """
    Market EKG: price deviation pattern detection on recent candles.
    """

    def __init__(self):
        super(market_ekg_strategy, self).__init__()
        self._deviation_threshold = self.Param("DeviationThresholdPercent", 0.15).SetDisplay("Deviation %", "Min deviation pct", "Signals")
        self._cooldown_bars = self.Param("CooldownBars", 16).SetDisplay("Cooldown", "Bars between signals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candles", "General")

        self._prev1 = None
        self._prev2 = None
        self._prev3 = None
        self._bars_from_signal = 16

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(market_ekg_strategy, self).OnReseted()
        self._prev1 = None
        self._prev2 = None
        self._prev3 = None
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted2(self, time):
        super(market_ekg_strategy, self).OnStarted2(time)
        self._bars_from_signal = self._cooldown_bars.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self._prev1 is not None and self._prev2 is not None and self._prev3 is not None:
            avg_close = (float(self._prev3.ClosePrice) + float(self._prev2.ClosePrice)) / 2.0
            diff_close = avg_close - float(self._prev1.ClosePrice)
            base_price = float(self._prev1.ClosePrice)
            diff_pct = abs(diff_close) / base_price * 100.0 if base_price != 0 else 0.0
            self._bars_from_signal += 1
            can_signal = self._bars_from_signal >= self._cooldown_bars.Value
            threshold = float(self._deviation_threshold.Value)
            if can_signal and diff_close > 0 and diff_pct >= threshold and self.Position <= 0:
                self.BuyMarket()
                self._bars_from_signal = 0
            elif can_signal and diff_close < 0 and diff_pct >= threshold and self.Position >= 0:
                self.SellMarket()
                self._bars_from_signal = 0
        self._prev3 = self._prev2
        self._prev2 = self._prev1
        self._prev1 = candle

    def CreateClone(self):
        return market_ekg_strategy()
