import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, AwesomeOscillator
from StockSharp.Algo.Strategies import Strategy

class momentum_alligator_4h_bitcoin_strategy(Strategy):
    """
    Momentum Alligator 4h Bitcoin: AO zero-line cross with Alligator alignment and stop-loss.
    """

    def __init__(self):
        super(momentum_alligator_4h_bitcoin_strategy, self).__init__()
        self._stop_loss_pct = self.Param("StopLossPercent", 0.02).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._cooldown_bars = self.Param("SignalCooldownBars", 2).SetDisplay("Cooldown", "Min bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_ao = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._bars_from_signal = 2

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(momentum_alligator_4h_bitcoin_strategy, self).OnReseted()
        self._prev_ao = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(momentum_alligator_4h_bitcoin_strategy, self).OnStarted(time)
        jaw = SmoothedMovingAverage()
        jaw.Length = 13
        teeth = SmoothedMovingAverage()
        teeth.Length = 8
        lips = SmoothedMovingAverage()
        lips.Length = 5
        ao = AwesomeOscillator()
        ao.ShortMa.Length = 5
        ao.LongMa.Length = 34
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ao, jaw, teeth, lips, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ao_val, jaw_val, teeth_val, lips_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        ao = float(ao_val)
        jaw = float(jaw_val)
        teeth = float(teeth_val)
        lips = float(lips_val)
        self._bars_from_signal += 1
        if self._has_prev:
            ao_cross_up = self._prev_ao <= 0.0 and ao > 0.0
            ao_cross_down = self._prev_ao >= 0.0 and ao < 0.0
            alligator_bull = lips > teeth and close > jaw
            alligator_bear = lips < teeth and close < jaw
            if self._bars_from_signal >= self._cooldown_bars.Value and ao_cross_up and alligator_bull and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
                self._bars_from_signal = 0
            if self._bars_from_signal >= self._cooldown_bars.Value and ao_cross_down and alligator_bear and self.Position >= 0:
                self.SellMarket()
                self._entry_price = close
                self._bars_from_signal = 0
        sl_pct = float(self._stop_loss_pct.Value)
        if self.Position > 0 and self._entry_price > 0:
            if close < self._entry_price * (1.0 - sl_pct) or close < teeth:
                self.SellMarket()
        elif self.Position < 0 and self._entry_price > 0:
            if close > self._entry_price * (1.0 + sl_pct) or close > teeth:
                self.BuyMarket()
        self._prev_ao = ao
        self._has_prev = True

    def CreateClone(self):
        return momentum_alligator_4h_bitcoin_strategy()
