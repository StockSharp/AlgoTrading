# 4153 Vortex-Oszillatorsystem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie reproduziert den MetaTrader 4 „Vortex Oscillator System“-Experten unter Verwendung der High-Level-Strategie API von StockSharp. Es leitet einen normalisierten Vortex-Oszillator ab, indem es die standardmäßigen Vortex-Indikatorkomponenten kombiniert und reagiert, wenn der Impuls ein konfigurierbares neutrales Band verlässt. Der Algorithmus handelt mit einem einzelnen Symbol und arbeitet immer mit vollständig geschlossenen oder umgekehrten Positionen.

## Handelsregeln
- Ein durch **CandleType** definiertes Kerzenabonnement speist einen Vortex-Indikator mit der Periode **VortexLength**. Der Oszillator wird als `(VI+ - VI-) / (VI+ + VI-)` berechnet, wodurch die Messwerte im Bereich `[-1, 1]` bleiben.
- Ein Long-Setup wird aktiviert, wenn der Oszillator unter **BuyThreshold** fällt und, wenn **UseBuyStopLoss** aktiviert ist, über **BuyStopLossLevel** bleibt. Ein Short-Setup wird aktiviert, wenn der Oszillator über **SellThreshold** steigt und, wenn **UseSellStopLoss** aktiviert ist, unter **SellStopLossLevel** bleibt.
- Immer wenn sich der Oszillator wieder innerhalb des neutralen Bandes bewegt, das durch **BuyThreshold** und **SellThreshold** begrenzt wird, werden beide Setups gelöscht, sodass der nächste Durchbruch aus einem neutralen Zustand erfolgen muss.
- Wenn ein Long-Setup aktiv ist, während die aktuelle Position flach oder short ist, sendet die Strategie einen Marktkauf für **Volumen**-Lots plus jede Menge, die zur Deckung eines bestehenden Short erforderlich ist. Short-Setups spiegeln dieses Verhalten wider, indem sie **Volumen**-Lots plus die ausstehende Long-Menge verkaufen.
- Offene Positionen können ohne umgekehrtes Setup geschlossen werden: Wenn **UseBuyStopLoss** aktiviert ist und der Oszillator **BuyStopLossLevel** berührt, wird der Long-Trade liquidiert; **UseBuyTakeProfit** beendet eine Long-Position, sobald der Oszillator **BuyTakeProfitLevel** überschreitet. Äquivalente Prüfungen mit **SellStopLossLevel** und **SellTakeProfitLevel** verwalten Short-Positionen, wenn ihre jeweiligen Schalter aktiviert sind.

## Parameter
- **VortexLength** – Anzahl der Kerzen, die zur Berechnung der VI+- und VI--Werte verwendet werden.
- **CandleType** – von der Marktdatenquelle angeforderter Zeitrahmen oder Datentyp.
- **Volumen** – Basisauftragsgröße für neue Einträge; Bei Umkehraufträgen wird automatisch die Menge hinzugefügt, die zum Ausgleich der aktuellen Position erforderlich ist.
- **BuyThreshold** – Oszillatorpegel, der nach Durchbrechen ein Long-Setup aktiviert.
- **UseBuyStopLoss** – erfordert, dass der Oszillator über **BuyStopLossLevel** bleibt, bevor ein Long-Einstieg aktiviert werden kann.
- **BuyStopLossLevel** – Oszillatorniveau, das eine Long-Position sofort schließt, wenn der Stoppfilter aktiviert ist.
- **UseBuyTakeProfit** – schaltet den oszillatorbasierten Take-Profit für Long-Trades um.
- **BuyTakeProfitLevel** – Oszillatorniveau, das Gewinne aus Long-Positionen realisiert, wenn der Take-Profit-Filter aktiv ist.
- **SellThreshold** – Oszillatorpegel, der bei Überschreitung ein Short-Setup aktiviert.
- **UseSellStopLoss** – erfordert, dass der Oszillator unter **SellStopLossLevel** bleibt, bevor ein Short-Einstieg aktiviert werden kann.
- **SellStopLossLevel** – Oszillatorniveau, das eine Short-Position sofort schließt, wenn der Stoppfilter aktiviert ist.
- **UseSellTakeProfit** – schaltet den oszillatorbasierten Take-Profit für Short-Trades um.
- **SellTakeProfitLevel** – Oszillatorniveau, das Gewinne aus Short-Positionen realisiert, wenn der Take-Profit-Filter aktiv ist.

## Zusätzliche Hinweise
- Die Strategie zeichnet automatisch Kerzen und ausgeführte Trades auf dem Chart auf; Die interne Oszillatorlogik fügt keine benutzerdefinierten Bereiche hinzu.
- Da der Oszillator normalisiert ist, werden die Standardschwellenwerte `-0.75`, `0.75`, `-1.00` und `1.00` direkt vom ursprünglichen Expert Advisor übernommen und können mithilfe des Parametersystems von StockSharp optimiert werden.
- Die Logik hält niemals gleichzeitige Long- und Short-Positionen; Bei jeder Umkehrung wird zuerst das aktuelle Engagement geschlossen und dann die Gegenseite in einer einzelnen Marktorder geöffnet.
