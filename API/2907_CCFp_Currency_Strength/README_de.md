# CCFp-Währungsstärke-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie portiert den klassischen MetaTrader CCFp-Expertenberater in die High-Level-API von StockSharp. Sie berechnet eine relative Stärke-Punktzahl für die acht wichtigsten Währungen (USD, EUR, GBP, CHF, JPY, AUD, CAD, NZD) anhand von Verhältnissen zwischen schnellen und langsamen einfachen gleitenden Durchschnitten auf den sieben USD-basierten Hauptpaaren (EURUSD, GBPUSD, AUDUSD, NZDUSD, USDCAD, USDCHF, USDJPY). Wenn die Differenz zwischen zwei Währungsstärken einen konfigurierbaren Schwellenwert überschreitet, öffnet die Strategie Marktpositionen, die die stärkere Währung gegen die schwächere ausdrücken.

Die Implementierung folgt der empfohlenen High-Level-Architektur: Jedes Instrument hat sein eigenes Kerzenabonnement, Indikatoren werden über `Bind` gebunden, und die Auftragsverwaltung verwendet `RegisterOrder` mit Marktaufträgen. Kommentare zu ausgeführten Aufträgen verwenden das ursprüngliche `(TOPDOWN)`-Format, um denselben Buchführungsstil wie die MQL-Version beizubehalten.

## Erforderliche Instrumente
Folgende Wertpapiere an die Strategieparameter anhängen:

- `EURUSD`
- `GBPUSD`
- `AUDUSD`
- `NZDUSD`
- `USDCAD`
- `USDCHF`
- `USDJPY`

Alle sieben Paare müssen denselben Zeitrahmen teilen, der über den Parameter `Candle Type` eingestellt wird.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `Fast MA` | Schnelle Moving-Average-Periode für die Stärkeberechnung. |
| `Slow MA` | Langsame Moving-Average-Periode für die Stärkeberechnung. |
| `Strength Step` | Minimale Differenz zwischen zwei Währungen, die überschritten werden muss, um ein neues Signal auszulösen. |
| `Close Opposite` | Bei Aktivierung schließt die Strategie gegenläufige Positionen vor dem Senden einer neuen Order. |
| `Candle Type` | Von den Indikatoren verarbeitete Kerzenserie. |
| Basis-`Volume` | Wird von der Standard-`Strategy.Volume`-Eigenschaft übernommen und für jeden gesendeten Marktauftrag verwendet. |

## Handelslogik
1. Jedes der sieben USD-Hauptpaare wird mit seinem eigenen Paar einfacher gleitender Durchschnitte (schnell und langsam) abonniert.
2. Jedes Mal, wenn eine abgeschlossene Kerze eintrifft, konvertiert die Strategie das Verhältnis der langsamen und schnellen Durchschnitte in dieselben synthetischen Stärkewerte, die vom ursprünglichen CCFp-Indikator erzeugt werden.
3. Nachdem alle sieben Paare aktualisiert wurden, werden die acht Währungsstärke-Punktzahlen neu berechnet.
4. Wenn die Differenz zwischen einer "Top"- und einer "Down"-Währung das `Strength Step`-Niveau aufwärts kreuzt, während die Top-Währung steigt und die Down-Währung fällt, wird eine Gelegenheit erkannt.
5. Die Strategie öffnet Marktaufträge, die Long-Exposition zur starken Währung und Short-Exposition zur schwachen Währung ausdrücken:
   - Wenn USD die starke Währung ist, wird nur ein Auftrag auf dem Gegenpaar platziert (zum Beispiel Short `EURUSD`).
   - Wenn USD die schwache Währung ist, kauft die Strategie das Paar, wo die starke Währung die Basis ist (zum Beispiel Long `EURUSD`).
   - Wenn beide Währungen Nicht-USD sind, sendet die Strategie zwei Aufträge: Long die Top-Währung gegen USD und Short die Down-Währung gegen USD.
6. Wenn `Close Opposite` aktiviert ist und eine gegenläufige Position auf einem Zielpaar noch offen ist, sendet die Strategie zuerst einen schließenden Marktauftrag, bevor sie in einen neuen Trade einsteigt.

## Risikomanagement
- Die Strategie fügt keine expliziten Stop-Loss- oder Take-Profit-Aufträge hinzu; die Risikokontrolle wird durch das `Close Opposite`-Flag zusammen mit manuellen Portfolio-Verwaltungstools gehandhabt.
- Die Einstiegsgröße wird durch die `Volume`-Eigenschaft kontrolliert. Entsprechend der Kontogröße und des gewünschten Exposures pro Segment konfigurieren.

## Unterschiede zur ursprünglichen MQL-Implementierung
- Die Währungsstärkeberechnung verwendet StockSharp-`SimpleMovingAverage`-Indikatoren auf einem einzigen Zeitrahmen. Multi-Zeitrahmen-Koeffizientenstapelung aus dem MQL-Indikator kann durch Anpassen der `Fast MA`- und `Slow MA`-Perioden emuliert werden.
- Schützende Stops werden nicht automatisch nachgezogen; stattdessen konzentriert sich die Strategie auf die Reproduktion der Einstiegs-/Ausstiegslogik und überlässt die erweiterte Risikokontrolle der Portfolio-Ebene von StockSharp.
- Die Auftragsweiterleitung verwendet den High-Level-`RegisterOrder`-Helfer und StockSharp-Sicherheitsreferenzen anstelle von MetaTrader-Handelsobjekten.
