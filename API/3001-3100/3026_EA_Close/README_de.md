# EA Close Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **EA Close Strategie** ist ein direkter StockSharp-Port des originalen MQL5-Expertenberaters "EA Close" von Vladimir Karputov. Die Strategie kombiniert einen Commodity Channel Index (CCI), einen gewichteten gleitenden Durchschnitt (WMA) und einen Stochastik-Oszillator, um Erschöpfungsbewegungen am Ende von Retracments zu erkennen. Aufträge werden nur einmal pro abgeschlossener Kerze ausgewertet, um die "neue Bar"-Logik des Quell-EAs nachzuahmen.

Die StockSharp-Implementierung behält den Parametersatz und die Struktur der MQL-Version bei, sodass bestehende Optimierungen wiederverwendet werden können. Signale werden aus der vorherigen abgeschlossenen Kerze generiert, was das Verhalten deterministisch macht, wenn die Strategie auf historischen Daten abgespielt wird.

## Indikatoren
- **Commodity Channel Index (CCI)** – identifiziert überkaufte und überverkaufte Extreme relativ zum Durchschnittspreis über einen konfigurierbaren Zeitraum.
- **Weighted Moving Average (WMA)** – fungiert als Mikrotrendfilter; der Original-EA verwendet eine 1-Perioden-LWMA des gewichteten Preises, der in der Praxis wie ein leicht geglätteter Kerzenpreis verhält. In diesem Port wird die WMA direkt auf den Kerzenstrom angewendet.
- **Stochastik-Oszillator (%K-Linie)** – bestätigt Momentum-Erschöpfung mit klassischen überkauften und überverkauften Niveaus.

## Handelslogik
1. **Long-Setup**
   - CCI der vorherigen Kerze fällt unter `-CciLevel`.
   - Vorheriger Stochastik %K liegt unter `StochasticLevelDown`.
   - Eröffnungspreis der vorherigen Kerze liegt über dem WMA-Wert dieser Kerze.
   - Wenn diese Bedingungen übereinstimmen und die aktuelle Nettoposition nicht positiv ist, kauft die Strategie. Bestehendes Short-Exposure wird vor dem Öffnen der neuen Long-Position ausgeglichen.
2. **Short-Setup**
   - CCI der vorherigen Kerze steigt über `CciLevel`.
   - Vorheriger Stochastik %K liegt über `StochasticLevelUp`.
   - Schlusskurs der vorherigen Kerze liegt unter dem WMA-Wert dieser Kerze.
   - Wenn wahr und die Position nicht negativ ist, verkauft die Strategie. Offene Long-Positionen werden in derselben Marktorder geschlossen.

Es werden nur finalisierte Kerzendaten verwendet. Dies spiegelt das `OnTick`-Neue-Bar-Gate im MQL-Skript wider und verhindert intrabar Neuzeichnen.

## Risikomanagement
`StartProtection` wird während `OnStarted` aktiviert, was die festen Stop-Loss- und Take-Profit-Abstände aus dem MQL-Code reproduziert. Abstände werden in **Pips** konfiguriert. Der Helper konvertiert Pips in Preiseinheiten, indem er den Preisschritt des Instruments mit 10 multipliziert, wenn die Schrittgenauigkeit drei oder fünf Dezimalstellen hat (z.B. 0,001 oder 0,00001), was der Ziffernanpassung des EAs für 3/5-stellige Notierungen entspricht. Das Setzen eines Abstands auf null deaktiviert diesen Schutzteil.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `Volume` | Auftragsgröße für Markteinstiege. | 1 |
| `StopLossPips` | Fester Stop-Loss-Abstand in Pips. | 35 |
| `TakeProfitPips` | Fester Take-Profit-Abstand in Pips. | 75 |
| `CciPeriod` | Durchschnittslänge des CCI-Indikators. | 14 |
| `CciLevel` | Absoluter Schwellenwert für CCI-Extreme. | 120 |
| `MaPeriod` | Länge des gewichteten gleitenden Durchschnittsfilters. | 1 |
| `StochasticLength` | Look-back-Fenster für den Stochastik-Oszillator (Hoch/Tief-Bereich). | 5 |
| `StochasticKPeriod` | Glättungsfaktor für die %K-Linie. | 3 |
| `StochasticDPeriod` | Glättungsfaktor für die %D-Linie. | 3 |
| `StochasticLevelUp` | Überkauft-Schwellenwert für die %K-Linie. | 70 |
| `StochasticLevelDown` | Überverkauft-Schwellenwert für die %K-Linie. | 30 |
| `CandleType` | Kerzenserie als Datenquelle. | 1-Stunden-Zeitrahmen |

## Verwendungshinweise
- Die Strategie speichert Indikator- und Preiswerte der zuletzt abgeschlossenen Kerze und wertet Signale bei der nächsten Bar-Eröffnung aus, was die Array-Shift-Logik (`CopyBuffer(..., start=1)`) im EA repliziert.
- Marktorders werden so dimensioniert, dass sie jedes entgegengesetzte Exposure ausgleichen und gleichzeitig die neue Position eröffnen, was dem `ClosePositions`-Helper in MQL eng entspricht.
- Der StockSharp `StochasticOscillator` verwendet `Length` als Look-back-Fenster, `KPeriod` für %K-Glättung und `DPeriod` für %D-Glättung, äquivalent zu den `iStochastic`-Parametern (K-Periode, Slowing und D-Periode).
- Da StockSharp mit aggregierten Kerzen statt Tick-Callbacks arbeitet, ist keine zusätzliche Rate-Refresh-Logik erforderlich; das Datenabonnement stellt sicher, dass Indikatoren vollständige Kerzen erhalten.

## Konvertierungshinweise
- Keine Python-Implementierung wird absichtlich bereitgestellt, entsprechend den Konvertierungsaufgabenanforderungen.
- Der gewichtete gleitende Durchschnitt arbeitet auf der Kerzenserie; wenn Sie den genauen MT5-gewichteten Preis `(High + Low + 2 * Close) / 4` benötigen, verarbeiten Sie die Kerzenwerte vor, bevor Sie sie in die WMA einspeisen.
- Schutzorders werden von der Plattform über `StartProtection` verwaltet, sodass explizite Stop/Take-Registrierungen nach jedem Trade nicht notwendig sind.
