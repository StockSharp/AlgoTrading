import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class vidya_auto_trading_reversal_logic_strategy(Strategy):
    def __init__(self):
        super(vidya_auto_trading_reversal_logic_strategy, self).__init__()
        self._vidya_length = self.Param("VidyaLength", 10) \
            .SetDisplay("VIDYA Length", "Length of VIDYA", "General")
        self._vidya_momentum = self.Param("VidyaMomentum", 20) \
            .SetDisplay("Momentum Length", "Length for momentum", "General")
        self._band_distance = self.Param("BandDistance", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Band Distance", "ATR multiplier for bands", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._vidya = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0
        self._cooldown = 0

    @property
    def vidya_length(self):
        return self._vidya_length.Value

    @property
    def vidya_momentum(self):
        return self._vidya_momentum.Value

    @property
    def band_distance(self):
        return self._band_distance.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vidya_auto_trading_reversal_logic_strategy, self).OnReseted()
        self._vidya = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(vidya_auto_trading_reversal_logic_strategy, self).OnStarted(time)
        cmo = ChandeMomentumOscillator()
        cmo.Length = self.vidya_momentum
        atr = AverageTrueRange()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cmo, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, cmo_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown > 0:
            self._cooldown -= 1
        alpha = 2 / (self.vidya_length + 1)
        abs_cmo = abs(cmo_value)
        prev = (self._vidya if self._vidya is not None else candle.ClosePrice)
        self._vidya = alpha * abs_cmo / 100 * candle.ClosePrice + (1 - alpha * abs_cmo / 100) * prev
        upper = self._vidya + atr_value * self.band_distance
        lower = self._vidya - atr_value * self.band_distance
        if self._prev_close != 0 and self._cooldown <= 0:
            trend_cross_up = self._prev_close <= self._prev_upper and candle.ClosePrice > upper
            trend_cross_down = self._prev_close >= self._prev_lower and candle.ClosePrice < lower
            if trend_cross_up and self.Position <= 0:
                self.BuyMarket()
                self._cooldown = 25
            elif trend_cross_down and self.Position >= 0:
                self.SellMarket()
                self._cooldown = 25
        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_close = candle.ClosePrice

    def CreateClone(self):
        return vidya_auto_trading_reversal_logic_strategy()
