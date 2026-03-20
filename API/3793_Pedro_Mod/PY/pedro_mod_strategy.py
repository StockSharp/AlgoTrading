import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class pedro_mod_strategy(Strategy):
    """Pedro Mod mean reversion strategy using Bollinger Bands.
    Buy when price touches the lower band, sell when price touches the upper band.
    Exit at the middle band."""

    def __init__(self):
        super(pedro_mod_strategy, self).__init__()

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators")
        self._bollinger_width = self.Param("BollingerWidth", 1.5) \
            .SetDisplay("Bollinger Width", "Standard deviation multiplier", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @property
    def BollingerWidth(self):
        return self._bollinger_width.Value

    def OnReseted(self):
        super(pedro_mod_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(pedro_mod_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self.BollingerPeriod
        bb.Width = self.BollingerWidth

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bb, self._process_candle).Start()

    def _process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not bb_value.IsFinal:
            return

        up_band = bb_value.UpBand if hasattr(bb_value, 'UpBand') else None
        low_band = bb_value.LowBand if hasattr(bb_value, 'LowBand') else None
        mid_band = bb_value.MovingAverage if hasattr(bb_value, 'MovingAverage') else None

        if up_band is None or low_band is None or mid_band is None:
            return

        upper = float(up_band)
        lower = float(low_band)
        middle = float(mid_band)
        close = float(candle.ClosePrice)

        # Buy when price touches lower band (mean reversion)
        if self.Position <= 0 and close <= lower:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell when price touches upper band
        elif self.Position >= 0 and close >= upper:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit at middle band
        elif self.Position > 0 and close >= middle:
            self.SellMarket()
        elif self.Position < 0 and close <= middle:
            self.BuyMarket()

    def CreateClone(self):
        return pedro_mod_strategy()
