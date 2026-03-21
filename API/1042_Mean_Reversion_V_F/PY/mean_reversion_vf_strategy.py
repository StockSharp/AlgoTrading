import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class mean_reversion_vf_strategy(Strategy):
    """
    Mean Reversion VF: WMA deviation entry with take profit.
    """

    def __init__(self):
        super(mean_reversion_vf_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 24).SetDisplay("MA Length", "WMA length", "Indicators")
        self._deviation = self.Param("Deviation1", 0.5).SetDisplay("Deviation %", "Lower deviation from WMA", "Signals")
        self._take_profit_pct = self.Param("TakeProfitPercent", 4.0).SetDisplay("TP %", "Target profit percent", "Risk")
        self._cooldown_bars = self.Param("SignalCooldownBars", 80).SetDisplay("Cooldown", "Min bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._entry_price = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._bars_from_signal = 80

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mean_reversion_vf_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(mean_reversion_vf_strategy, self).OnStarted(time)
        wma = WeightedMovingAverage()
        wma.Length = self._ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return
        ma = float(ma_val)
        if ma == 0:
            return
        close = float(candle.ClosePrice)
        dev = float(self._deviation.Value)
        l1 = ma * (1.0 - dev / 100.0)
        self._bars_from_signal += 1
        tp_pct = float(self._take_profit_pct.Value) / 100.0
        if self.Position > 0 and self._entry_price > 0:
            tp_price = self._entry_price * (1.0 + tp_pct)
            if float(candle.HighPrice) >= tp_price:
                self.SellMarket()
                self._entry_price = 0.0
                self._prev_close = close
                return
        crossed_below = self._has_prev and self._prev_close >= l1 and close < l1
        if self._bars_from_signal >= self._cooldown_bars.Value and crossed_below and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._bars_from_signal = 0
        elif close > ma and self.Position > 0:
            self.SellMarket()
            self._entry_price = 0.0
        self._prev_close = close
        self._has_prev = True

    def CreateClone(self):
        return mean_reversion_vf_strategy()
