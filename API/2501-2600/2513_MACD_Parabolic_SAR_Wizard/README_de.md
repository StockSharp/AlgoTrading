# MACD Parabolic SAR Assistent-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Konvertierung des MetaTrader Expert Advisors, der vom MQL5-Assistenten generiert wurde und MACD-Momentum mit der Parabolic-SAR-Trendrichtung kombiniert. Die Logik reproduziert den Bewertungsmechanismus des Assistenten, indem jedem Indikator eine normalisierte Punktzahl (0..100) zugewiesen und die Beiträge vor Handelsentscheidungen gewichtet werden.

## Handelslogik
- **Indikatoren**
  - *MACD (12, 24, 9)*: das Vorzeichen des Histogramms definiert, ob bullisches Momentum (Histogramm > 0) oder bearisches Momentum (Histogramm < 0) aktiv ist.
  - *Parabolic SAR (0.02, 0.2)*: der Schlusskurs über dem SAR-Punkt wird als Aufwärtstrend interpretiert, und unterhalb des SAR-Punktes als Abwärtstrend.
- **Punktzahl-Konstruktion**
  - MACD produziert entweder 100 (bullisch) oder 0 (bearisch) Punkte für die Long-Seite. Die invertierten Werte werden für die Short-Seite verwendet.
  - Parabolic SAR verhält sich gleich und liefert 100 Punkte, wenn der Trend mit der jeweiligen Richtung übereinstimmt.
  - Beide Punktzahlen werden über die benutzerdefinierten Gewichte (`MacdWeight` und `SarWeight`) kombiniert. Mit den Standardgewichten (0.9 und 0.1) dominiert der MACD die Endentscheidung genau wie in der Assistenten-Vorlage.
- **Einstiegsregeln**
  - Bullische Punktzahl berechnen: `bullScore = macdBull * MacdWeight + sarBull * SarWeight`.
  - Bearische Punktzahl berechnen: `bearScore = macdBear * MacdWeight + sarBear * SarWeight`.
  - Long-Position eröffnen (oder von Short umkehren) wenn `bullScore >= OpenThreshold` (Standard `20`).
  - Short-Position eröffnen (oder von Long umkehren) wenn `bearScore >= OpenThreshold`.
- **Ausstiegsregeln**
  - Long-Positionen werden geschlossen, wenn die bearische Punktzahl das starke Bestätigungsniveau `CloseThreshold` erreicht (Standard `100`).
  - Short-Positionen werden geschlossen, wenn die bullische Punktzahl `CloseThreshold` erreicht.
  - Ausstiegssignale werden vor Einstiegssignalen ausgewertet, um das Verhalten des ursprünglichen Experts zu imitieren, der das Schließen widersprüchlicher Trades priorisiert.

## Risikomanagement
- `StopLossPoints` und `TakeProfitPoints` replizieren die punktbasierte Geldverwaltung des Assistenten. Beide Werte werden mit dem `PriceStep` des Instruments in Preiseinheiten umgewandelt und dann an `StartProtection` übergeben.
- Setzen Sie beide Parameter auf `0`, um die entsprechende Schutzorder zu deaktivieren.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `MacdFastPeriod` | Schnelle EMA-Periode für MACD. | 12 |
| `MacdSlowPeriod` | Langsame EMA-Periode für MACD. | 24 |
| `MacdSignalPeriod` | Signal-SMA-Periode für MACD. | 9 |
| `MacdWeight` | Gewicht der MACD-Punktzahl (0..1). | 0.9 |
| `SarWeight` | Gewicht der Parabolic-SAR-Punktzahl (0..1). | 0.1 |
| `OpenThreshold` | Mindestpunktzahl zum Öffnen/Umkehren von Positionen. | 20 |
| `CloseThreshold` | Mindest-Gegenpunktzahl zum Schließen von Positionen. | 100 |
| `SarStep` | Parabolic-SAR-Beschleunigungsschritt. | 0.02 |
| `SarMax` | Maximale Parabolic-SAR-Beschleunigung. | 0.2 |
| `StopLossPoints` | Stop-Loss-Distanz in Preispunkten. | 50 |
| `TakeProfitPoints` | Take-Profit-Distanz in Preispunkten. | 115 |
| `CandleType` | Kerzendatenquelle für Indikatorberechnungen. | 15-Minuten-Zeitrahmen |

## Verwendungshinweise
- Die Standardparameter spiegeln die `.mq5`-Vorlage wider, sodass sich die Strategie konsistent mit dem ursprünglich vom Assistenten generierten Expert Advisor verhält.
- Passen Sie `MacdWeight`, `SarWeight` und die Schwellenwerte an, um die Empfindlichkeit von Ein- und Ausstiegen zu ändern. Beispielsweise erfordert eine Erhöhung von `OpenThreshold` eine stärkere Bestätigung vor dem Öffnen neuer Trades.
- Die internen Felder `_lastBullScore` und `_lastBearScore` werden jede Bar aktualisiert und können protokolliert oder exponiert werden, wenn Sie überwachen möchten, wie sich die kombinierte Punktzahl über die Zeit entwickelt.
- Da die Strategie von abgeschlossenen Kerzen abhängt, stellen Sie sicher, dass Ihr Datenfeed vollständige Kerzenaktualisierungen für den ausgewählten `CandleType` bereitstellt.
- Geldverwaltung wird in Punkten ausgedrückt; stellen Sie sicher, dass das gewählte Instrument den erwarteten Preisschritt verwendet, damit Schutzorders mit den beabsichtigten Distanzen übereinstimmen.
