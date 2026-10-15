# Maximus vX Lite-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Portierung des MetaTrader 5-Expertenberaters „maximus_vX lite" auf die StockSharp High-Level-API.
Die Strategie sucht nach Konsolidierungszonen ober- und unterhalb des aktuellen Preises und wartet, bis sich der Preis um eine konfigurierbare
Anzahl von Punkten von diesen Zonen entfernt, bevor sie einsteigt. Die Positionsgröße wird aus einem optionalen Risikoprozent-Budget bestimmt,
und schwebende Gewinne können eine erzwungene Liquidation aller offenen Exposition auslösen.

## Strategielogik

1. **Historischer Scan** – bei jeder abgeschlossenen Kerze hält die Strategie bis zu `HistoryDepth` Kerzen und verwendet ein gleitendes
   `RangeLookback`-Fenster, um kompakte Hochs und Tiefs zu erkennen, die Konsolidierungsbereiche bilden.
2. **Oberer Kanal** – wenn ein gültiger oberer Block erkannt wird, wird der Kanal um den aktuellen Schluss mit einer Breite von `RangePoints`
   verankert. Wenn kein historischer Block die Anforderungen erfüllt, fällt der Kanal auf die gleiche Breite zurück, die am aktuellen Preis ausgerichtet ist.
3. **Unterer Kanal** – der untere Block wird entweder direkt aus historischen Hochs/Tiefs genommen, die die Bedingungen erfüllen, oder,
   wenn keine vorhanden sind, aus einem synthetischen Niveau um den aktuellen Schluss minus `RangePoints`.
4. **Long-Einstiege** – zwei Long-Setups sind erlaubt:
   - Ausbruch über die untere Konsolidierung: Der Preis muss `_lowerMax` um `DistancePoints` überschreiten und der obere Kanal muss
     verfügbar sein. Das Take Profit verwendet zwei Drittel der Distanz zwischen `_lowerMax` und `_upperMin`, mit einem Minimum von `RangePoints`.
   - Ausbruch über den oberen Kanal: Der Preis muss `_upperMax` um `DistancePoints` überschreiten. Das Take Profit wird auf `2 * RangePoints` gesetzt.
5. **Short-Einstiege** – symmetrische Logik wird ausgelöst, wenn der Preis um `DistancePoints` unter `_upperMin` oder `_lowerMin` fällt.
   Das primäre Short-Setup verwendet ebenfalls das dynamische Zweidrittel-Ziel, das sekundäre verwendet `2 * RangePoints`.
6. **Stops und Ausstiege** – `StopLossPoints` definiert einen festen Schutz-Stop, wenn größer als null. `MinProfitPercent` überwacht
   schwebende Kapital gegenüber dem letzten Flat-Balance und schließt alle Positionen, sobald der Schwellenwert überschritten wird. Manuelle
   Stop-/Ziel-Prüfungen emulieren das ursprüngliche Expertenberater-Verhalten innerhalb der Strategie.
7. **Positionsgröße** – wenn `RiskPercent` größer als null ist und ein Stop definiert ist, wird das Ordervolumen aus dem Portfolio-Wert und
   der Stop-Distanz berechnet. Andernfalls verwendet die Strategie die `Volume`-Eigenschaft erneut.

## Parameter

- `DelayOpen` (Standard `2`) – Anzahl der Zeitrahmenbars, während derer das Hinzufügen zur gleichen Seite erlaubt ist.
- `DistancePoints` (Standard `850`) – Mindestabstand von einer Konsolidierungsgrenze vor dem Einstieg.
- `RangePoints` (Standard `500`) – Breite der Konsolidierungsboxen.
- `HistoryDepth` (Standard `1000`) – Anzahl der im Speicher gehaltenen Kerzen für historische Scans.
- `RangeLookback` (Standard `40`) – Fensterlänge zur Berechnung lokaler Maxima und Minima.
- `CandleType` (Standard `TimeSpan.FromMinutes(15).TimeFrame()`) – Zeitrahmen für Berechnungen.
- `RiskPercent` (Standard `5m`) – Prozentsatz des Portfolio-Werts, der pro Trade riskiert wird. Auf null setzen, um festes Volumen zu verwenden.
- `StopLossPoints` (Standard `1000`) – Schutz-Stop-Distanz; null deaktiviert den Stop.
- `MinProfitPercent` (Standard `1m`) – schwebender Gewinnprozentsatz, der alle Positionen zum Schließen zwingt.

## Details

- **Long/Short**: Beide Richtungen
- **Ausstiegskriterien**: Fester Stop oder Take Profit, Kapitalsperre via `MinProfitPercent`
- **Stops**: Optionaler fester Stop aus `StopLossPoints`
- **Indikatoren**: Keine (reiner Price Action mit Gleitfenster-Analyse)
- **Zeitrahmen**: Konfigurierbar via `CandleType` (Standard 15 Minuten)
- **Komplexität**: Mittel (kombiniert History-Scanning, dynamische Ziele und Risikosizing)
- **Risikolevel**: Hoch bei Verwendung des Risikoprozentsatzes aufgrund der Ausbruchsnatur
