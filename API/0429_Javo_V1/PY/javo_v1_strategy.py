import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Array, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Messages import CandleStates
from datatype_extensions import *
from indicator_extensions import *

class javo_v1_strategy(Strategy):
    """Javo v1 strategy with Heikin Ashi candles and dual EMAs."""

    def __init__(self):
        super(javo_v1_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(60)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast = self.Param("FastEmaPeriod", 1) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
        self._slow = self.Param("SlowEmaPeriod", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
        
        self._fast_ema = ExponentialMovingAverage()
        self._slow_ema = ExponentialMovingAverage()
        
        # Heikin-Ashi state variables
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(javo_v1_strategy, self).OnReseted()
        self._fast_ema = ExponentialMovingAverage()
        self._slow_ema = ExponentialMovingAverage()
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0

    def OnStarted(self, time):
        super(javo_v1_strategy, self).OnStarted(time)
        self._fast_ema.Length = self._fast.Value
        self._slow_ema.Length = self._slow.Value
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
        
        # Process EMAs with HA close
        fast_val = process_float(self._fast_ema, ha_close, candle.ServerTime, True)
        slow_val = process_float(self._slow_ema, ha_close, candle.ServerTime, True)
        
        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed:
            # Store current HA values for next candle
            self._prev_ha_open = ha_open
            self._prev_ha_close = ha_close
            return
        
        # Get numeric values
        fast_ema = float(fast_val)
        slow_ema = float(slow_val)
        
        # Check for crossovers
        if fast_ema > slow_ema and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif fast_ema < slow_ema and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Store current HA values for next candle
        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close

    def CreateClone(self):
        return javo_v1_strategy()
