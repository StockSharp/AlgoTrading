# AltrTrend Signal v2.2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MetaTrader-Expertenberaters **Exp_AltrTrend_Signal_v2_2**. Sie rekonstruiert die
adaptive Kanallogik des ursprünglichen AltrTrend Signal-Indikators und führt Trades auf verzögerten Balken genau wie die
MQL5-Version aus. Der ADX-Wert verengt oder erweitert den Kanal, sodass Ausbrüche nur dann auslösen, wenn die Trendstärke sie
unterstützt.

## Funktionsweise

1. Bei jeder abgeschlossenen Kerze des konfigurierten Zeitrahmens wird ein dynamischer Kanal berechnet. Die Kanalbreite wird
   durch den höchsten und niedrigsten Preis innerhalb eines Lookbacks definiert, der sich gemäß dem vorherigen ADX-Wert
   ausdehnt oder zusammenzieht (`KPeriod / ADX`).
2. Die inneren Grenzen (`smin`, `smax`) werden um `KPercent` zur Mitte gezogen. Der Preis muss außerhalb dieser inneren Grenzen
   schließen, um einen direktionalen Trendstatus zu etablieren.
3. Wenn der Trend von bearish zu bullish wechselt und der Schluss über der oberen Grenze liegt, wird ein Kaufsignal generiert.
   Ein bearisher Wechsel unter die untere Grenze gibt ein Verkaufssignal aus. Signale werden auf dem durch die `SignalBar`-
   Verzögerung definierten Balken ausgeführt, was dem ursprünglichen Expertenberater-Verhalten entspricht.
4. Optionale Stop-Loss- und Take-Profit-Level werden von Punkten auf Preisschritte abgebildet, damit Schutzausstiege die
   ursprüngliche Orderplatzierung mit festen SL/TP-Werten imitieren.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorheriger Trend war bearish oder neutral, Preis schließt über der oberen kontrahierten Grenze und Long-Einstiege
    sind aktiviert. Short-Positionen können automatisch geschlossen werden wenn erlaubt.
  - **Short**: Vorheriger Trend war bullish oder neutral, Preis schließt unter der unteren kontrahierten Grenze und Short-
    Einstiege sind aktiviert. Long-Positionen können automatisch geschlossen werden wenn erlaubt.
- **Ausstiegskriterien**:
  - Entgegengesetztes Ausbruchssignal wenn Ausstiege für die aktuelle Richtung erlaubt sind.
  - Stop-Loss- oder Take-Profit-Abstände ausgedrückt in Preisschritten.
- **Long/Short**: Doppelte Richtung mit unabhängigen Aktivierungs-/Deaktivierungsschaltern für Einstiege und Ausstiege.
- **Risikomanagement**:
  - `StopLossPoints` und `TakeProfitPoints` replizieren das ursprüngliche MM-Modul durch Anwendung distanzbasierter Ausstiege
    nachdem Marktorders ausgeführt werden.
- **Indikatoreinstellungen**:
  - `KPercent` steuert, wie sehr die Kanalränder zur Mitte des Bereichs gezogen werden.
  - `KStop` hält den ursprünglichen Pfeilprojektionswert für Charting und Logging.
  - `KPeriod` ist der Basis-Lookback vor der ADX-Modulation.
  - `AdxPeriod` legt die Länge des Average Directional Index fest, der die Kanalbreite anpasst.
  - `SignalBar` verzögert die Order-Ausführung um die angegebene Anzahl abgeschlossener Kerzen.
- **Empfohlene Märkte**:
  - Funktioniert am besten bei Instrumenten mit klaren Swing-Phasen, bei denen die Trendstärke im Laufe der Zeit variiert
    (Forex-Majors, Gold und Index-Futures). Standardzeitrahmen ist H1 wie in der MQL5-Vorlage.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `CandleType` | Zeitrahmen für den Aufbau des adaptiven Kanals. |
| `KPercent` | Prozentsatz, der die inneren Kanalgrenzen nach innen zieht. |
| `KStop` | Multiplikator für projizierte Pfeilpreise (für Kompatibilität beibehalten). |
| `KPeriod` | Basisanzahl der Kerzen vor ADX-Anpassung. |
| `AdxPeriod` | Periode des Average Directional Index, der die Kanalbreite antreibt. |
| `SignalBar` | Anzahl abgeschlossener Kerzen, die vor der Ausführung eines Signals gewartet werden. |
| `AllowBuyEntries` / `AllowSellEntries` | Öffnung von Positionen in jede Richtung aktivieren oder deaktivieren. |
| `AllowBuyExits` / `AllowSellExits` | Automatisches Schließen von Positionen bei entgegengesetzten Signalen erlauben. |
| `StopLossPoints` | Stop-Loss-Abstand in Preisschritten (0 deaktiviert). |
| `TakeProfitPoints` | Take-Profit-Abstand in Preisschritten (0 deaktiviert). |

Dieser Port behält die diskreten Schalter und Risikoparameter des ursprünglichen Expertenberaters bei, was es einfach macht,
das gleiche Verhalten in StockSharp Designer, Shell oder Runner zu reproduzieren.
