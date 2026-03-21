import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_divergence_strategy(Strategy):
    """
    CCI Divergence strategy.
    Detects divergences between price and CCI for reversal signals.
    Bullish: price falling but CCI rising.
    Bearish: price rising but CCI falling.
    """

    def __init__(self):
        super(cci_divergence_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 14).SetDisplay("CCI Period", "Period for CCI", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_price = 0.0
        self._prev_cci = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_divergence_strategy, self).OnReseted()
        self._prev_price = 0.0
        self._prev_cci = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(cci_divergence_strategy, self).OnStarted(time)

        self._prev_price = 0.0
        self._prev_cci = 0.0
        self._cooldown = 0

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, cci_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        cv = float(cci_val)

        if self._prev_price == 0:
            self._prev_price = close
            self._prev_cci = cv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_price = close
            self._prev_cci = cv
            return

        cd = self._cooldown_bars.Value

        bullish_div = close < self._prev_price and cv > self._prev_cci
        bearish_div = close > self._prev_price and cv < self._prev_cci

        if self.Position == 0 and bullish_div and cv > -100:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and bearish_div and cv < 100:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and cv > 100:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and cv < -100:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_price = close
        self._prev_cci = cv

    def CreateClone(self):
        return cci_divergence_strategy()
