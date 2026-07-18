# XDeMarker Histogram Vol Direct-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader 5-Expert **Exp_XDeMarker_Histogram_Vol_Direct** mit der High-Level-API von StockSharp. Sie multipliziert den XDeMarker-Oszillator mit dem gewählten Volumenstrom, glättet sowohl den Oszillator als auch das Volumen mit demselben gleitenden Durchschnitt und vergleicht das Ergebnis mit konfigurierbaren oberen/unteren Niveaus. Handelsentscheidungen werden getroffen, wenn das geglättete Histogramm zwischen aufeinanderfolgenden Bars die Richtung wechselt.

## Indikatorlogik

1. Den klassischen XDeMarker-Oszillator auf dem ausgewählten Zeitrahmen berechnen.
2. Den Oszillator für jede abgeschlossene Kerze mit Tick-Anzahl oder realem Volumen skalieren.
3. Sowohl das Histogramm als auch das Volumen mit dem ausgewählten gleitenden Durchschnittstyp glätten.
4. Das geglättete Volumen mit den konfigurierten Niveau-Multiplikatoren multiplizieren, um vier dynamische Bänder zu erhalten.
5. Die Histogrammrichtung erkennen (steigend oder fallend). Wenn die Richtung wechselt, eröffnet die Strategie eine neue Position in der entsprechenden Richtung und schließt gleichzeitig jeden entgegengesetzten Trade.

Die Glättungsmethode unterstützt einfache, exponentielle, geglättete (RMA/SMMA) und gewichtete gleitende Durchschnitte. Exotische Filter aus der ursprünglichen Bibliothek (JJMA, JurX, ParMA, T3, VIDYA, AMA) sind in diesem Port nicht verfügbar.

## Handelsregeln

- **Long-Einstieg** — aktiviert wenn `Allow Long Entry = true`. Wenn der vorherige Bar eine "hoch"-Richtung hatte und der letzte Bar zu "runter" wechselte, zielt die Strategie auf eine Long-Position von `Volume` Lots.
- **Short-Einstieg** — aktiviert wenn `Allow Short Entry = true`. Ausgelöst wenn der vorherige Bar "runter" war und der neueste Bar auf "hoch" dreht.
- **Long-Ausstieg** — aktiviert wenn `Allow Long Exit = true`. Wenn die vorherige Bar-Richtung "runter" ist, wird die Position liquidiert, es sei denn, ein neuer Short-Einstieg wird im selben Bar ausgelöst.
- **Short-Ausstieg** — aktiviert wenn `Allow Short Exit = true`. Aktiviert wenn die vorherige Bar-Richtung "hoch" ist.

Signale werden einmal pro abgeschlossener Kerze ausgewertet. Die StockSharp-Implementierung behält die ursprüngliche Ein-Bar-Verzögerung bei; der `Signal Bar`-Parameter ist als Referenz vorhanden, aber Werte ungleich `1` werden mit einer Warnung ignoriert.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| Candle Type | Zeitrahmen für den Aufbau von Kerzen für den Indikator. |
| DeMarker Period | Rückblickzeitraum für den Basis-XDeMarker-Oszillator. |
| Volume Source | Auswahl zwischen Tick-Anzahl und realem gehandeltem Volumen. |
| High Level 2 / High Level 1 | Multiplikatoren auf das geglättete Volumen zur Bildung oberer Bänder. |
| Low Level 1 / Low Level 2 | Multiplikatoren für untere Bänder. |
| Smoothing Method | Gleitender Durchschnittstyp auf Histogramm und Volumen angewendet. |
| Smoothing Length | Länge des Glättungsfensters. |
| Smoothing Phase | Kompatibilitäts-Platzhalter (nicht verwendet, aber für Parität beibehalten). |
| Signal Bar | Historischer Versatz, fest auf 1 wie im Expert. |
| Allow Long/Short Entry | Öffnung von Positionen in der jeweiligen Richtung aktivieren. |
| Allow Long/Short Exit | Automatisches Schließen bestehender Trades aktivieren. |

## Implementierungshinweise

- Die Klasse `XDeMarkerHistogramVolDirectIndicator` reproduziert die MT5-Indikatorpuffer und exponiert das geglättete Histogramm, Bänder und Richtungsflags durch einen komplexen Indikatorwert.
- Wenn eine neue Zielexponierung erforderlich ist, sendet die Strategie eine einzelne Marktorder, die die aktuelle Position auf das gewünschte Niveau verschiebt (`Volume`, `-Volume` oder flat). Dies ahmt die sequentiellen Schließ-/Öffnungsaufrufe im ursprünglichen MQL5-Code nach, ohne Orders zu duplizieren.
- Das Chart-Rendering plottet automatisch die Kerzen, den benutzerdefinierten Indikator und die ausgeführten Trades, wenn ein Chart-Bereich verfügbar ist.
