import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class btfd_strategy(Strategy):
    """
    BTFD (buy the dip) strategy.
    Long only: when flat, buys a candle whose volume exceeds VolumeMultiplier times the SMA(VolumeLength) of volume while RSI(RsiLength)
    is below RsiOversold. Five take-profit levels Tp1..Tp5 percent above the entry each close Q1..Q5 percent of the position still
    open (Q5 = 100 closes the rest), and a StopLossPercent stop closes everything.
    """

    def __init__(self):
        super(btfd_strategy, self).__init__()
        self._volume_length = self.Param("VolumeLength", 70).SetGreaterThanZero().SetDisplay("Volume Length", "Period of the volume SMA", "Entry")
        self._volume_multiplier = self.Param("VolumeMultiplier", 2.5).SetGreaterThanZero().SetDisplay("Volume Multiplier", "Multiple of the volume SMA that marks a spike", "Entry")
        self._rsi_length = self.Param("RsiLength", 20).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Entry")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level below which the market is oversold", "Entry")
        self._tp1 = self.Param("Tp1", 0.4).SetNotNegative().SetDisplay("TP1 %", "First take profit", "Targets")
        self._tp2 = self.Param("Tp2", 0.6).SetNotNegative().SetDisplay("TP2 %", "Second take profit", "Targets")
        self._tp3 = self.Param("Tp3", 0.8).SetNotNegative().SetDisplay("TP3 %", "Third take profit", "Targets")
        self._tp4 = self.Param("Tp4", 1.0).SetNotNegative().SetDisplay("TP4 %", "Fourth take profit", "Targets")
        self._tp5 = self.Param("Tp5", 1.2).SetNotNegative().SetDisplay("TP5 %", "Fifth take profit", "Targets")
        self._q1 = self.Param("Q1", 20.0).SetRange(0.0, 100.0).SetDisplay("Q1 %", "Share of the open position closed at TP1", "Targets")
        self._q2 = self.Param("Q2", 40.0).SetRange(0.0, 100.0).SetDisplay("Q2 %", "Share of the open position closed at TP2", "Targets")
        self._q3 = self.Param("Q3", 60.0).SetRange(0.0, 100.0).SetDisplay("Q3 %", "Share of the open position closed at TP3", "Targets")
        self._q4 = self.Param("Q4", 80.0).SetRange(0.0, 100.0).SetDisplay("Q4 %", "Share of the open position closed at TP4", "Targets")
        self._q5 = self.Param("Q5", 100.0).SetRange(0.0, 100.0).SetDisplay("Q5 %", "Share of the open position closed at TP5", "Targets")
        self._stop_loss_percent = self.Param("StopLossPercent", 5.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss below the entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(3))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_sma = None
        self._entry_price = Decimal(0)
        self._next_target = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(btfd_strategy, self).OnReseted()
        self._volume_sma = None
        self._entry_price = Decimal(0)
        self._next_target = 0

    def OnStarted2(self, time):
        super(btfd_strategy, self).OnStarted2(time)

        self._entry_price = Decimal(0)
        self._next_target = 0
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_length.Value

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return

        volume_average = process_float(self._volume_sma, candle.TotalVolume, candle.ServerTime, True).GetValue[Decimal](None)

        if not self._volume_sma.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            self._manage_position(candle)
            return

        if self.Position == 0 and candle.TotalVolume > volume_average * Decimal(self._volume_multiplier.Value) and rsi < Decimal(self._rsi_oversold.Value):
            self.BuyMarket(self.Volume)
            self._entry_price = candle.ClosePrice
            self._next_target = 0

    def _manage_position(self, candle):
        if self._entry_price <= 0:
            return

        hundred = Decimal(100)
        stop_loss = Decimal(self._stop_loss_percent.Value)
        if stop_loss > 0 and candle.LowPrice <= self._entry_price * (Decimal(1) - stop_loss / hundred):
            self.SellMarket(self.Position)
            self._entry_price = Decimal(0)
            return

        targets = [self._tp1.Value, self._tp2.Value, self._tp3.Value, self._tp4.Value, self._tp5.Value]
        shares = [self._q1.Value, self._q2.Value, self._q3.Value, self._q4.Value, self._q5.Value]

        remaining = self.Position

        while self._next_target < len(targets) and remaining > 0:
            level = self._entry_price * (Decimal(1) + Decimal(targets[self._next_target]) / hundred)
            if candle.HighPrice < level:
                break

            is_last = self._next_target == len(targets) - 1
            quantity = remaining if is_last else self._round_volume(remaining * Decimal(shares[self._next_target]) / hundred)
            if quantity >= remaining:
                quantity = remaining

            if quantity > 0:
                self.SellMarket(quantity)
                remaining -= quantity

            self._next_target += 1

        if remaining <= 0:
            self._entry_price = Decimal(0)

    def _round_volume(self, volume):
        security = self.Security
        if security is not None and security.VolumeStep is not None and security.VolumeStep > 0:
            step = security.VolumeStep
            return Math.Floor(volume / step) * step
        return volume

    def CreateClone(self):
        return btfd_strategy()
