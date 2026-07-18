import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, VolumeWeightedMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vwap_adx_strategy(Strategy):
    """
    Strategy based on VWAP and ADX indicators.
    Enters long when ADX crosses above 25 and price above VWAP.
    Enters short when ADX crosses above 25 and price below VWAP.
    Exits when ADX < 18.
    """

    def __init__(self):
        super(vwap_adx_strategy, self).__init__()

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop loss (%)", "Stop loss percentage from entry price", "Risk Management")

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for Average Directional Movement Index", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 25) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between entries", "General")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe of data for strategy", "General")

        self._prev_adx_value = 0.0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(vwap_adx_strategy, self).OnStarted2(time)
        self._prev_adx_value = 0.0
        self._cooldown = 0

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        vwap = VolumeWeightedMovingAverage()
        vwap.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(adx, vwap, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

    def OnReseted(self):
        super(vwap_adx_strategy, self).OnReseted()
        self._prev_adx_value = 0.0
        self._cooldown = 0

    def ProcessCandle(self, candle, adx_value, vwap_value):
        if candle.State != CandleStates.Finished:
            return

        # Get VWAP value
        vwap = float(vwap_value)

        # Get current ADX value
        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return
        current_adx_value = float(adx_ma)

        if self._cooldown > 0:
            self._cooldown -= 1

        adx_impulse_up = self._prev_adx_value <= 25 and current_adx_value > 25
        cooldown_val = int(self._cooldown_bars.Value)
        price = float(candle.ClosePrice)

        if self._cooldown == 0 and adx_impulse_up:
            if price > vwap * 1.001 and self.Position <= 0:
                self.BuyMarket()
                self._cooldown = cooldown_val
            elif price < vwap * 0.999 and self.Position >= 0:
                self.SellMarket()
                self._cooldown = cooldown_val
        elif current_adx_value < 18 and self.Position != 0:
            if self.Position > 0:
                self.SellMarket()
            else:
                self.BuyMarket()
            self._cooldown = cooldown_val

        self._prev_adx_value = current_adx_value

    def CreateClone(self):
        return vwap_adx_strategy()
