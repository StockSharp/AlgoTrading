import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class low_vol_reversion_strategy(Strategy):
    """
    Low volatility mean reversion strategy.
    Trades when ATR is below average, expecting price to revert to MA.
    """

    def __init__(self):
        super(low_vol_reversion_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._atr_lookback = self.Param("AtrLookbackPeriod", 20).SetDisplay("ATR Lookback", "Lookback period for ATR average calculation", "Indicators")
        self._atr_threshold = self.Param("AtrThresholdPercent", 80.0).SetDisplay("ATR Threshold %", "ATR threshold as percentage of average ATR", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._avg_atr = 0.0
        self._lookback_counter = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(low_vol_reversion_strategy, self).OnReseted()
        self._avg_atr = 0.0
        self._lookback_counter = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(low_vol_reversion_strategy, self).OnStarted(time)

        self._avg_atr = 0.0
        self._lookback_counter = 0
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        sv = float(sma_val)
        av = float(atr_val)
        lb = self._atr_lookback.Value

        if self._lookback_counter < lb:
            if self._lookback_counter == 0:
                self._avg_atr = av
            else:
                self._avg_atr = (self._avg_atr * self._lookback_counter + av) / (self._lookback_counter + 1)
            self._lookback_counter += 1
            return
        else:
            self._avg_atr = (self._avg_atr * (lb - 1) + av) / lb

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        threshold = self._avg_atr * (float(self._atr_threshold.Value) / 100.0)
        is_low_vol = av < threshold
        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and is_low_vol:
            if close < sv:
                self.BuyMarket()
                self._cooldown = cd
            elif close > sv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if close > sv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            if close < sv:
                self.BuyMarket()
                self._cooldown = cd

    def CreateClone(self):
        return low_vol_reversion_strategy()
