# Bezier ReOpen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Bezier ReOpen-Strategie** verwendet einen benutzerdefinierten Bezier-Kurven-Indikator, um der Trendrichtung zu folgen.
Wenn der Indikator nach oben dreht und der letzte Wert über dem vorherigen liegt, kann die Strategie eine Long-Position eröffnen.
Wenn der Indikator nach unten dreht, kann sie eine Short-Position eröffnen. Bestehende Positionen werden geschlossen, wenn der Indikator die Richtung wechselt.
Nach dem Einstieg werden zusätzliche Positionen jedes Mal neu eröffnet, wenn der Preis um einen benutzerdefinierten Schritt fortschreitet, was ein Skalieren in den Trend ermöglicht.

Diese Implementierung basiert auf dem MetaTrader Expert Advisor `Exp_Bezier_ReOpen.mq5` (ID 16883).

## Details

- **Indikator**: Bezier-Kurve aus den letzten `BPeriod` Preisen und Parameter `T` für die Kurvenspannung.
- **Einstieg**:
  - **Long**: Indikatorsteigung dreht nach oben und aktueller Wert liegt über dem vorherigen.
  - **Short**: Indikatorsteigung dreht nach unten und aktueller Wert liegt unter dem vorherigen.
- **Ausstieg**:
  - **Long**: Indikatorsteigung dreht nach unten.
  - **Short**: Indikatorsteigung dreht nach oben.
- **Wiedereinstieg**: nach dem ersten Einstieg wird jedes Mal eine zusätzliche Order gesendet, wenn sich der Preis `PriceStep` vom letzten Einstiegspreis entfernt, bis zu `PosTotal` Orders.
- **Stops**: optionaler Stop-Loss und Take-Profit in absoluten Preiseinheiten.

## Parameter

- `CandleType` – Kerzen-Zeitrahmen für Berechnungen. Standard: 4 Stunden.
- `BPeriod` – Anzahl der Balken für die Bezier-Berechnung. Standard: 8.
- `T` – Bezier-Kurvenspannung (0..1). Standard: 0.5.
- `PriceType` – Preisquelle für den Indikator (close, open, high, low, median, typical, weighted). Standard: weighted.
- `PriceStep` – Preisabstand zum Senden zusätzlicher Orders. Standard: 300.
- `PosTotal` – maximale Anzahl von Positionen in der Skalierungssequenz. Standard: 10.
- `BuyPosOpen` – Long-Positionen öffnen erlauben. Standard: true.
- `SellPosOpen` – Short-Positionen öffnen erlauben. Standard: true.
- `BuyPosClose` – Longs bei entgegengesetztem Signal schließen. Standard: true.
- `SellPosClose` – Shorts bei entgegengesetztem Signal schließen. Standard: true.
- `StopLoss` – Stop-Loss in Preiseinheiten. Standard: 1000.
- `TakeProfit` – Take-Profit in Preiseinheiten. Standard: 2000.

## Filter-Tags
- Kategorie: Trendfolge
- Richtung: Beide
- Indikatoren: Benutzerdefiniert
- Stops: Optional
- Komplexität: Moderat
- Zeitrahmen: Mittelfristig
- Risikolevel: Moderat
