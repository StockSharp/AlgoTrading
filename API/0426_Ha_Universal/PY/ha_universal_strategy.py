import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Array, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Messages import CandleStates, Unit, UnitTypes
from datatype_extensions import *
from indicator_extensions import *

class ha_universal_strategy(Strategy):
    """Heikin Ashi universal strategy.

    Converts standard candles to Heikin Ashi and uses SSL Channel for signals.
    Can be extended with additional filters.
    """

    def __init__(self):
        super(ha_universal_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._period = self.Param("Period", 3) \
            .SetDisplay("Period", "SSL period", "Strategy")
        self._stop_loss = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._take_profit = self.Param("TakeProfitPercent", 0.3) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
        
        # Indicators for SSL Channel
        self._sma_high = Highest()
        self._sma_low = Lowest()
        
        # Heikin-Ashi state variables
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        
        # SSL Channel state variables
        self._hlv = 0
        self._ssl_down = 0.0
        self._ssl_up = 0.0
        self._prev_ssl_up = 0.0
        self._prev_ssl_down = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ha_universal_strategy, self).OnReseted()
        self._sma_high = Highest()
        self._sma_low = Lowest()
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._hlv = 0
        self._ssl_down = 0.0
        self._ssl_up = 0.0
        self._prev_ssl_up = 0.0
        self._prev_ssl_down = 0.0

    def OnStarted(self, time):
        super(ha_universal_strategy, self).OnStarted(time)
        
        # Initialize indicators
        self._sma_high.Length = self._period.Value
        self._sma_low.Length = self._period.Value
        
        # Enable protection
        self.StartProtection(
            Unit(self._take_profit.Value, UnitTypes.Percent),
            Unit(self._stop_loss.Value, UnitTypes.Percent)
        )
        
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
            ha_high = candle.HighPrice
            ha_low = candle.LowPrice
        else:
            # Calculate based on previous HA candle
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2
            ha_close = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4
            ha_high = max(max(candle.HighPrice, ha_open), ha_close)
            ha_low = min(min(candle.LowPrice, ha_open), ha_close)
        
        # Process indicators with HA values
        high_val = process_float(self._sma_high, ha_high, candle.ServerTime, True)
        low_val = process_float(self._sma_low, ha_low, candle.ServerTime, True)
        
        if not self._sma_high.IsFormed or not self._sma_low.IsFormed:
            # Store current HA values for next candle
            self._prev_ha_open = ha_open
            self._prev_ha_close = ha_close
            return
        
        # Calculate SSL Channel
        sma_high_value = float(high_val)
        sma_low_value = float(low_val)
        
        # Update HLV (High-Low Value)
        if ha_close > sma_high_value:
            self._hlv = 1
        elif ha_close < sma_low_value:
            self._hlv = -1
        # else keep previous _hlv value
        
        # Calculate SSL lines
        self._ssl_down = sma_high_value if self._hlv < 0 else sma_low_value
        self._ssl_up = sma_low_value if self._hlv < 0 else sma_high_value
        
        # Check for crossovers
        bullish_cross = self._ssl_up > self._ssl_down and self._prev_ssl_up <= self._prev_ssl_down
        bearish_cross = self._ssl_down > self._ssl_up and self._prev_ssl_down <= self._prev_ssl_up
        
        # Execute trades
        if bullish_cross and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif bearish_cross and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Store current values for next candle
        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        self._prev_ssl_up = self._ssl_up
        self._prev_ssl_down = self._ssl_down

    def CreateClone(self):
        return ha_universal_strategy()
