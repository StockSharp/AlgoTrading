# Münzwurf-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **Münzwurf-Strategie** wählt zufällig Long oder Short bei jeder neuen Kerze, wenn keine Position offen ist. Nach dem Schließen einer Position wird die nächste Handelsgröße bei einem Verlusthandel mithilfe eines Martingale-Multiplikators erhöht. Die Strategie schließt Positionen mit festen Take-Profit- und Stop-Loss-Niveaus, die in Preisschritten definiert sind, und kann optional Gewinne nach einer bestimmten Distanz verfolgen.

## Parameter

- `Volume` – Basisauftragsgröße für den ersten Versuch.
- `Martingale` – Multiplikator, der nach einem Verlusthandel auf das Volumen angewendet wird.
- `MaxVolume` – Obergrenze für die Positionsgröße nach Martingale-Erhöhungen.
- `TakeProfit` – Gewinnziel in Preisschritten.
- `StopLoss` – Verlustlimit in Preisschritten.
- `TrailingStart` – Abstand in Preisschritten, ab dem das Trailing aktiv wird.
- `TrailingStop` – Trailing-Stop-Abstand in Preisschritten.
- `CandleType` – Zeitrahmen der Kerzen für die Entscheidungsfindung.

## Funktionsweise

1. Bei jeder abgeschlossenen Kerze prüft die Strategie, ob eine offene Position vorhanden ist.
2. Wenn eine Position besteht, wird Gewinn oder Verlust anhand des aktuellen Schlusskurses überwacht. Sobald Take-Profit-, Stop-Loss- oder Trailing-Stop-Bedingungen erfüllt sind, wird die Position geschlossen.
3. Wenn keine Position offen ist, wird eine virtuelle Münze geworfen:
   - Kopf öffnet eine Long-Position.
   - Zahl öffnet eine Short-Position.
4. Wenn der vorherige Trade ein Verlust war, wird das Volumen mit `Martingale` multipliziert, begrenzt durch `MaxVolume`.
5. Der Trailing Stop wird aktiviert, sobald sich der Preis um `TrailingStart` in die günstige Richtung bewegt.

## Hinweise

Dieses Beispiel dient zu Lehrzwecken und zeigt, wie man mit Zufallssignalen und Positionsgrößen mithilfe der StockSharp High-Level API arbeitet.
