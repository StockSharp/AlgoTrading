import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Decimal, Math, TimeSpan, Convert, InvalidOperationException
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange, DecimalIndicatorValue, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates, OrderTypes, Sides, Unit, UnitTypes, ITickTradeMessage

class anomalous_holonomy_field_theory_strategy(Strategy):
    """Six-component signal with an entry-frozen ATR stop."""

    def __init__(self):
        super(anomalous_holonomy_field_theory_strategy, self).__init__()
        self._signalThreshold = self.Param("SignalThreshold", Decimal(2)).SetGreaterThanZero() \
            .SetDisplay("SignalThreshold", "Absolute composite threshold", "Parameters")
        self._cooldownBars = self.Param("CooldownBars", 50).SetNotNegative() \
            .SetDisplay("CooldownBars", "Minimum finished bars between entries", "Parameters")
        self._atrLength = self.Param("AtrLength", 14).SetGreaterThanZero() \
            .SetDisplay("AtrLength", "Wilder ATR window", "Parameters")
        self._atrStopMultiplier = self.Param("AtrStopMultiplier", Decimal(2)).SetGreaterThanZero() \
            .SetDisplay("AtrStopMultiplier", "Entry-frozen ATR stop multiple", "Parameters")
        self._rocPeriod = self.Param("RocPeriod", 10).SetGreaterThanZero() \
            .SetDisplay("RocPeriod", "Rate-of-change interval", "Parameters")
        self._vwapPeriod = self.Param("VwapPeriod", 20).SetGreaterThanZero() \
            .SetDisplay("VwapPeriod", "Volume-weighted typical-price window", "Parameters")
        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("CandleType", "Finished signal candle interval", "Parameters")
        self._clear_state()

    @property
    def SignalThreshold(self):
        return self._signalThreshold.Value

    @SignalThreshold.setter
    def SignalThreshold(self, value):
        self._signalThreshold.Value = value

    @property
    def CooldownBars(self):
        return self._cooldownBars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldownBars.Value = value

    @property
    def AtrLength(self):
        return self._atrLength.Value

    @AtrLength.setter
    def AtrLength(self, value):
        self._atrLength.Value = value

    @property
    def AtrStopMultiplier(self):
        return self._atrStopMultiplier.Value

    @AtrStopMultiplier.setter
    def AtrStopMultiplier(self, value):
        self._atrStopMultiplier.Value = value

    @property
    def RocPeriod(self):
        return self._rocPeriod.Value

    @RocPeriod.setter
    def RocPeriod(self, value):
        self._rocPeriod.Value = value

    @property
    def VwapPeriod(self):
        return self._vwapPeriod.Value

    @VwapPeriod.setter
    def VwapPeriod(self, value):
        self._vwapPeriod.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def _clear_state(self):
        self._closes = []
        self._vwap_bars = []
        self._pending = None
        self._bar = 0
        self._last_entry = None

    def OnReseted(self):
        super(anomalous_holonomy_field_theory_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        super(anomalous_holonomy_field_theory_strategy, self).OnStarted2(time)
        ema20 = ExponentialMovingAverage()
        ema20.Length = 20
        ema50 = ExponentialMovingAverage()
        ema50.Length = 50
        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = 12
        macd.Macd.LongMa.Length = 26
        macd.SignalMa.Length = 9
        atr = AverageTrueRange()
        atr.Length = self.AtrLength
        self._indicators = (ema20, ema50, rsi, macd, atr)
        candles = self.SubscribeCandles(self.CandleType)
        candles.BindEx(ema20, ema50, rsi, macd, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, ema20)
            self.DrawIndicator(area, ema50)
            self.DrawOwnTrades(area)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Start()

    @staticmethod
    def _clip(value, limit):
        return max(-limit, min(limit, value))

    def _process_candle(self, candle, fast_value, slow_value, rsi_value, macd_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        self._bar += 1
        self._closes.append(candle.ClosePrice)
        if len(self._closes) > self.RocPeriod + 1:
            self._closes.pop(0)
        self._vwap_bars.append(((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3), candle.TotalVolume))
        if len(self._vwap_bars) > self.VwapPeriod:
            self._vwap_bars.pop(0)
        if not all(i.IsFormed for i in self._indicators) or len(self._closes) <= self.RocPeriod or len(self._vwap_bars) < self.VwapPeriod or macd_value.Macd is None or macd_value.Signal is None:
            return
        atr = atr_value.GetValue[Decimal](None)
        volume = sum((b[1] for b in self._vwap_bars), Decimal(0))
        if atr <= 0 or volume <= 0 or self._closes[0] <= 0:
            return
        close = candle.ClosePrice
        fast = fast_value.GetValue[Decimal](None)
        slow = slow_value.GetValue[Decimal](None)
        rsi = rsi_value.GetValue[Decimal](None)
        composite = Decimal(1.5) if close > fast and close > slow else Decimal(1) if close > fast else Decimal(-1.5) if close < fast and close < slow else Decimal(-1) if close < fast else Decimal(0)
        vwap = sum((b[0] * b[1] for b in self._vwap_bars), Decimal(0)) / volume
        composite += self._clip((close - vwap) / atr, Decimal(2))
        if rsi < 30 and composite > 0:
            composite += Decimal(1.5)
        elif rsi < 40 and composite > 0:
            composite += Decimal(0.5)
        elif rsi > 70 and composite < 0:
            composite -= Decimal(1.5)
        elif rsi > 60 and composite < 0:
            composite -= Decimal(0.5)
        composite += self._clip((macd_value.Macd - macd_value.Signal) / atr, Decimal(1))
        composite += self._clip((close - self._closes[0]) / atr, Decimal(1))
        pending = self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)
        if self.Position != 0 or pending or self._last_entry is not None and self._bar - self._last_entry <= self.CooldownBars or not self.IsFormedAndOnlineAndAllowTrading() or abs(composite) < self.SignalThreshold:
            return
        self.StartProtection(Unit(), Unit(atr * self.AtrStopMultiplier, UnitTypes.Absolute), useMarketOrders=True, isLocalStop=True)
        self._last_entry = self._bar
        self._pending = self.CreateOrder(Sides.Buy if composite > 0 else Sides.Sell, Decimal(0), self.Volume)
        self._pending.Type = OrderTypes.Market
        self._pending.Comment = "0522Entry"
        self.RegisterOrder(self._pending)

    def CreateClone(self):
        return anomalous_holonomy_field_theory_strategy()
