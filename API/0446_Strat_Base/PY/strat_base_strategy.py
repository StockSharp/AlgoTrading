import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class strat_base_strategy(Strategy):
    """Strategy Base - EMA trend following strategy.
    Buys when price crosses above EMA, sells when price crosses below EMA.
    """

    def __init__(self):
        super(strat_base_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period", "Moving Averages")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(strat_base_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(strat_base_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema = float(ema_val)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = close
            self._prev_ema = ema
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            self._prev_ema = ema
            return

        if self._prev_close == 0.0 or self._prev_ema == 0.0:
            self._prev_close = close
            self._prev_ema = ema
            return

        # Price crosses above EMA
        cross_up = close > ema and self._prev_close <= self._prev_ema
        # Price crosses below EMA
        cross_down = close < ema and self._prev_close >= self._prev_ema

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

        self._prev_close = close
        self._prev_ema = ema

    def CreateClone(self):
        return strat_base_strategy()
