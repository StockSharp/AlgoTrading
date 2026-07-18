import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class adx_macd_strategy(Strategy):
    """
    ADX + MACD strategy.
    Enters on MACD crossover when ADX indicates strong trend.
    """

    def __init__(self):
        super(adx_macd_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._adx_period = self.Param("AdxPeriod", 14).SetDisplay("ADX Period", "Period for ADX calculation", "ADX Settings")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetDisplay("ADX Threshold", "ADX threshold for trend strength", "ADX Settings")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._adx_value = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_macd_strategy, self).OnReseted()
        self._adx_value = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(adx_macd_strategy, self).OnStarted2(time)

        self._adx_value = 0.0
        self._cooldown = 0

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        macd = MovingAverageConvergenceDivergenceSignal()

        subscription = self.SubscribeCandles(self.candle_type)

        # Bind ADX with BindEx (composite indicator)
        subscription.BindEx(adx, self._on_adx)

        # Bind MACD for main logic
        subscription.BindEx(macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            adx_area = self.CreateChartArea()
            if adx_area is not None:
                self.DrawIndicator(adx_area, adx)
            macd_area = self.CreateChartArea()
            if macd_area is not None:
                self.DrawIndicator(macd_area, macd)

    def _on_adx(self, candle, adx_iv):
        if adx_iv.MovingAverage is not None:
            self._adx_value = float(adx_iv.MovingAverage)

    def _process_candle(self, candle, macd_iv):
        if candle.State != CandleStates.Finished:
            return

        if macd_iv.Macd is None or macd_iv.Signal is None:
            return

        macd_line = float(macd_iv.Macd)
        signal_line = float(macd_iv.Signal)
        cd = self._cooldown_bars.Value
        threshold = float(self._adx_threshold.Value)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        strong_trend = self._adx_value > threshold

        # Entry: strong trend + MACD bullish = buy
        if strong_trend and macd_line > signal_line and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        # Entry: strong trend + MACD bearish = sell
        elif strong_trend and macd_line < signal_line and self.Position == 0:
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
        return adx_macd_strategy()
