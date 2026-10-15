# Arrows & Curves-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine Konvertierung des MQL5 Expert Advisors **Exp_Arrows_Curves**.
Sie erstellt einen dynamischen Preiskanal anhand aktueller Hochs und Tiefs und reagiert auf
Ausbrüche. Die Strategie kann Positionen in Abhängigkeit von den Benutzerberechtigungen
und der Trendrichtung öffnen oder schließen.

## Strategielogik
- Berechnung des höchsten Hochs und des niedrigsten Tiefs über den konfigurierten Zeitraum.
- Erweiterung des Bereichs um einen Prozentsatz zur Bildung der äußeren Kanallinien.
- Erstellung innerer Stop-Linien unter Verwendung eines zusätzlichen Prozentsatzes.
- Wenn der Preis über den oberen Kanal ausbricht, Long-Position eingehen; wenn er unter
  den unteren Kanal fällt, Short-Position eingehen.
- Innere Stop-Linien lösen Positionsausstiege aus, wenn die gegenüberliegende Seite des
  Kanals gekreuzt wird.

## Parameter
- `SspPeriod` – Rückblickperiode für Hochs und Tiefs.
- `Channel` – Erweiterungsprozentsatz für die Hauptkanallinien.
- `StopChannel` – zusätzlicher Prozentsatz für die inneren Stop-Linien.
- `CandleType` – Kerzen-Zeitrahmen.
- `BuyPosOpen` / `SellPosOpen` – Öffnung von Long-/Short-Positionen erlauben.
- `BuyPosClose` / `SellPosClose` – Schließung von Long-/Short-Positionen erlauben.

## Indikatoren
- Highest
- Lowest

## Hinweise
Die Strategie arbeitet nur auf abgeschlossenen Kerzen. Stop-Loss- und Take-Profit-Management
sind nicht enthalten; Ausstiege erfolgen über Kanalkreuzungen.
