# Strategie mit linearem Regressions-Steigungs-Trigger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie verwendet einen Indikator für die Steigung der linearen Regression und eine abgeleitete Triggerlinie, um Trendänderungen zu identifizieren. Eine Long-Position wird eröffnet, wenn die Triggerlinie die Steigungslinie von unten nach oben kreuzt, während eine Short-Position eröffnet wird, wenn die Triggerlinie die Steigungslinie von oben nach unten kreuzt. Bestehende Positionen werden geschlossen, wenn ein entgegengesetztes Signal erscheint. Der Ansatz ist von der ursprünglichen MQL5-Strategie "Exp_LinearRegSlopeV2" inspiriert.

## Indikatorlogik
1. Die **lineare Regressionssteigung** wird über einen konfigurierbaren Zeitraum auf den Schlusskursen der Kerzen berechnet.
2. Eine **Triggerlinie** wird als `2 * slope - slope[Shift]` berechnet, wobei `slope[Shift]` der Steigungswert von einigen Bars zuvor ist.
3. Kreuzungen zwischen der Trigger- und der Steigungslinie dienen als Handelssignale.

## Handelsregeln
- **Long einsteigen:** Trigger kreuzt die Steigung von unten und Short-Trades sind erlaubt.
- **Short einsteigen:** Trigger kreuzt die Steigung von oben und Long-Trades sind erlaubt.
- **Long aussteigen:** Steigung steigt über den Trigger.
- **Short aussteigen:** Trigger steigt über die Steigung.

## Parameter
- `SlopeLength` – Periode zur Berechnung der linearen Regressionssteigung.
- `TriggerShift` – Anzahl der Bars zur Berechnung der Triggerlinie.
- `EnableLong` – Erlaubt Long-Einstiege.
- `EnableShort` – Erlaubt Short-Einstiege.
- `TakeProfitPercent` – Take-Profit als Prozentsatz des Einstiegspreises.
- `StopLossPercent` – Stop-Loss als Prozentsatz des Einstiegspreises.
- `CandleType` – Zeitrahmen der von der Strategie verwendeten Kerzen.

## Hinweise
- Die Strategie arbeitet ausschließlich auf abgeschlossenen Kerzen.
- Der Schutz über `StartProtection` wendet feste prozentbasierte Take-Profit- und Stop-Loss-Niveaus an.
- Stellen Sie sicher, dass ausreichend historische Daten vorhanden sind, damit der Indikator seine Werte bilden kann.
