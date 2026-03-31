import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Strategies import Strategy


class pixel_art_strategy(Strategy):
    """Strategy that prints pixel art logos to the log."""

    def __init__(self):
        super(pixel_art_strategy, self).__init__()
        self._logo = self.Param("Logo", "pAulse") \
            .SetDisplay("Logo", "Pixel art logo to display", "General")

    @property
    def logo(self):
        return self._logo.Value

    def OnStarted2(self, time):
        super(pixel_art_strategy, self).OnStarted2(time)

        pixels = {
            "Pine": self._pine_array,
            "Bitcoin": self._bitcoin_array,
            "Heart": self._heart_array,
        }

        colors = pixels.get(str(self.logo))
        if colors is None:
            self.LogError("Unknown logo '{}'.".format(self.logo))
            return

        width = int(math.sqrt(len(colors)))
        for r in range(width):
            row = " ".join(colors[r * width: r * width + width])
            self.LogInfo(row)

    _pine_array = [
        "#058544", "#058544", "#058544", "#058544", "#058544",
        "#058544", "#058544", "#058544", "#058544", "#058544",
    ]

    _bitcoin_array = [
        "#000000", "#000000", "#feaf00", "#feaf00", "#000000",
    ]

    _heart_array = [
        "#000000", "#eb020a", "#eb020a", "#000000", "#000000",
    ]

    def CreateClone(self):
        return pixel_art_strategy()
