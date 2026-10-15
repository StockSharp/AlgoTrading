# MSL EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

MSL EA ist eine Ausbruch-Strategie, die dynamische Support- und Resistenzlinien aus jüngsten lokalen Extrempunkten aufbaut. Die Strategie erkennt kurzfristige Fraktal-Hochs und -Tiefs, passt sie um eine angegebene Distanz in Ticks an und eröffnet Positionen, wenn der Preis über diese Niveaus hinaus schließt. Sie wurde aus der ursprünglichen MQL4-Implementierung konvertiert.

## Funktionsweise

1. Der Algorithmus verfolgt Kerzenhochs und -tiefs, um lokale Extrempunkte zu bestimmen.
2. Das höchste Hoch und das niedrigste Tief unter den letzten *Level* erkannten Extrempunkten werden als Resistenz- und Support-Linien gespeichert.
3. Jede Linie wird um *Distance* Ticks verschoben, um Marktrauschen zu berücksichtigen.
4. Wenn der Schlusskurs über die obere Linie bricht, wird eine Long-Position eröffnet; wenn er unter die untere Linie bricht, wird eine Short-Position eröffnet.
5. Die Anzahl gleichzeitiger Trades ist durch *Max Trades* begrenzt.

## Parameter

- **Max Trades** – maximale Anzahl erlaubter offener Positionen.
- **Level** – Anzahl lokaler Extrempunkte für den Linienaufbau.
- **Distance** – Versatz vom Extrempunkt in Ticks beim Setzen der Linien.
- **Candle Type** – Zeitrahmen der von der Strategie verarbeiteten Kerzen.

## Hinweise

Diese C#-Version verwendet die High-Level StockSharp API und enthält englische Kommentare. Die Risikomanagementfunktionen aus der ursprünglichen MQL4-Hilfsbibliothek werden auf grundlegende Positionsprüfungen vereinfacht.
