# Auftragsexperte-Strategie (1916)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Marktposition, wenn der Preis des Instruments vordefinierte Niveaus erreicht. Sie ahmt das Verhalten des ursprünglichen MQL-Experten nach, der Aufträge über Chart-Linien verwaltete.

## Funktionsweise
- Abonniert Kerzen eines konfigurierbaren Zeitrahmens.
- Wenn der Schlusskurs die Schwellen `BuyLevel` oder `SellLevel` kreuzt, wird eine Long- oder Short-Marktposition eröffnet.
- Stop-Loss- und Take-Profit-Werte werden vom Einstiegspreis aus mit `StopLossPip` und `TakeProfitPip` berechnet.
- Ein optionaler Trailing-Stop verschiebt den Stop-Loss in Richtung des aktuellen Preises, wenn dieser sich in eine günstige Richtung bewegt.

## Parameter
- **TakeProfitPip** – Abstand vom Einstiegspreis zum Take Profit in Pips.
- **StopLossPip** – Abstand vom Einstiegspreis zum Stop Loss in Pips.
- **EnableTrailingStop** – Trailing-Stop-Logik aktivieren oder deaktivieren.
- **CandleType** – Kerzentyp für Berechnungen.
- **BuyLevel** – Preisniveau, das den Long-Einstieg auslöst (0 deaktiviert).
- **SellLevel** – Preisniveau, das den Short-Einstieg auslöst (0 deaktiviert).

## Hinweise
- Die Strategie verwendet die High-Level-API und verarbeitet nur abgeschlossene Kerzen.
- Das Schutzsystem wird beim Start aktiviert, um versehentlich große Positionen zu vermeiden.
