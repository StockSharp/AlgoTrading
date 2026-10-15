# Hedger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie platziert eine Limitorder und eine gegenläufige Stoporder, um die ursprüngliche Position abzusichern. Sie funktioniert sowohl im Long- als auch im Short-Modus und verfügt über mehrere Risikokontrollen.

Die Absicherungsorder schützt den Haupthandel, wenn der Preis in die falsche Richtung läuft. Eine 75-50-Trailing-Regel kann den Stop auf die Hälfte des Ziels verschieben, sobald 75 % des Gewinnziels erreicht sind. Optionales Risiko-Hedging kann eine Marktorder gegen die Position eröffnen, wenn ein starker ungünstiger Kursschritt auftritt, und der Stop kann nach einer konfigurierbaren Anzahl von Ticks enger gesetzt werden.

## Details

- **Einstiegskriterien**: Limitorder bei `EntryPrice` und Absicherungs-Stop bei `EntryPrice ± Spread` platzieren.
- **Long/Short**: Über `IsLong` konfiguriert.
- **Ausstiegskriterien**: Stop-Loss, Take-Profit, 75-50-Regel oder Risiko-Hedge.
- **Stops**: Ja, mit optionaler Nachziehung.
- **Filter**: Keine.

## Parameter

- `EntryPrice` – Preis für die ausstehende Order.
- `StopLoss` – Schutz-Stop-Niveau.
- `TakeProfit` – Gewinnziel.
- `Volume` – Ordervolumen.
- `Spread` – Abstand für die Absicherungsorder.
- `IsLong` – Long-Trade bei true, Short-Trade bei false.
- `UseRiskHedge` – bei starkem ungünstigem Kursbewegung gegenläufige Marktorder eröffnen.
- `UseRiskSl` – Stop nach ungünstiger Bewegung von `RiskSlTicks` Ticks nachziehen.
- `RiskSlTicks` – Anzahl der Ticks für die Risiko-Stop-Nachziehung.
- `UseRule7550` – 75-50-Trailing-Regel aktivieren.
