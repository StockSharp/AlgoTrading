import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Array, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Messages import CandleStates, Unit
from datatype_extensions import *
from indicator_extensions import *

class heikin_ashi_v2_strategy(Strategy):
    """Heikin Ashi V2 strategy.

    Adds an EMA trend filter to the universal Heikin Ashi approach. Trades only
    when the HA candle direction aligns with the EMA trend.
    """

    def __init__(self):
        super(heikin_ashi_v2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ema_len = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Period of EMA filter", "Indicator")
        
        self._ema = ExponentialMovingAverage()
        
        # Heikin-Ashi state variables
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(heikin_ashi_v2_strategy, self).OnReseted()
        self._ema = ExponentialMovingAverage()
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0

    def OnStarted(self, time):
        super(heikin_ashi_v2_strategy, self).OnStarted(time)
        self._ema.Length = self._ema_len.Value
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._on_process).Start()

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        
        # Calculate Heikin-Ashi values
        if self._prev_ha_open == 0:
            # First candle
            ha_open = (candle.OpenPrice + candle.ClosePrice) / 2
            ha_close = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4
        else:
            # Calculate based on previous HA candle
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2
            ha_close = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4
        
        # Process EMA with regular candle close
        ema_val = process_float(self._ema, candle.ClosePrice, candle.ServerTime, True)
        
        if not self._ema.IsFormed:
            # Store current HA values for next candle
            self._prev_ha_open = ha_open
            self._prev_ha_close = ha_close
            return
        
        # Get EMA value
        ema_value = float(ema_val)
        
        # Check Heikin-Ashi candle direction and EMA filter
        if ha_close > ha_open and candle.ClosePrice > ema_value and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif ha_close < ha_open and candle.ClosePrice < ema_value and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Store current HA values for next candle
        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close

    def CreateClone(self):
        return heikin_ashi_v2_strategy()
