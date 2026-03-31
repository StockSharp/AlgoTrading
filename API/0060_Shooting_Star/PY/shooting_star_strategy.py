import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class shooting_star_strategy(Strategy):
    """
    Shooting Star strategy.
    Enters short on shooting star pattern (long upper shadow, small lower shadow).
    Enters long on hammer pattern (long lower shadow, small upper shadow).
    Exits via SMA crossover.
    """

    def __init__(self):
        super(shooting_star_strategy, self).__init__()
        self._shadow_to_body_ratio = self.Param("ShadowToBodyRatio", 2.0).SetDisplay("Shadow/Body Ratio", "Min ratio of shadow to body", "Pattern")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(shooting_star_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted2(self, time):
        super(shooting_star_strategy, self).OnStarted2(time)

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

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        body_size = abs(float(candle.OpenPrice) - float(candle.ClosePrice))
        upper_shadow = float(candle.HighPrice) - max(float(candle.OpenPrice), float(candle.ClosePrice))
        lower_shadow = min(float(candle.OpenPrice), float(candle.ClosePrice)) - float(candle.LowPrice)

        ratio = float(self._shadow_to_body_ratio.Value)

        # Shooting star: long upper shadow, small lower shadow (bearish)
        is_shooting_star = body_size > 0 and upper_shadow > body_size * ratio and lower_shadow < body_size * 0.5
        # Hammer: long lower shadow, small upper shadow (bullish)
        is_hammer = body_size > 0 and lower_shadow > body_size * ratio and upper_shadow < body_size * 0.5

        close = float(candle.ClosePrice)
        sv = float(sma_val)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and is_shooting_star and close > sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position == 0 and is_hammer and close < sv:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return shooting_star_strategy()
