# PSAR Trader v2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie handelt Marktumkehrungen mithilfe des Parabolic SAR-Indikators. Eine Position wird eröffnet, wenn der SAR-Wert die Seite relativ zum Preis wechselt, was auf eine mögliche Trendwende hinweist. Der Algorithmus operiert nur innerhalb eines festgelegten Zeitfensters und kann optional eine bestehende Position schließen, wenn ein entgegengesetztes Signal erscheint.

## Strategielogik
- **Indikator**: Parabolic SAR.
- **Kauf** wenn der SAR nach dem Schlusskurs der Kerze nach unten wechselt, nachdem er oberhalb der vorherigen Kerze war.
- **Verkauf** wenn der SAR nach dem Schlusskurs der Kerze nach oben wechselt, nachdem er unterhalb der vorherigen Kerze war.
- Handelt nur im Bereich `StartHour`–`EndHour`.
- Wenn `CloseOnOppositeSignal` aktiviert ist, wird eine Position geschlossen, wenn ein entgegengesetztes Signal erscheint, bevor eine neue geöffnet wird.

### Risikomanagement
Beim Einstieg in eine Position setzt die Strategie interne Take-Profit- und Stop-Loss-Level. Die Position wird automatisch geschlossen, wenn der Preis einen dieser Level berührt.

## Parameter
| Name | Beschreibung |
|------|-------------|
| `CandleType` | Zeitrahmen der für den Handel verwendeten Kerzen. |
| `Step` | Beschleunigungsschritt des Parabolic SAR. |
| `Maximum` | Maximaler Beschleunigungsfaktor des Parabolic SAR. |
| `TakeProfit` | Gewinnziel in Preiseinheiten. |
| `StopLoss` | Stop-Loss in Preiseinheiten. |
| `StartHour` | Handelsbeginnstunde (0–23). |
| `EndHour` | Handelsendstunde (0–23). |
| `CloseOnOppositeSignal` | Aktuelle Position schließen, wenn ein entgegengesetztes Signal erscheint. |

## Hinweise
Dieses Beispiel demonstriert die grundlegende Verwendung der High-Level-API mit einem populären Trendumkehr-Indikator. Passen Sie Parameter und Risikomanagement entsprechend dem gehandelten Instrument und persönlichen Vorlieben an.
