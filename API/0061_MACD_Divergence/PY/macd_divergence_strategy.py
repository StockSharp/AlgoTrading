import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_divergence_strategy(Strategy):
    """
    MACD Divergence strategy.
    Detects divergences between price and MACD for reversal signals.
    Bullish: price falling but MACD rising.
    Bearish: price rising but MACD falling.
    """

    def __init__(self):
        super(macd_divergence_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_price = 0.0
        self._prev_macd = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_divergence_strategy, self).OnReseted()
        self._prev_price = 0.0
        self._prev_macd = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(macd_divergence_strategy, self).OnStarted(time)

        self._prev_price = 0.0
        self._prev_macd = 0.0
        self._cooldown = 0

        macd = MovingAverageConvergenceDivergenceSignal()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed:
            return

        macd_line = macd_value.Macd
        signal = macd_value.Signal

        if macd_line is None or signal is None:
            return

        macd_f = float(macd_line)
        signal_f = float(signal)
        close = float(candle.ClosePrice)

        if self._prev_price == 0:
            self._prev_price = close
            self._prev_macd = macd_f
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_price = close
            self._prev_macd = macd_f
            return

        cd = self._cooldown_bars.Value

        # Bullish divergence: price down but MACD up
        bullish_div = close < self._prev_price and macd_f > self._prev_macd
        # Bearish divergence: price up but MACD down
        bearish_div = close > self._prev_price and macd_f < self._prev_macd

        if self.Position == 0 and bullish_div and macd_f > signal_f:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and bearish_div and macd_f < signal_f:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and macd_f < signal_f:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and macd_f > signal_f:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_price = close
        self._prev_macd = macd_f

    def CreateClone(self):
        return macd_divergence_strategy()
