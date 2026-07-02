# Explosion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie reproduziert die Logik des MetaTrader-Experten "Explosion". Sie beobachtet die Spanne jeder fertigen Kerze und tritt in den Markt ein, wenn der neueste Balken "explodiert" – seine Höhe verdoppelt die Spanne der vorherigen Kerze mehr als. Die Richtung wird durch den Kerzenkörper entschieden: Ein bullischer Körper öffnet eine Long-Position, während ein bärischer Körper eine Short-Position öffnet.

## Handelsregeln

1. Verarbeite nur fertige Kerzen aus dem konfigurierten `CandleType`-Abonnement.
2. Berechne die aktuelle Spanne als `High - Low` und vergleiche sie mit der Spanne der vorherigen Kerze.
3. Ein **Long**-Einstieg wird eröffnet, wenn `currentRange > previousRange * 2` und der Schlusskurs über dem Eröffnungskurs liegt.
4. Ein **Short**-Einstieg wird eröffnet, wenn `currentRange > previousRange * 2` und der Schlusskurs unter dem Eröffnungskurs liegt.
5. Wenn `OnlyOnePositionPerBar` aktiviert ist, ist maximal ein Trade pro Richtung für eine Kerzeneröffnungszeit erlaubt. Versuche auf demselben Balken werden ignoriert.
6. Die Strategie hält eine einzelne Nettoposition, daher schließt ein entgegengesetzter Trade automatisch jedes bestehende Exposure, bevor die neue Richtung etabliert wird.
7. Schutzmechanismen:
   - `StopLossPips` und `TakeProfitPips` platzieren virtuelle Ausstiegsniveaus in Pips vom Einstiegspreis gemessen.
   - `TrailingStopPips` und `TrailingStepPips` bewegen den Stop, sobald der Preis zugunsten der Position um mindestens die Trailing-Distanz und jeden weiteren Schritt wandert.
   - Der optionale Pip-Multiplikator emuliert den MQL-Auto-Digits-Helfer, indem er die Pip-Größe auf 3- und 5-stelligen Instrumenten mit 10 multipliziert.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `TradeVolume` | `0.01` | Marktorder-Volumen für Einstiege. |
| `StopLossPips` | `20` | Stop-Loss-Abstand in Pips. Null deaktiviert den Stop. |
| `TakeProfitPips` | `10` | Take-Profit-Abstand in Pips. Null deaktiviert den Take. |
| `TrailingStopPips` | `25` | Aktivierungsabstand für den Trailing Stop in Pips. Null deaktiviert das Trailing. |
| `TrailingStepPips` | `5` | Zusätzliche Bewegung in Pips, bevor der Trailing Stop vorrückt. Muss bei aktiviertem Trailing positiv bleiben. |
| `UseAutoPipMultiplier` | `true` | Multipliziert die Pip-Größe bei Instrumenten mit 3 oder 5 Dezimalstellen mit 10, entsprechend dem MQL-Auto-Digits-Helfer. |
| `OnlyOnePositionPerBar` | `true` | Verbietet mehr als einen Einstieg pro Balkeneröffnungszeit. |
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Kerzenserie für Berechnungen. |

## Hinweise zur Konvertierung

- StockSharp arbeitet mit einer Nettoposition, daher wird das Hedging mehrerer simultaner Orders des originalen Expert Advisors nicht unterstützt. Das Verhalten entspricht der Aktivierung von `OnlyOneOpenedPos` in der MQL-Version.
- Trailing-Stop-Updates werden bei Kerzenschlusskursen statt bei jedem Tick durchgeführt. Die Logik entspricht den ursprünglichen Schwellenwerten und bleibt mit der High-Level-API kompatibel.
- Der Pip-Multiplikator reproduziert die automatische Ziffernerkennung, die Abstände bei 5-stelligen Forex-Symbolen mit 10 skaliert.

## Verwendungsempfehlungen

1. Wählen Sie das Instrument und den Zeitrahmen, die dem ursprünglichen Experten entsprechen (z.B. die empfohlenen M15/M30-Charts für Forex-Paare).
2. Passen Sie die pip-basierten Risikoparameter an die Volatilität des Instruments an.
3. Aktivieren Sie die Protokollierung, um zu überwachen, wann der Trailing Stop vorrückt und wie Schutzlevels neu berechnet werden.
