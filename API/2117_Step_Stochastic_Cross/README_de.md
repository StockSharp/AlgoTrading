# Step Stochastic Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie verwendet den Step Stochastic-Indikator (einen benutzerdefinierten Oszillator basierend auf ATR), um Umkehrsignale zu generieren. Sie abonniert einen vom Benutzer gewählten Kerzen-Zeitrahmen und berechnet schnelle und langsame Step Stochastic-Linien skaliert von 0 bis 100.

## Einstiegs- und Ausstiegsregeln
- **Long-Einstieg:** Die langsame Linie ist über 50 und die schnelle Linie kreuzt von oben nach unten durch die langsame Linie.
- **Short-Einstieg:** Die langsame Linie ist unter 50 und die schnelle Linie kreuzt von unten nach oben durch die langsame Linie.
- **Long-Ausstieg:** Die langsame Linie ist unter 50 und das Schließen von Long-Positionen ist erlaubt.
- **Short-Ausstieg:** Die langsame Linie ist über 50 und das Schließen von Short-Positionen ist erlaubt.

## Parameter
- `KFast` – Multiplikator für den schnellen Kanal.
- `KSlow` – Multiplikator für den langsamen Kanal.
- `CandleType` – Zeitrahmen der Kerzen.
- `AllowBuyOpen`, `AllowSellOpen`, `AllowBuyClose`, `AllowSellClose` – Berechtigungen für Handelsaktionen.
- `StopLoss`, `TakeProfit` – optionale Schutzlevel in Preiseinheiten.

Die Strategie ruft `StartProtection` auf, um Stop-Loss und Take-Profit anzuwenden, wenn angegeben.

Der `StepStochasticIndicator` ist ein C#-Port des originalen MQL5-Indikators und erzeugt `Fast`- und `Slow`-Werte für jede abgeschlossene Kerze.
