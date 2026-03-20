import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class sweet_spot_extreme_strategy(Strategy):
    """EMA slope + CCI filter strategy. Buys when EMA slope is up and CCI is oversold.
    Sells when EMA slope is down and CCI is overbought. Exits on EMA slope reversal."""

    def __init__(self):
        super(sweet_spot_extreme_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "Trend EMA period", "Indicators")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._buy_cci_level = self.Param("BuyCciLevel", -50.0) \
            .SetDisplay("Buy CCI", "Oversold CCI level for buy", "Indicators")
        self._sell_cci_level = self.Param("SellCciLevel", 50.0) \
            .SetDisplay("Sell CCI", "Overbought CCI level for sell", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series", "General")

        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._has_prev_ema = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @property
    def BuyCciLevel(self):
        return self._buy_cci_level.Value

    @property
    def SellCciLevel(self):
        return self._sell_cci_level.Value

    def OnReseted(self):
        super(sweet_spot_extreme_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._has_prev_ema = False

    def OnStarted(self, time):
        super(sweet_spot_extreme_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, cci, self._process_candle).Start()

    def _process_candle(self, candle, ema_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        cci_val = float(cci_value)

        if not self._has_prev_ema:
            self._prev_prev_ema = ema_val
            self._prev_ema = ema_val
            self._has_prev_ema = True
            return

        slope_up = ema_val > self._prev_ema and self._prev_ema > self._prev_prev_ema
        slope_down = ema_val < self._prev_ema and self._prev_ema < self._prev_prev_ema

        buy_cci = float(self.BuyCciLevel)
        sell_cci = float(self.SellCciLevel)

        # Entry
        if slope_up and cci_val <= buy_cci and self.Position <= 0:
            self.BuyMarket()
        elif slope_down and cci_val >= sell_cci and self.Position >= 0:
            self.SellMarket()
        # Exit on slope reversal
        elif self.Position > 0 and ema_val < self._prev_ema:
            self.SellMarket()
        elif self.Position < 0 and ema_val > self._prev_ema:
            self.BuyMarket()

        self._prev_prev_ema = self._prev_ema
        self._prev_ema = ema_val

    def CreateClone(self):
        return sweet_spot_extreme_strategy()
