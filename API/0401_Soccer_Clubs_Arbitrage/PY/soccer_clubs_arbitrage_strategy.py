import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class soccer_clubs_arbitrage_strategy(Strategy):
    """Arbitrage strategy for two share classes of the same soccer club."""

    def __init__(self):
        super(soccer_clubs_arbitrage_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Second Security Id", "Identifier of the second security", "General")

        self._entry = self.Param("EntryThreshold", 0.005) \
            .SetDisplay("Entry Threshold", "Premium difference to open position", "Parameters")

        self._exit = self.Param("ExitThreshold", 0.001) \
            .SetDisplay("Exit Threshold", "Premium difference to close position", "Parameters")

        self._cooldown_bars = self.Param("CooldownBars", 5) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._second_security = None
        self._price_a = 0.0
        self._price_b = 0.0
        self._primary_updated = False
        self._second_updated = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(soccer_clubs_arbitrage_strategy, self).OnReseted()
        self._second_security = None
        self._price_a = 0.0
        self._price_b = 0.0
        self._primary_updated = False
        self._second_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(soccer_clubs_arbitrage_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Second security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._second_security = s

        primary_sub = self.SubscribeCandles(self.candle_type, True, self.Security)
        second_sub = self.SubscribeCandles(self.candle_type, True, self._second_security)

        primary_sub.Bind(self._process_primary_candle).Start()
        second_sub.Bind(self._process_second_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary_sub)
            self.DrawOwnTrades(area)

    def _process_primary_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._price_a = float(candle.ClosePrice)
        self._primary_updated = True
        self._try_evaluate()

    def _process_second_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._price_b = float(candle.ClosePrice)
        self._second_updated = True
        self._try_evaluate()

    def _try_evaluate(self):
        if not self._primary_updated or not self._second_updated:
            return

        if self._price_a <= 0 or self._price_b <= 0:
            return

        self._primary_updated = False
        self._second_updated = False

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        premium = self._price_a / self._price_b - 1.0
        entry_thresh = float(self._entry.Value)
        exit_thresh = float(self._exit.Value)
        cooldown = int(self._cooldown_bars.Value)

        primary_pos = self.GetPositionValue(self.Security, self.Portfolio)
        if primary_pos is None:
            primary_pos = 0
        primary_pos = float(primary_pos)

        if abs(premium) < exit_thresh and primary_pos != 0:
            self._flatten(primary_pos)
            self._cooldown_remaining = cooldown
            return

        if premium > entry_thresh and primary_pos >= 0:
            if primary_pos > 0:
                self._flatten(primary_pos)
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif premium < -entry_thresh and primary_pos <= 0:
            if primary_pos < 0:
                self._flatten(primary_pos)
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown

    def _flatten(self, primary_pos):
        if primary_pos > 0:
            self.SellMarket(primary_pos)
        elif primary_pos < 0:
            self.BuyMarket(abs(primary_pos))

    def CreateClone(self):
        return soccer_clubs_arbitrage_strategy()
