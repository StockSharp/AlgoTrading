# EMA 6/12 Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Expertenberater, der den Übergang zwischen einem schnellen EMA(6) und einem langsamen EMA(12) tauscht. Es abonniert standardmäßig stündliche Kerzen, berechnet beide gleitenden Durchschnitte und wartet auf einen bestätigten Crossover am Ende einer Kerze, bevor es handelt.

## Handelslogik

- **Eintrag:**
  - Ein bullisches Signal erscheint, wenn EMA(6) EMA(12) überschreitet. Die Strategie eröffnet eine Long-Position, wenn keine aktive Position vorhanden ist.
  - Ein rückläufiges Signal erscheint, wenn EMA(6) unter EMA(12) fällt. Die Strategie eröffnet eine Short-Position, wenn keine aktive Position vorhanden ist.
- **Ausgang:**
  - Wenn `UseCloseSignals` aktiviert ist (Standardverhalten), schließt die Strategie die aktuelle Position, sobald ein entgegengesetzter Crossover erkannt wird. Es wartet auf den nächsten Crossover, bevor es einen neuen Trade eröffnet, und spiegelt damit den ursprünglichen Expert Advisor wider.
  - Optionale Take-Profit- und Trailing-Stop-Schutzmaßnahmen werden über den integrierten `StartProtection`-Helper von StockSharp verwaltet.
- **Positionsgröße:**
  - Bestellungen verwenden den Parameter `OrderVolume` (Standard 1 Los). Vor dem Versenden von Bestellungen werden die Volumina an die Sicherheitseinstellungen angepasst.

## Risikomanagement

- **Trailing Stop:** Wandelt die ursprüngliche „Punkte“-Einstellung in Preisschritte um. Wenn der Stop größer als Null ist, bewegt er sich automatisch in die Richtung des Handels, sobald die Position profitabel wird.
- **Take Profit:** Ausgedrückt in Preisschritten. Zum Deaktivieren auf Null setzen.
- Bei der Strategie werden niemals Durchschnittswerte nach unten oder Pyramiden gebildet. Es ist nur eine Position pro Symbol zulässig.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Zeitrahmen, der zum Aufbau von Kerzen und EMAs verwendet wird. Der Standardwert ist 1 Stunde. |
| `OrderVolume` | Handelsgröße in Losen. |
| `ShortEmaLength` | Zeitraum für das Fasten EMA (Standard 6). |
| `LongEmaLength` | Zeitraum für den langsamen EMA (Standard 12). |
| `UseCloseSignals` | Schließen Sie die aktuelle Position an einer gegenüberliegenden Kreuzung (Standard: aktiviert). |
| `TrailingStopSteps` | Nachlaufdistanz in Preisschritten. Null deaktiviert das Nachziehen. |
| `TakeProfitSteps` | Nehmen Sie die Gewinndistanz in Preisschritten. Null deaktiviert es. |

## Notizen

- Signale werden nur bei fertigen Kerzen verarbeitet, um Intrabar-Rauschen zu vermeiden.
- Die vorherigen EMA-Werte werden jedes Mal zurückgesetzt, wenn die Position auf Null zurückkehrt, um eine saubere Erkennung für den nächsten Crossover sicherzustellen.
- Alle Codekommentare sind auf Englisch verfasst und beim Einrücken werden Tabulatoren gemäß den Projektrichtlinien verwendet.
