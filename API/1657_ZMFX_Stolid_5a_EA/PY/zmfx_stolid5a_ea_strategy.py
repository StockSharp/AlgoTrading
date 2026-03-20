import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class zmfx_stolid5a_ea_strategy(Strategy):
    def __init__(self):
        super(zmfx_stolid5a_ea_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 11) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._fast_ema_length = self.Param("FastEmaLength", 20) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", TimeSpan.FromHours(4)) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_rsi = 0.0
        self._has_prev_rsi = False

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def fast_ema_length(self):
        return self._fast_ema_length.Value

    @property
    def slow_ema_length(self):
        return self._slow_ema_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zmfx_stolid5a_ea_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev_rsi = False

    def OnStarted(self, time):
        super(zmfx_stolid5a_ea_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_ema_length
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_ema_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, fast_ema, slow_ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi, fast_ema, slow_ema):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev_rsi:
            self._prev_rsi = rsi
            self._has_prev_rsi = True
            return
        up_trend = fast_ema > slow_ema
        down_trend = fast_ema < slow_ema
        # Buy pullback: uptrend, RSI was oversold, now crossing up
        if up_trend and self._prev_rsi < 35 and rsi >= 35 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell pullback: downtrend, RSI was overbought, now crossing down
        elif down_trend and self._prev_rsi > 65 and rsi <= 65 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit long on RSI overbought or trend reversal
        if self.Position > 0 and (rsi > 75 or fast_ema < slow_ema):
            self.SellMarket()
        # Exit short on RSI oversold or trend reversal
        elif self.Position < 0 and (rsi < 25 or fast_ema > slow_ema):
            self.BuyMarket()
        self._prev_rsi = rsi

    def CreateClone(self):
        return zmfx_stolid5a_ea_strategy()
