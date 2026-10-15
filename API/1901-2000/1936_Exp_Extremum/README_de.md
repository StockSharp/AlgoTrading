# Exp-Extremum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Umkehrungen, die durch den Vergleich von Preisextremen über ein Rückblickfenster erkannt werden. Sie beobachtet, ob die aktuelle Kerze den Preis über vorherige Hochs oder Tiefs treibt, und reagiert, wenn sich das Vorzeichen dieses Vergleichs ändert.

## Funktionsweise

1. Für jede abgeschlossene Kerze ermittelt die Strategie:
   - Das niedrigste Hoch über die letzten *N* Bars.
   - Das höchste Tief über die letzten *N* Bars.
2. Die Differenzen zwischen dem aktuellen Hoch/Tief und diesen Niveaus werden summiert.
3. Eine positive Summe zeigt Aufwärtsdruck, eine negative Summe zeigt Abwärtsdruck an.
4. Wenn das Vorzeichen von vor zwei Bars dem Vorzeichen vom letzten Bar entgegengesetzt ist, erscheint ein Umkehrsignal:
   - Aufwärts dann Abwärts → Long-Position eröffnen.
   - Abwärts dann Aufwärts → Short-Position eröffnen.
5. Optionale Berechtigungen erlauben das unabhängige Deaktivieren des Öffnens oder Schließens von Long-/Short-Positionen.

## Parameter

- `Length` – Indikatorperiode für Extremberechnungen.
- `CandleType` – Zeitrahmen der eingehenden Kerzen.
- `BuyPosOpen` / `SellPosOpen` – Berechtigungen zum Öffnen von Long- oder Short-Positionen.
- `BuyPosClose` / `SellPosClose` – Berechtigungen zum Schließen von Long- oder Short-Positionen.

## Hinweise

Die Strategie verwendet die High-Level-API mit Kerzenabonnements und integrierten `Highest`/`Lowest`-Indikatoren. Positionen werden mit Market-Orders eröffnet und über `ClosePosition()` geschlossen, wenn das entgegengesetzte Signal erscheint.
