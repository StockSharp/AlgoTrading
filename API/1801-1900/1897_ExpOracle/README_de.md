# Exp Oracle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein C#-Port des MetaTrader-Expertenberaters **Exp_Oracle**. Sie basiert auf einem benutzerdefinierten *Oracle*-Indikator, der den Relative Strength Index (RSI) und den Commodity Channel Index (CCI) kombiniert, um die Marktrichtung mehrere Bars im Voraus zu prognostizieren. Der Indikator erzeugt zwei Linien:

- **Oracle-Linie** – Rohmischung der CCI- und RSI-Extremwerte.
- **Signallinie** – geglätteter gleitender Durchschnitt der Oracle-Linie.

Die Strategie bietet drei Trading-Modi zur Interpretation dieser Linien:

1. **Breakdown** – eröffnet Positionen, wenn die Signallinie das Nullniveau kreuzt.
2. **Twist** – reagiert auf lokale Wendepunkte der Signallinie.
3. **Disposition** – handelt auf Kreuzungen zwischen der Signal- und der Oracle-Linie.

## Parameter

- `OraclePeriod` – Periode für RSI- und CCI-Berechnungen.
- `Smooth` – Anzahl der Bars zum Glätten der Signallinie.
- `Mode` – Algorithmus zur Signalgenerierung (`Breakdown`, `Twist` oder `Disposition`).
- `CandleType` – Zeitrahmen der eingehenden Kerzen.
- `AllowBuy` – aktiviert Long-Einstiege.
- `AllowSell` – aktiviert Short-Einstiege.
- `Volume` – Strategievolumen, geerbt von der Basisklasse `Strategy`.

## Einstiegs- und Ausstiegsregeln

### Breakdown
- **Kaufen**, wenn die Signallinie über null kreuzt.
- **Verkaufen**, wenn die Signallinie unter null kreuzt.

### Twist
- **Kaufen**, wenn die Signallinie nach einem Rückgang nach oben dreht.
- **Verkaufen**, wenn die Signallinie nach einem Anstieg nach unten dreht.

### Disposition
- **Kaufen**, wenn die Signallinie die Oracle-Linie von unten nach oben kreuzt.
- **Verkaufen**, wenn die Signallinie die Oracle-Linie von oben nach unten kreuzt.

Bestehende Positionen werden geschlossen und umgekehrt, wenn ein entgegengesetztes Signal erscheint. Die Strategie verwendet der Einfachheit halber Marktorders.

## Indikatorlogik

Für jeden Bar:
1. RSI und CCI mit dem angegebenen `OraclePeriod` berechnen.
2. Vier Divergenzwerte aus Differenzen zwischen aktuellen CCI- und RSI-Werten bilden.
3. Die Oracle-Linie ist die Summe aus maximaler und minimaler Divergenz.
4. Die Signallinie ist der einfache gleitende Durchschnitt der Oracle-Linie über `Smooth` Bars.

Dieser Ansatz versucht, kurzfristige Preisbewegungen durch die Kombination von Momentum (RSI) und Kanal (CCI) zu prognostizieren.

## Hinweise

- Die Strategie arbeitet ausschließlich auf abgeschlossenen Kerzen.
- Schutz-Stops sind nicht implementiert; verwenden Sie bei Bedarf externe Risikokontrollen.
