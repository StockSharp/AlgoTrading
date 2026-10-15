# Doppel-MA-Trendbestätigungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Doppel-MA-Trendbestätigungs-Strategie** repliziert den ursprünglichen MetaTrader-Experten, der einen langsamen exponentiellen gleitenden Durchschnitt (EMA) mit einem schnellen linear gewichteten gleitenden Durchschnitt (LWMA) kombiniert. Das System wartet, bis beide gleitenden Durchschnitte in dieselbe Richtung ausgerichtet sind, und verwendet den Schlusskurs der vorherigen Kerze als zusätzliche Bestätigung vor dem Öffnen einer Position. Die Idee ist, nur an starken Momentum-Schwankungen teilzunehmen, wenn sowohl der langsame Trendfilter als auch der schnelle Bestätigungsfilter gleichzeitig nach oben oder unten geneigt sind.

Die StockSharp-Implementierung verarbeitet nur vollständig abgeschlossene Kerzen, verfolgt die Steigung jedes gleitenden Durchschnitts über die letzten drei Bars und verwaltet automatisch Schutzorders über den integrierten `StartProtection`-Mechanismus. Die Strategie ist instrument-agnostisch: Sie kann auf jedem Wertpapier und Zeitrahmen operieren, der Kerzen bereitstellt und das Konzept von "Punkten" über den Instrumentenpreisschritt unterstützt.

## Indikatoren
- **Langsame EMA** – Standardperiode 57. Stellt die dominante Trendrichtung dar. Die Strategie erfordert, dass die EMA für zwei aufeinanderfolgende Kerzen steigt (oder fällt), bevor sie handelt.
- **Schnelle LWMA** – Standardperiode 3. Fungiert als Momentum-Bestätigungsfilter. Ihre Neigung muss mit der langsamen EMA übereinstimmen und verstärkt, dass das Momentum den Trend unterstützt.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `SlowMaLength` | 57 | Periode des langsamen EMA-Trendfilters. |
| `FastMaLength` | 3 | Periode des schnellen LWMA-Bestätigungsfilters. |
| `StopLossPoints` | 100 | Schutz-Stop-Abstand in Instrumentenpunkten (multipliziert mit `Security.PriceStep`). |
| `TakeProfitPoints` | 100 | Take-Profit-Abstand in Instrumentenpunkten (multipliziert mit `Security.PriceStep`). |
| `CandleType` | 15-Minuten-Zeitrahmen | Kerzendatentyp für alle Berechnungen. |

Alle Parameter werden als `StrategyParam<T>`-Werte exponiert, sodass sie zur Laufzeit geändert oder durch StockSharps Optimierungstools optimiert werden können.

## Handelsregeln
### Long-Setup
1. Langsame EMA steigt: aktueller Wert > vorheriger Wert > Wert vor zwei Kerzen.
2. Schnelle LWMA steigt: aktueller Wert > vorheriger Wert > Wert vor zwei Kerzen.
3. Vorheriger Kerzenschlusskurs liegt über dem vorherigen Wert der langsamen EMA.
4. Aktueller Wert der langsamen EMA liegt über dem aktuellen Wert der schnellen LWMA.
5. Aktuelle Position ist flach oder short.
6. Wenn alle Bedingungen erfüllt sind, sendet die Strategie eine Kauf-Marktorder für `Volume + |Position|`, um in eine Long-Position zu wechseln.

### Short-Setup
1. Langsame EMA fällt: aktueller Wert < vorheriger Wert < Wert vor zwei Kerzen.
2. Schnelle LWMA fällt: aktueller Wert < vorheriger Wert < Wert vor zwei Kerzen.
3. Vorheriger Kerzenschlusskurs liegt unter dem vorherigen Wert der langsamen EMA.
4. Aktueller Wert der langsamen EMA liegt unter dem aktuellen Wert der schnellen LWMA.
5. Aktuelle Position ist flach oder long.
6. Wenn alle Bedingungen erfüllt sind, sendet die Strategie eine Verkauf-Marktorder für `Volume + |Position|`, um in eine Short-Position zu wechseln.

### Schutzlogik
- `StartProtection` konvertiert `StopLossPoints` und `TakeProfitPoints` in absolute Preisoffsets durch Multiplikation mit `Security.PriceStep`. Stop-Loss- und Take-Profit-Orders werden als Marktausstiege ausgegeben, damit die Engine die Position schließen kann, auch wenn Limit-Orders nicht unterstützt werden.
- Wenn das entgegengesetzte Signal erscheint, kehrt die Strategie sofort die Position um, unabhängig von den Schutzorders.

## Implementierungsdetails
- Nur fertige Kerzen werden verarbeitet, was die Neuen-Bar-Prüfung der ursprünglichen MQL-Version emuliert.
- Die Strategie hält die letzten zwei gleitenden Durchschnittswerte und den vorherigen Schlusskurs in privaten Feldern, um Indikatorverlaufssuchen zu vermeiden.
- `IsFormedAndOnlineAndAllowTrading()` stellt sicher, dass der Handel nur stattfindet, wenn alle Datenströme aktiv und der Handel erlaubt ist.
- Trade-Richtungslogs (`LogInfo`) sorgen für Transparenz beim Debugging und Live-Monitoring.
- Chart-Rendering (falls verfügbar) zeichnet Kerzen und beide gleitenden Durchschnitte zur schnellen visuellen Validierung.

## Verwendungshinweise
- Wählen Sie `Volume` entsprechend der Instrument-Lotgröße. Die Strategie sendet immer Marktorders der Größe `Volume + |Position|`, um effizient umzukehren.
- Bei Instrumenten ohne definierten `PriceStep` greift der Code auf einen Wert von `1` zurück. Passen Sie Parameter entsprechend an, wenn die Tick-Größe abweicht.
- Die Optimierung kann sich auf die gleitenden Durchschnittszeiträume und die Schutzabstände konzentrieren, um die Strategie an verschiedene Märkte anzupassen.
- Kombinieren Sie mit zusätzlichen Filtern (Volatilität, Session-Zeiten usw.) falls erforderlich. Die modulare Struktur ermöglicht einfache Erweiterung.

## Empfohlene Optimierungsbereiche
- `SlowMaLength`: 20 – 120 mit Schritt 5–10.
- `FastMaLength`: 2 – 10 mit Schritt 1.
- `StopLossPoints` / `TakeProfitPoints`: 50 – 200 je nach Instrumentenvolatilität.

Diese Bereiche spiegeln die ursprünglichen Experteneinstellungen genau wider und bieten Flexibilität für andere Instrumente.
