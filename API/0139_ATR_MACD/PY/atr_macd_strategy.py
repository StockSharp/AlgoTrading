import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class atr_macd_strategy(Strategy):
    """
    ATR + MACD strategy.
    Uses ATR for volatility detection and MACD for trend direction.
    Enters when MACD confirms trend direction.
    """

    def __init__(self):
        super(atr_macd_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._atr_value = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_macd_strategy, self).OnReseted()
        self._atr_value = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(atr_macd_strategy, self).OnStarted2(time)

        self._atr_value = 0.0
        self._cooldown = 0

        atr = AverageTrueRange()
        atr.Length = 14

        macd = MovingAverageConvergenceDivergenceSignal()

        subscription = self.SubscribeCandles(self.candle_type)

        # Bind ATR to capture value
        subscription.BindEx(atr, self._on_atr)

        # Bind MACD for main logic
        subscription.BindEx(macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            atr_area = self.CreateChartArea()
            if atr_area is not None:
                self.DrawIndicator(atr_area, atr)
            macd_area = self.CreateChartArea()
            if macd_area is not None:
                self.DrawIndicator(macd_area, macd)

    def _on_atr(self, candle, atr_iv):
        if atr_iv.IsFormed:
            self._atr_value = float(atr_iv.Value)

    def _process_candle(self, candle, macd_iv):
        if candle.State != CandleStates.Finished:
            return

        if macd_iv.Macd is None or macd_iv.Signal is None:
            return

        macd_line = float(macd_iv.Macd)
        signal_line = float(macd_iv.Signal)
        cd = self._cooldown_bars.Value

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Entry: MACD bullish crossover
        if macd_line > signal_line and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        # Entry: MACD bearish crossover
        elif macd_line < signal_line and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit on MACD crossover against position
        if self.Position > 0 and macd_line < signal_line:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and macd_line > signal_line:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return atr_macd_strategy()
