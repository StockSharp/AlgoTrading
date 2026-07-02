# Strategie Macd Pattern Trader v02 (StockSharp Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-High-Level-API-Konvertierung des MetaTrader-Experten **MacdPatternTraderv02.mq4** (Verzeichnis `MQL/8194`). Es reproduziert die ursprüngliche MACD-Mustererkennung und die aktiven Positionsverwaltungsregeln und stellt gleichzeitig praktische Parameter für die weitere Optimierung bereit.

## Kernidee

1. Berechnen Sie die MACD-Hauptlinie mithilfe der schnellen und langsamen EMA-Perioden (`FastEmaPeriod`, `SlowEmaPeriod`) mit einer Signallänge von einer Kerze (entsprechend der MQL-Version).
2. Überwachen Sie nur abgeschlossene Kerzen. Wenn der Wert MACD eine bestimmte Drei-Takt-Sequenz um die Nulllinie zeichnet, aktivieren Sie entweder das kurze oder das lange Muster:
   - **Kurzes Muster**: erfordert eine positive MACD-Phase, gefolgt von einem negativen Rückzug über `MinThreshold` und dann einer Abwärtsbewegung.
   - **Langes Muster**: erfordert eine negative MACD-Phase, gefolgt von einem positiven Rückzug unter `MaxThreshold` und dann einer Aufwärtsbewegung.
3. Führen Sie Marktaufträge mit `TradeVolume` aus, sobald das Muster bestätigt wird.
4. Schützen Sie jede Position mit einem Stop-Loss, der über dem letzten Swing-Extrem liegt (über `StopLossBars` Kerzen) plus einem zusätzlichen Offset in Punkten (`OffsetPoints`).
5. Definieren Sie das Take-Profit-Niveau, indem Sie aufeinanderfolgende `TakeProfitBars`-Segmente scannen und die extremsten erreichten Höchst-/Tiefstwerte auswählen, während die Sequenz weiterhin neue Extremwerte druckt.
6. Verwalten Sie offene Positionen mit dem aktiven Positionsmanager des ursprünglichen Experten: Nachdem ein Mindestgewinn von fünf Punkten erreicht wurde, schließt die Strategie ein Drittel des Volumens, wenn die vorherige Kerze den Trend bestätigt (Filter `Ema2Period`), und eine weitere Hälfte, wenn der Preis mit der Mittellinie von `SmaPeriod` und `Ema3Period` interagiert.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `StopLossBars` | Anzahl der abgeschlossenen Kerzen, die bei der Berechnung des Stop-Loss-Swing-Extrems überprüft werden. |
| `TakeProfitBars` | Fenstergröße (in Kerzen) für die sequentielle Suche nach Extremwerten, die das Take-Profit-Ziel bildet. |
| `OffsetPoints` | Zusätzlicher Offset, ausgedrückt in Instrumentenpunkten, der dem Stop-Loss hinzugefügt wird. |
| `FastEmaPeriod` | Schnelle EMA-Länge für die MACD-Hauptzeile. |
| `SlowEmaPeriod` | Langsame EMA-Länge für die MACD-Hauptzeile. |
| `MaxThreshold` | Positiver MACD-Schwellenwert, der die Kurzmustervorbereitung beendet. |
| `MinThreshold` | Negativer MACD-Schwellenwert, der die Vorbereitung des langen Musters beendet. |
| `Ema1Period` | Erster EMA-Zeitraum, der vom ursprünglichen Geldverwaltungsblock verwendet wird (der Vollständigkeit halber beibehalten). |
| `Ema2Period` | Zweiter Zeitraum EMA, der zur Validierung des Teilgewinns für Long-/Short-Positionen verwendet wird. |
| `SmaPeriod` | SMA Zeitraum, der im zweiten Teilabschlusstrigger verwendet wird. |
| `Ema3Period` | Langsamer EMA-Zeitraum gepaart mit SMA, um Mean-Reversion-Exits zu erkennen. |
| `TradeVolume` | Market-Order-Volumen (Lots). |
| `CandleType` | Kerzendatentyp, der zur Versorgung aller Indikatoren verwendet wird. |

## Handelslogik

- **Kurzer Eintrag**: wird ausgelöst, wenn die MACD-Sequenz `(prev3, prev2, prev1, current)` mit den ursprünglichen Bedingungen übereinstimmt (`macdPrev1 < macdPrev3`, `macdPrev1 > macdPrev2`, `current < prev1`, `current < 0` und Größenprüfung). Bestehende Long-Positionen werden reduziert, bevor eine neue Short-Position eröffnet wird.
- **Langer Eintrag**: Symmetrische Regeln, bei denen `current > 0`, die MACD-Werte das Spiegelbildmuster bilden und die Größenprüfung erfüllt ist. Vorhandene Short-Positionen werden reduziert, bevor eine neue Long-Position eröffnet wird.
- **Stopps und Ziele**: werden unmittelbar nach jedem Einstieg berechnet und nur aktualisiert, wenn ein neuer Trade ausgeführt wird.
- **Teilweise Schließungen**: Sobald der Gewinn fünf Punkte erreicht (im Verhältnis zur Punktgröße des Instruments), schließt die Strategie ein Drittel des verbleibenden Volumens, wenn die vorherige Kerze über `EMA2` schließt. Die nächste Stufe schließt die Hälfte des verbleibenden Volumens, wenn die vorherige Kerze den Durchschnitt von `SMA` und `EMA3` durchbricht.
- **Vollständiger Ausstieg**: Jede Preisberührung des Stop-Loss- oder Take-Profit-Niveaus schließt die gesamte Position. Nach jedem erzwungenen Verlassen wird der interne Zustand automatisch zurückgesetzt.

## Notizen

- Die Punktgröße wird aus `Security.PriceStep` oder, falls nicht verfügbar, aus den Sicherheitsdezimalstellen abgeleitet. Als sicherer Fallback wird der Standardwert `0.0001` verwendet.
- Der Kerzenverlauf wird gespeichert (bis zu 1024 Einträge), um die MQL-Hilfsfunktionen `iHighest`, `iLowest` und den sequentiellen Extremum-Scan von `TakeProfit()` zu replizieren.
- Alle Kommentare innerhalb der Strategie bleiben in englischer Sprache, wie in den Repository-Richtlinien gefordert.
- Python-Ports werden für diese Aufgabe absichtlich weggelassen.
