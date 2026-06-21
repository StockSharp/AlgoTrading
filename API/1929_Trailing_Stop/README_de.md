# Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie implementiert die Trailing-Stop-Logik aus dem ursprünglichen MQL-Skript `TRAILING.mq4`. Sie verwaltet eine bestehende offene Position und schließt sie, wenn der Markt ein bestimmtes Gewinnziel erreicht oder einen Stop-Loss trifft. Wenn der Trailing-Parameter aktiviert ist, folgt das Stop-Niveau dem Preis, um Gewinne zu sichern.

## Parameter
- **TakeProfit** – Gewinnabstand vom Einstiegspreis in absoluten Preiseinheiten.
- **StopLoss** – maximal zulässiger ungünstiger Abstand vom Einstiegspreis.
- **Trailing** – Abstand für das dynamische Trailing des Stop-Niveaus.
- **CandleType** – Kerzenserie für Preisaktualisierungen.

## Funktionsweise
1. Die Strategie abonniert die gewählte Kerzenserie.
2. Nach jeder abgeschlossenen Kerze wird die aktuelle Position bewertet.
3. Bei Long-Positionen schließt die Strategie die Position, wenn der Gewinn *TakeProfit* überschreitet oder der Verlust *StopLoss* überschreitet.
4. Wenn *Trailing* größer als null ist, bewegt sich das Stop-Niveau mit dem Preis nach oben. Wenn der Preis unter den Trailing Stop fällt, wird die Position geschlossen.
5. Short-Positionen folgen derselben Logik in entgegengesetzter Richtung.
6. Der Einstiegspreis wird vom ersten ausgeführten Trade aufgezeichnet und zurückgesetzt, wenn die Position geschlossen wird.

## Hinweise
- Die Strategie verwendet die High-Level-API mit `Bind` zur Verarbeitung von Kerzen.
- Sie eröffnet keine neuen Positionen selbst; sie verwaltet nur eine bereits geöffnete Position.
- Parameter werden über `StrategyParam` bereitgestellt und können optimiert werden.
