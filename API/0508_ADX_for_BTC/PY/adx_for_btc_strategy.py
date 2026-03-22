import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, SimpleMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class adx_for_btc_strategy(Strategy):
    def __init__(self):
        super(adx_for_btc_strategy, self).__init__()
        self._entry_level = self.Param("EntryLevel", 14.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Entry Level", "ADX threshold for entry", "Strategy")
        self._exit_level = self.Param("ExitLevel", 40.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Exit Level", "ADX threshold for exit", "Strategy")
        self._sma_length = self.Param("SmaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length", "Length for trend SMA", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_adx = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_for_btc_strategy, self).OnReseted()
        self._prev_adx = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adx_for_btc_strategy, self).OnStarted(time)
        adx = AverageDirectionalIndex()
        adx.Length = 14
        sma = SimpleMovingAverage()
        sma.Length = int(self._sma_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, sma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, adx_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return
        dx = adx_value.Dx
        di_plus_val = dx.Plus
        di_minus_val = dx.Minus
        if di_plus_val is None or di_minus_val is None:
            return

        adx_v = float(adx_ma)
        di_plus = float(di_plus_val)
        di_minus = float(di_minus_val)
        sma_v = float(IndicatorHelper.ToDecimal(sma_value))
        close = float(candle.ClosePrice)
        entry = float(self._entry_level.Value)
        exit_lv = float(self._exit_level.Value)
        cooldown = int(self._cooldown_bars.Value)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_adx = adx_v
            return

        if self._prev_adx > 0 and self._prev_adx <= entry and adx_v > entry and di_plus > di_minus and close > sma_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self._prev_adx > 0 and self._prev_adx <= entry and adx_v > entry and di_minus > di_plus and close < sma_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and adx_v < exit_lv and self._prev_adx >= exit_lv:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and adx_v < exit_lv and self._prev_adx >= exit_lv:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_adx = adx_v

    def CreateClone(self):
        return adx_for_btc_strategy()
