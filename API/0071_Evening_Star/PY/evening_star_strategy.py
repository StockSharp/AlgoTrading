import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class evening_star_strategy(Strategy):
    """
    Evening Star candle pattern strategy.
    Evening Star: 1st bullish, 2nd small body (doji), 3rd bearish closing below midpoint of 1st.
    Morning Star (reverse): 1st bearish, 2nd small body, 3rd bullish closing above midpoint of 1st.
    Uses SMA for exit signals.
    """

    def __init__(self):
        super(evening_star_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._bar1 = None
        self._bar2 = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(evening_star_strategy, self).OnReseted()
        self._bar1 = None
        self._bar2 = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(evening_star_strategy, self).OnStarted(time)

        self._bar1 = None
        self._bar2 = None
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._bar1 = self._bar2
            self._bar2 = candle
            return

        if self._bar1 is not None and self._bar2 is not None:
            first_body = abs(float(self._bar1.OpenPrice) - float(self._bar1.ClosePrice))
            second_body = abs(float(self._bar2.OpenPrice) - float(self._bar2.ClosePrice))
            second_small = first_body > 0 and second_body < first_body * 0.5
            first_mid = (float(self._bar1.HighPrice) + float(self._bar1.LowPrice)) / 2.0

            # Evening Star (bearish reversal) - primary
            first_bullish = self._bar1.ClosePrice > self._bar1.OpenPrice
            third_bearish = candle.ClosePrice < candle.OpenPrice
            evening_star = first_bullish and second_small and third_bearish and float(candle.ClosePrice) < first_mid

            # Morning Star (bullish reversal) - secondary
            first_bearish = self._bar1.ClosePrice < self._bar1.OpenPrice
            third_bullish = candle.ClosePrice > candle.OpenPrice
            morning_star = first_bearish and second_small and third_bullish and float(candle.ClosePrice) > first_mid

            sv = float(sma_val)
            close = float(candle.ClosePrice)
            cd = self._cooldown_bars.Value

            if self.Position == 0 and evening_star:
                self.SellMarket()
                self._cooldown = cd
            elif self.Position == 0 and morning_star:
                self.BuyMarket()
                self._cooldown = cd
            elif self.Position > 0 and close < sv:
                self.SellMarket()
                self._cooldown = cd
            elif self.Position < 0 and close > sv:
                self.BuyMarket()
                self._cooldown = cd

        self._bar1 = self._bar2
        self._bar2 = candle

    def CreateClone(self):
        return evening_star_strategy()
