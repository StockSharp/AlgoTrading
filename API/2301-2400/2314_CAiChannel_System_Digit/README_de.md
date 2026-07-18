# CaiChannel System Digit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein vereinfachter StockSharp-Port des MetaTrader-Experten **i-CAiChannel System Digit**.

Der Algorithmus überwacht einen Volatilitätskanal, der aus einem gleitenden Durchschnitt und einer Standardabweichung (Bollinger Bänder) aufgebaut ist.
Wenn eine Kerze außerhalb des Kanals schließt und die nächste Kerze wieder zurückkehrt, handelt die Strategie in Richtung des Wiedereintritts.

## Parameter
- `Length` – Periode des gleitenden Durchschnitts.
- `Width` – Standardabweichungsmultiplikator.
- `Candle Type` – Zeitrahmen für die Verarbeitung.

## Handelslogik
1. Kerzen des gewählten Zeitrahmens abonnieren.
2. Bollinger Bänder mit den angegebenen Parametern berechnen.
3. Wenn die vorherige Kerze über dem oberen Band schloss und die aktuelle Kerze wieder innerhalb schließt, Long gehen.
4. Wenn die vorherige Kerze unter dem unteren Band schloss und die aktuelle Kerze wieder innerhalb schließt, Short gehen.
5. Die Position wird umgekehrt, wenn das entgegengesetzte Signal auftritt.

Alle Signale werden nur auf abgeschlossenen Kerzen generiert.
