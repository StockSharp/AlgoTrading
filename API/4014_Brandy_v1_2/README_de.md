# Brandy v1.2-Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Brandy v1.2-Strategie** ist eine direkte Konvertierung des MetaTrader 4 Expert Advisors „Brandy_v1_2.mq4“ in das StockSharp High-Level-Strategie-Framework. Das System wertet ein Paar verschobener einfacher gleitender Durchschnitte (SMAs) aus, die auf der Grundlage des Schlusskurses der konfigurierten Kerzenserie berechnet werden. Neue Positionen werden nur eröffnet, wenn sowohl der langfristige als auch der kurzfristige SMA ein synchronisiertes Momentum in die gleiche Richtung aufweisen, während bestehende Geschäfte mithilfe von Steigungsumkehrungen, festen Stop-Loss-Levels und einem optionalen Trailing-Stop-Modul verwaltet werden.

Das ursprüngliche MQL-Skript wurde genau einmal pro abgeschlossenem Balken ausgeführt. Dieser Port verarbeitet fertige StockSharp-Kerzen auf die gleiche Weise und stellt so sicher, dass alle Handelsentscheidungen auf geschlossenen Daten basieren, ohne sich auf teilweise geformte Balken zu verlassen.

## Handelslogik
1. **Indikatorvorbereitung**
   - Es werden zwei SMAs berechnet: eine längere Basislinie (`LongPeriod`) und eine kürzere Bestätigungslinie (`ShortPeriod`).
   - Auf jeden Durchschnitt wird zweimal zugegriffen: auf den Wert des vorherigen Balkens (Verschiebung = 1) und einen anderen um jeweils `LongShift`/`ShortShift` Balken verschobenen Wert. Dadurch werden die im ursprünglichen EA vorhandenen `iMA(..., shift)`-Aufrufe reproduziert.
2. **Eintrittsregeln**
   - **Kaufen**, wenn der Wert beider SMAs auf dem vorherigen Balken größer ist als ihre verschobenen Gegenstücke (beide Steigungen zeigen nach oben) und keine Position offen ist.
   - **Verkaufen**, wenn der Wert beider SMAs auf dem vorherigen Balken niedriger ist als der ihrer verschobenen Gegenstücke (beide Steigungen zeigen nach unten) und keine Position offen ist.
   - Es kann immer nur eine Position aktiv sein, was die `k == 0`-Prüfung in der MQL-Quelle widerspiegelt.
3. **Ausgangsregeln**
   - **Steigungsumkehr**: Eine offene Long-Position wird liquidiert, wenn die Long-Position SMA nach unten geht (`longPrev < longShifted`), während eine Short-Position gedeckt wird, wenn die Long-Position SMA nach oben geht (`longPrev > longShifted`).
   - **Fester Stop-Loss**: Beim Einstieg speichert die Strategie einen anfänglichen Stop-Level, der um `StopLossPoints × PriceStep` vom Einstiegspreis abweicht. Der Stop wird anhand des Hoch-/Tief-Bereichs der Kerze überprüft, was annähernd dem Tick-Level-Management des ursprünglichen Beraters entspricht.
   - **Trailing Stop**: Bei `TrailingStopPoints ≥ 100` repliziert die Strategie die Trailing-Logik (Parameter `ts`). Sobald der variable Gewinn die Trailing-Distanz überschreitet, wird der Stop auf `currentPrice ± trailingDistance` gezogen, sofern das neue Niveau näher am Preis liegt als der bestehende Stop. Dieses Verhalten entspricht den `OrderModify`-Aufrufen im MQL-Experten.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `LongPeriod` | 70 | Länge des primären SMA (`p1` in MQL). Muss > 0 sein. |
| `LongShift` | 5 | Auf den langen SMA-Vergleich (`s1`) wird eine Rückwärtsverschiebung angewendet. Kann Null sein. |
| `ShortPeriod` | 20 | Länge der Bestätigung SMA (`p2`). Muss > 0 sein. |
| `ShortShift` | 5 | Rückwärtsverschiebung für das kurze SMA (`s2`). Kann Null sein. |
| `StopLossPoints` | 50 | Feste Stoppdistanz in Preisschritten (`sl`). Auf 0 setzen, um den harten Stopp zu deaktivieren. |
| `TrailingStopPoints` | 150 | Nachlaufdistanz in Preisschritten (`ts`). Trailing wird nur aktiviert, wenn der Wert ≥ 100 ist, was den ursprünglichen Schwellenwert widerspiegelt. |
| `Volume` | 0,1 | Für Einträge verwendetes Bestellvolumen (`lots`). |
| `CandleType` | 15-minütiger Zeitrahmen | Von der Strategie verarbeitete Kerzenserie (vom Benutzer konfigurierbar). |

### Preisschrittabhängigkeit
Beide Stoppparameter wirken in Instrumentenpunkten. Die Hilfsmethode wandelt sie über `Security.PriceStep` in absolute Preisdeltas um. Wenn die Datenquelle `PriceStep` nicht bereitstellt, greift die Strategie auf `0.0001` zurück, sodass die Logik weiterhin funktioniert, wenn auch mit einer ungefähren Konvertierung. Überprüfen Sie vor der Live-Nutzung immer die Symbolmetadaten in StockSharp.

## Risikomanagement
- **Hard Stop**: intern gespeichert und anhand jeder fertigen Kerze validiert. Wenn der Preis den Stop überschreitet, schließt der entsprechende `SellMarket`/`BuyMarket`-Anruf die gesamte Position.
- **Trailing Stop**: folgt den genauen Bedingungen des ursprünglichen EA und verschiebt den Stop nur, wenn der aktuelle Gewinn die Trailing-Distanz überschreitet *und* der bestehende Stop immer noch weiter als diese Distanz entfernt ist.
- **Einzelne Position**: Der Algorithmus bildet niemals eine Pyramide; Es gibt entweder eine einzelne Long-Position, eine einzelne Short-Position oder es ist flach.

## Implementierungshinweise
- Der Status (Einstiegspreis, Stop-Level, SMA-Historien) wird am `OnReseted()` automatisch zurückgesetzt, um saubere Backtests und Neustarts zu gewährleisten.
- Indikatorverläufe werden in kurzen Rollpuffern gespeichert, um die `iMA(..., shift)`-Offsets ohne Aufruf von `GetValue()` zu reproduzieren.
- Alle Inline-Kommentare bleiben gemäß den Repository-Richtlinien auf Englisch.
- Es wird kein Python-Gegenstück bereitgestellt. Nur die C#-High-Level-Implementierung wird wie angefordert in `CS/BrandyV12Strategy.cs` bereitgestellt.

## Nutzung
1. Platzieren Sie die Strategie in einer StockSharp-Lösung, wählen Sie das gewünschte Instrument aus und stellen Sie sicher, dass die Kerzendaten mit dem durch `CandleType` angegebenen Zeitrahmen übereinstimmen.
2. Konfigurieren Sie die Parameter in der Benutzeroberfläche oder per Code. Die Standardwerte replizieren die ursprünglichen MT4-Werte.
3. Starten Sie die Strategie. Es abonniert die Kerzenserie, zeichnet beide SMAs auf dem Chart und verwaltet Trades automatisch.

> **Haftungsausschluss:** Dieser Port ist für Bildungs- und Testzwecke gedacht. Überprüfen Sie immer das Verhalten bei historischen und Papierhandelssitzungen, bevor Sie es auf Live-Märkten einsetzen.
