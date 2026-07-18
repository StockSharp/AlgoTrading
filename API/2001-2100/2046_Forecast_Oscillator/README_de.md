# Prognose-Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie adaptiert den klassischen Forecast-Oszillator-Indikator für StockSharp. Sie kombiniert eine lineare Regressionsbasis mit Tillson-T3-Glättung, um Trendumkehrungen hervorzuheben. Ein Kaufsignal erscheint, wenn der Oszillator seine geglättete Linie von unten kreuzt, während die geglättete Linie unter null verbleibt. Ein Verkaufssignal wird bei den entgegengesetzten Bedingungen erzeugt.

Der Algorithmus folgt der ursprünglichen MQL-Implementierung und unterstützt das separate Aktivieren oder Deaktivieren der Positionseröffnung und -schließung.

## Details

- **Einstiegskriterien**:
  - **Long**: Oszillator kreuzt T3 von unten und T3 ist negativ.
  - **Short**: Oszillator kreuzt T3 von oben und T3 ist positiv.
- **Long/Short**: Beide Richtungen werden unterstützt.
- **Ausstiegskriterien**:
  - Entgegengesetzte Signale, wenn die entsprechenden Schließungsoptionen aktiviert sind.
- **Stops**: Keine.
- **Filter**: Keine.
