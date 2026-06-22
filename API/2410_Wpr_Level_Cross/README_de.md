# WPR Niveau-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des Williams %R-Oszillators, wenn er vordefinierte Überkauf- und Überverkauf-Niveaus kreuzt.

Wenn der Indikator unter das **Low Level** fällt, signalisiert er eine mögliche Umkehr aus einer überkauften Bedingung. Wenn er über das **High Level** steigt, deutet er auf eine mögliche Umkehr aus einer überkauften Bedingung hin. Je nach ausgewähltem **Trend Mode** kann die Strategie in Richtung des Indikators handeln oder die Signale für gegenläufiges Trading umkehren.

## Parameter

- `WprPeriod` – Lookback-Periode für Williams %R.
- `HighLevel` – Überkauf-Schwellenwert.
- `LowLevel` – Überverkauf-Schwellenwert.
- `Trend` – `Direct` handelt mit Indikatorsignalen, `Against` kehrt sie um.
- `EnableBuyEntry` / `EnableSellEntry` – Einstieg in Long-/Short-Positionen erlauben.
- `EnableBuyExit` / `EnableSellExit` – Schließen von Short-/Long-Positionen erlauben.
- `StopLoss` – Stop-Loss-Wert in Preiseinheiten.
- `TakeProfit` – Take-Profit-Wert in Preiseinheiten.
- `CandleType` – Zeitrahmen der für Berechnungen verwendeten Kerzen.

## Funktionsweise

1. Die Strategie abonniert Kerzen und berechnet den Williams %R-Indikator.
2. Bei jeder abgeschlossenen Kerze prüft sie, ob der Indikator die angegebenen Niveaus gekreuzt hat.
3. Abhängig von `Trend` und aktivierten Aktionen öffnet oder schließt sie Positionen mit Marktaufträgen.
4. Optionaler Stop-Loss- und Take-Profit-Schutz wird über `StartProtection` aktiviert.

## Hinweise

- Kommentare im Code sind auf Englisch.
- Nur die C#-Version ist implementiert; die Python-Version ist absichtlich weggelassen.
