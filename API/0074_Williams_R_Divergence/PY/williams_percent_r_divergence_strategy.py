import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy

class williams_percent_r_divergence_strategy(Strategy):
    """
    Williams %R Divergence strategy.
    Detects divergences between price and Williams %R for reversal signals.
    Bullish: price falling but Williams %R rising (oversold zone).
    Bearish: price rising but Williams %R falling (overbought zone).
    """

    def __init__(self):
        super(williams_percent_r_divergence_strategy, self).__init__()
        self._wr_period = self.Param("WilliamsRPeriod", 14).SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_price = 0.0
        self._prev_wr = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_percent_r_divergence_strategy, self).OnReseted()
        self._prev_price = 0.0
        self._prev_wr = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(williams_percent_r_divergence_strategy, self).OnStarted(time)

        self._prev_price = 0.0
        self._prev_wr = 0.0
        self._cooldown = 0

        wr = WilliamsR()
        wr.Length = self._wr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, wr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        wv = float(wr_val)

        if self._prev_price == 0:
            self._prev_price = close
            self._prev_wr = wv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_price = close
            self._prev_wr = wv
            return

        cd = self._cooldown_bars.Value

        # Bullish divergence: price lower but WR higher (in oversold zone)
        bullish_div = close < self._prev_price and wv > self._prev_wr
        # Bearish divergence: price higher but WR lower (in overbought zone)
        bearish_div = close > self._prev_price and wv < self._prev_wr

        if self.Position == 0 and bullish_div and wv < -80:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and bearish_div and wv > -20:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and wv > -20:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and wv < -80:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_price = close
        self._prev_wr = wv

    def CreateClone(self):
        return williams_percent_r_divergence_strategy()
