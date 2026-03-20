import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hv_breakout_strategy(Strategy):
    """
    Strategy that trades breakouts based on historical volatility.
    Calculates price levels for breakouts using HV and enters positions
    when price breaks above or below those levels.
    """

    def __init__(self):
        super(hv_breakout_strategy, self).__init__()
        self._hv_period = self.Param("HvPeriod", 20).SetDisplay("HV Period", "Period for Historical Volatility calculation", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average calculation for exit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._reference_price = 0.0
        self._is_reference_set = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hv_breakout_strategy, self).OnReseted()
        self._reference_price = 0.0
        self._is_reference_set = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(hv_breakout_strategy, self).OnStarted(time)

        self._reference_price = 0.0
        self._is_reference_set = False
        self._cooldown = 0

        std_dev = StandardDeviation()
        std_dev.Length = self._hv_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(std_dev, sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, std_dev_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        hv = float(std_dev_val) / close if close > 0 else 0.0

        if not self._is_reference_set:
            self._reference_price = close
            self._is_reference_set = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        upper_breakout = self._reference_price * (1.0 + hv)
        lower_breakout = self._reference_price * (1.0 - hv)
        sv = float(sma_val)
        cd = self._cooldown_bars.Value

        if self.Position == 0:
            if close > upper_breakout:
                self.BuyMarket()
                self._cooldown = cd
                self._reference_price = close
            elif close < lower_breakout:
                self.SellMarket()
                self._cooldown = cd
                self._reference_price = close
        elif self.Position > 0:
            if close < sv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            if close > sv:
                self.BuyMarket()
                self._cooldown = cd

    def CreateClone(self):
        return hv_breakout_strategy()
