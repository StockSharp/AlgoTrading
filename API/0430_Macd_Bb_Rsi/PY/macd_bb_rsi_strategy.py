import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Array, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, BollingerBands, RelativeStrengthIndex
from StockSharp.Messages import CandleStates
from datatype_extensions import *

class macd_bb_rsi_strategy(Strategy):
    """MACD + Bollinger Bands + RSI strategy."""

    def __init__(self):
        super(macd_bb_rsi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._macd_fast = self.Param("MacdFastLength", 12)
        self._macd_slow = self.Param("MacdSlowLength", 26)
        self._macd_signal = self.Param("MacdSignalLength", 9)
        self._bb_len = self.Param("BBLength", 20)
        self._bb_mult = self.Param("BBMultiplier", 2.0)
        self._rsi_len = self.Param("RSILength", 14)
        self._show_long = self.Param("ShowLong", True)
        self._show_short = self.Param("ShowShort", True)

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._boll = BollingerBands()
        self._rsi = RelativeStrengthIndex()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_bb_rsi_strategy, self).OnReseted()
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._boll = BollingerBands()
        self._rsi = RelativeStrengthIndex()

    def OnStarted(self, time):
        super(macd_bb_rsi_strategy, self).OnStarted(time)

        # Initialize MACD correctly - access through Macd.ShortMa/LongMa and SignalMa
        self._macd.Macd.ShortMa.Length = self._macd_fast.Value
        self._macd.Macd.LongMa.Length = self._macd_slow.Value
        self._macd.SignalMa.Length = self._macd_signal.Value

        self._boll.Length = self._bb_len.Value
        self._boll.Width = self._bb_mult.Value
        self._rsi.Length = self._rsi_len.Value

        sub = self.SubscribeCandles(self.candle_type)
        sub.BindEx(self._macd, self._boll, self._rsi, self._on_process).Start()

    def _on_process(self, candle, macd_val, boll_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Access MACD signal value correctly
        macd_value = float(macd_val.Signal) if macd_val.Signal is not None else 0.0
        upper = float(boll_val.UpBand) if boll_val.UpBand is not None else 0.0
        lower = float(boll_val.LowBand) if boll_val.LowBand is not None else 0.0
        rsi_value = float(rsi_val)
        close = candle.ClosePrice

        entry_long = macd_value > 0 and close < lower and rsi_value < 30
        entry_short = macd_value < 0 and close > upper and rsi_value > 70

        if entry_long and self._show_long.Value and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif entry_short and self._show_short.Value and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return macd_bb_rsi_strategy()
