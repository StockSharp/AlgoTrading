# Trailing-Stop-Aktivierungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **Trailing-Stop-Aktivierungs-Strategie** verwaltet schützende Stop-Levels für bestehende Positionen. Sie erzeugt keine Einstiegssignale, sondern passt Stops nach dem Eröffnen einer Position an, um Gewinne abzusichern.

## Parameter

- `TrailingStop` – Abstand in Preiseinheiten, um den sich der Markt zugunsten der Position bewegen muss, bevor ein Trailing Stop aktiviert wird.
- `StopLoss` – optionaler anfänglicher Stop-Loss-Abstand in Preiseinheiten. Auf `0` setzen, um zu deaktivieren.
- `CandleType` – Kerzentyp für die Preisüberwachung.

## Handelsregeln

1. Beim Öffnen einer Position wird ein anfänglicher Stop-Loss gesetzt, wenn `StopLoss` größer als null ist.
2. Sobald der Gewinn `TrailingStop` überschreitet, folgt der Stop-Level dem Preis mit dem angegebenen Abstand.
3. Die Position wird geschlossen, wenn der Preis das Trailing-Stop-Niveau berührt.
4. Die Strategie funktioniert sowohl für Long- als auch für Short-Positionen.

## Hinweise

Diese Strategie ist dafür konzipiert, zusammen mit einer anderen Strategie eingesetzt zu werden, die Einstiegssignale liefert. Sie konzentriert sich ausschließlich auf das Exit-Management durch Trailing Stops.
