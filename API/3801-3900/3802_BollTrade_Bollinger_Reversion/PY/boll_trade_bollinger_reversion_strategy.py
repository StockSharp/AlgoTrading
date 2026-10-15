import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class boll_trade_bollinger_reversion_strategy(Strategy):
    """Bollinger Bands reversion strategy.
    Buys when price closes below lower band, sells when above upper band."""

    def __init__(self):
        super(boll_trade_bollinger_reversion_strategy, self).__init__()

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bollinger_width = self.Param("BollingerWidth", 0.5) \
            .SetDisplay("BB Width", "Bollinger Bands width", "Indicators")
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
        super(boll_trade_bollinger_reversion_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(boll_trade_bollinger_reversion_strategy, self).OnStarted2(time)

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

        if up_band is None or low_band is None:
            return

        upper = float(up_band)
        lower = float(low_band)
        close = float(candle.ClosePrice)

        # Buy when close below lower band (oversold)
        if close < lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell when close above upper band (overbought)
        elif close > upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return boll_trade_bollinger_reversion_strategy()
