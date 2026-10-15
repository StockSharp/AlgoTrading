# Exp Leading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein Kreuzungssystem basierend auf dem benutzerdefinierten **Leading**-Indikator, der von John F. Ehlers in *Cybernetics Analysis for Stock and Futures* beschrieben wird. Der Indikator berechnet zwei Linien:

1. **NetLead** – geglätteter Leading-Filter, gesteuert durch die Koeffizienten `Alpha1` und `Alpha2`.
2. **EMA** – ein einfacher exponentieller gleitender Durchschnitt mit einem konstanten Faktor von 0.5.

Die Strategie arbeitet auf abgeschlossenen Kerzen des ausgewählten Zeitrahmens. Wenn die NetLead-Linie die EMA-Linie **von oben nach unten** kreuzt, wird eine Aufwärtsumkehr erwartet und eine Long-Position eröffnet. Umgekehrt wird eine Short-Position eröffnet, wenn NetLead die EMA-Linie **von unten nach oben** kreuzt. Die vorherige Position, falls vorhanden, wird implizit geschlossen, wenn eine entgegengesetzte Order gesendet wird.

## Parameter

- `Alpha1` – Koeffizient für die zwischengeschaltete Leading-Berechnung. Standard: `0.25`.
- `Alpha2` – Glättungsfaktor für das Leading-Ergebnis. Standard: `0.33`.
- `CandleType` – Kerzendatentyp für Berechnungen. Standard: 4-Stunden-Zeitrahmen.
- `StopLoss` – Stop-Loss in absoluten Preiseinheiten. Standard: `1000`.
- `TakeProfit` – Take-Profit in absoluten Preiseinheiten. Standard: `2000`.

## Handelslogik

1. Jede abgeschlossene Kerze aktualisiert die NetLead- und EMA-Werte.
2. Wenn die vorherige Kerze NetLead über EMA zeigte und die neueste Kerze NetLead unter EMA zeigt, wird eine **Kauf**-Marktorder gesendet.
3. Wenn die vorherige Kerze NetLead unter EMA zeigte und die neueste Kerze NetLead über EMA zeigt, wird eine **Verkauf**-Marktorder gesendet.
4. `StartProtection` wird verwendet, um Stop-Loss- und Take-Profit-Regeln automatisch anzuwenden.

Dieses Beispiel dient zu Bildungszwecken und zeigt, wie eine MetaTrader-Strategie auf die StockSharp High-Level-API portiert werden kann.
