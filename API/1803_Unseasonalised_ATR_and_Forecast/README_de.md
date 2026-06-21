# Strategie für nicht saisonbereinigtes ATR und Prognose
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie analysiert die durchschnittliche Handelsspanne der letzten Kerzen und prognostiziert die nächste Spanne mithilfe linearer Trendregression. Sie platziert keine Trades, sondern zeigt Statistiken an, die für manuelle Entscheidungen genutzt werden können.

## Parameter

- **SampleSize** – Anzahl der letzten Kerzen für die Berechnungen.
- **DesiredRange** – Zielspanne für die Schätzung des Konfidenzintervalls.
- **CandleType** – zu analysierende Kerzenserie.

## Indikatoren

- SimpleMovingAverage – wird zur Berechnung der durchschnittlichen Spanne verwendet.
- StandardDeviation – misst die Volatilität der Spanne.
- Lineare Regression (benutzerdefiniert) – prognostiziert die nächste Spanne und den MAPE.

## Verhalten

Für jede abgeschlossene Kerze führt die Strategie folgendes aus:

1. Berechnet die Spanne (Hoch minus Tief) und aktualisiert Durchschnitt und Standardabweichung.
2. Schätzt ein Konfidenzintervall für die gewünschte Spanne.
3. Erstellt einen linearen Trend der Spannen und prognostiziert die nächste.
4. Bewertet den mittleren absoluten prozentualen Fehler (MAPE) der Prognose.

Werte werden in der Strategieausgabe protokolliert und können auf dem Chart visualisiert werden.

## Hinweise

- Die Strategie ist informativ und führt keine Orders aus.
- Spannen werden in Preiseinheiten gemessen; passen Sie den Parameter `DesiredRange` an Ihr Instrument an.
