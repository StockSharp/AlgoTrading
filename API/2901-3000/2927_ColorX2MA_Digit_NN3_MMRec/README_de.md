# ColorX2MA Digit NN3 MMRec-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Reproduziert den Drei-Zeitrahmen-Expertenberater basierend auf dem ColorX2MA Digit-Indikator.
- Verwendet einen benutzerdefinierten doppelt geglätteten gleitenden Durchschnittsindikator, der die ursprüngliche X2MA-Logik mit wählbaren Glättungsmethoden nachahmt (Simple, Exponential, Smoothed, Linear Weighted, Jurik, Kaufman Adaptive).
- Wendet drei unabhängige Indikatorinstanzen (standardmäßig 12h, 6h, 3h) an; jede Instanz kann Long/Short-Exposition gemäß ihren eigenen Einstellungen unabhängig öffnen oder schließen.
- Aggregiert das gewünschte Volumen jedes Zeitrahmens und handelt die Differenz mit Marktorders, sodass die Nettoposition immer der Summe der einzelnen Signale entspricht.
- Signale werden nach `SignalBars` aufeinanderfolgenden Bars mit derselben Neigungsrichtung bestätigt, was den `SignalBar`-Shift in der MQL-Version emuliert.
- Enthält optionale Schalter, um das Öffnen/Schließen von Long- und Short-Exposition separat für jeden Zeitrahmen zu erlauben oder zu verbieten, und reproduziert die "Must Trade"-Flags des Originals.

## Parameter
- **A/B/C Candle Type** – Datentyp (Zeitrahmen) für jede Indikatorinstanz.
- **Fast/Slow Method** – Glättungsmethode für den ersten und zweiten gleitenden Durchschnitt innerhalb des X2MA-Klons.
- **Fast/Slow Length** – Periode der jeweiligen gleitenden Durchschnitte (Standard: 12 und 5).
- **Signal Bars** – Anzahl aufeinanderfolgender Bars, die vor der Akzeptanz einer neuen Richtung erforderlich sind (Standard: 1).
- **Digits** – Rundungsgenauigkeit auf die Indikatorausgabe vor der Neigungsberechnung angewendet (simuliert den `Digit`-Eingang).
- **Price Type** – vom Indikator verwendete Preisquelle (Schlusskurs, Eröffnungskurs, Median, typisch, gewichtet, vereinfacht, Quartal, TrendFollow- und DeMark-Formeln).
- **Allow Long/Short Entry/Exit** – boolesche Flags, die steuern, ob ein bestimmter Zeitrahmen Long/Short-Exposition öffnen oder schließen kann.
- **Volume** – gehandeltes Volumen, das der Zeitrahmen beiträgt, wenn er Long (positiv) oder Short (negativ) ist.

## Signale und Positionsmanagement
1. Jeder Zeitrahmen verarbeitet nur abgeschlossene Kerzen und aktualisiert seinen Indikatorwert.
2. Wenn die Neigung des doppelt geglätteten Durchschnitts positiv wird (Farbindex 0 im MQL-Indikator) und für die konfigurierte Anzahl von Bars so bleibt, wird der Kontext bullisch:
   - Bestehende Short-Exposition wird geschlossen, wenn `Allow Short Exit` aktiviert ist.
   - Eine Long-Position des konfigurierten Volumens wird geöffnet, wenn `Allow Long Entry` aktiviert ist.
3. Wenn die Neigung negativ wird (Farbindex 2), wird der Kontext bärisch:
   - Bestehende Long-Exposition wird geschlossen, wenn `Allow Long Exit` aktiviert ist.
   - Eine Short-Position des konfigurierten Volumens wird geöffnet, wenn `Allow Short Entry` aktiviert ist.
4. Die Strategie summiert die gewünschten Volumina aus den drei Zeitrahmen und sendet eine Marktorder für die Differenz mit dem aktuellen Portfolio, sodass die globale `Position` immer die kombinierte Absicht widerspiegelt.

## Hinweise
- Nicht unterstützte Glättungstypen aus der MQL-Bibliothek (JurX, Parabolic MA, T3, VIDYA/AMA-Variationen) sind nicht verfügbar; bei Bedarf können sie manuell abgebildet werden.
- Der benutzerdefinierte Indikator rundet Werte mit `Digits` und arbeitet nur auf abgeschlossenen Kerzen, wodurch Intrabar-Neuzeichnung vermieden wird.
- Kein integrierter Stop-Loss oder Take-Profit wird hinzugefügt, da das Original MMRec-Money-Management verwendet; die `Volume`-Parameter ermöglichen stattdessen manuelles Sizing.
