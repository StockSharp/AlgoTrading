# Durchschnittliche Candle-Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie bildet den Experten „Durchschnittliches Kerzenkreuz“ MetaTrader nach. Es wartet auf einen abgeschlossenen Balken, bei dem die vorherige Kerze über einem gleitenden Durchschnitt schloss, während zwei zusätzliche Filter für gleitende Durchschnitte bereits den vorherrschenden Trend bestätigen. Es kann jeweils nur eine Position aktiv sein. Unmittelbar nach der Eröffnung eines Handels fügt der Algorithmus einen Stop-Loss und einen Take-Profit hinzu, deren Abstand vom angegebenen Pip-basierten Stop abgeleitet wird. Dadurch ist das Verhalten identisch mit der ursprünglichen Blocklogik, die einmal pro Takt ausgelöst wird.

Die Einstiegslogik liest historische Balkendaten statt unvollendeter Ticks, sodass alle Signale am Ende der zuletzt abgeschlossenen Kerze ausgewertet werden. Separate Parametersätze steuern die bullischen und bärischen Filter und ermöglichen asymmetrische Glättung oder Periodenlängen. Die Schutzniveaus werden mit nativen Stop- und Limit-Orders erstellt, die `StopLossPips * PipSize` vom Einstiegspreis entfernt positioniert sind. Der Take-Profit verwendet denselben Stoppabstand wieder und multipliziert ihn mit dem für jede Seite definierten Prozentfaktor.

## Einzelheiten

- **Eintrittskriterien**:
  - **Long**: Die schnellen und langsamen Trendfilter für die Long-Seite steigen beide auf dem vorherigen Balken (`MA_fast1[1] > MA_slow1[1]` und `MA_fast2[1] > MA_slow2[1]`) und die vorherige Kerze schließt über ihrem dedizierten Durchschnitt, während die Kerze von vor zwei Balken darunter lag (`Close[2] <= MA_cross[2]` und `Close[1] > MA_cross[1]`).
  - **Short**: Die schnellen und langsamen Trendfilter für die Short-Seite sind beide auf dem vorherigen Balken rückläufig (`MA_fast1[1] < MA_slow1[1]` und `MA_fast2[1] < MA_slow2[1]`) und die vorherige Kerze schließt unter ihrem dedizierten Durchschnitt, während die Kerze von vor zwei Balken darüber lag (`Close[2] >= MA_cross[2]` und `Close[1] < MA_cross[1]`).
- **Lang/Kurz**: Beide Richtungen, aber niemals gleichzeitig.
- **Ausstiegskriterien**:
  - Positionen werden ausschließlich durch schützende Stop-Loss- oder Take-Profit-Orders geschlossen.
- **Stoppt**: Ja. Der Stop wird `StopLossPips * PipSize` vom Einstiegspreis entfernt platziert; Der Take-Profit entspricht der Stoppdistanz multipliziert mit dem Parameter `% of SL`.
- **Standardwerte**:
  - `FirstTrendFastPeriod` = 5, `FirstTrendFastMethod` = SMA.
  - `FirstTrendSlowPeriod` = 20, `FirstTrendSlowMethod` = SMA.
  - `SecondTrendFastPeriod` = 20, `SecondTrendFastMethod` = SMA.
  - `SecondTrendSlowPeriod` = 30, `SecondTrendSlowMethod` = SMA.
  - `BullCrossPeriod` = 5, `BullCrossMethod` = SMA.
  - `BuyVolume` = 0,01, `BuyStopLossPips` = 50, `BuyTakeProfitPercent` = 100.
  - `FirstTrendBearFastPeriod` = 5, `FirstTrendBearFastMethod` = SMA.
  - `FirstTrendBearSlowPeriod` = 20, `FirstTrendBearSlowMethod` = SMA.
  - `SecondTrendBearFastPeriod` = 20, `SecondTrendBearFastMethod` = SMA.
  - `SecondTrendBearSlowPeriod` = 30, `SecondTrendBearSlowMethod` = SMA.
  - `BearCrossPeriod` = 5, `BearCrossMethod` = SMA.
  - `SellVolume` = 0,01, `SellStopLossPips` = 50, `SellTakeProfitPercent` = 100.
  - `PipSize` = 0,0001.
- **Filter**:
  - Kategorie: Trendfolge.
  - Richtung: Dual (lang + kurz).
  - Indikatoren: Mehrere gleitende Durchschnitte.
  - Stopps: Fester Pip-basierter Stopp und proportionaler Take-Profit.
  - Komplexität: Mäßig.
  - Zeitrahmen: Funktioniert mit der konfigurierten Kerzenserie (Standard 15 Minuten).
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikostufe: Mittel.
