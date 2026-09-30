import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class smc_strategy(Strategy):
    def __init__(self):
        super(smc_strategy, self).__init__()
        self._swing_high_length = self.Param("SwingHighLength", 8).SetGreaterThanZero()
        self._swing_low_length = self.Param("SwingLowLength", 8).SetGreaterThanZero()
        self._sma_length = self.Param("SmaLength", 50).SetGreaterThanZero()
        self._order_block_length = self.Param("OrderBlockLength", 20).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._candles = []

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(smc_strategy, self).OnReseted()
        self._candles = []

    def OnStarted2(self, time):
        super(smc_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._candles.append(candle)
        required = max(
            int(self._swing_high_length.Value),
            int(self._swing_low_length.Value),
            int(self._sma_length.Value),
            int(self._order_block_length.Value)) + 1

        if len(self._candles) > required:
            del self._candles[:-required]
        if len(self._candles) < required:
            return

        previous = self._candles[:-1]
        sh = int(self._swing_high_length.Value)
        sl = int(self._swing_low_length.Value)
        sma_len = int(self._sma_length.Value)
        ob_len = int(self._order_block_length.Value)

        swing_high = max(float(c.HighPrice) for c in previous[-sh:])
        swing_low = min(float(c.LowPrice) for c in previous[-sl:])
        sma = sum(float(c.ClosePrice) for c in self._candles[-sma_len:]) / sma_len

        order_block = previous[-ob_len:]
        support = min(float(c.LowPrice) for c in order_block)
        resistance = max(float(c.HighPrice) for c in order_block)

        close = float(candle.ClosePrice)
        has_support = float(candle.LowPrice) <= support and close >= support
        has_resistance = float(candle.HighPrice) >= resistance and close <= resistance

        signal = self.get_signal(close, swing_low, swing_high, sma, has_support, has_resistance)

        if signal > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif signal < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    @staticmethod
    def get_signal(price, swing_low, swing_high, sma, has_support, has_resistance):
        if swing_high <= swing_low:
            return 0
        equilibrium = (swing_high + swing_low) / 2.0
        if price <= equilibrium and price > sma and has_support:
            return 1
        if price >= equilibrium and price < sma and has_resistance:
            return -1
        return 0

    def CreateClone(self):
        return smc_strategy()
