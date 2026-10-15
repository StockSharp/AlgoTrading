# MasterMind 3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt extreme Umkehrungen mit vier **Williams-%R**-Indikatoren mit unterschiedlichen Zeiträumen. Wenn alle Indikatoren auf tiefe überverkaufte Werte fallen, geht die Strategie eine Long-Position ein. Wenn alle Indikatoren auf starke überkaufte Werte steigen, geht sie eine Short-Position ein.

## Handelslogik

1. Kerzen des gewählten Zeitrahmens abonnieren.
2. Vier Williams-%R-Indikatoren mit den Zeiträumen 26, 27, 29 und 30 berechnen.
3. **Kaufen** wenn alle Indikatoren unter `-99.99` liegen.
4. **Verkaufen** wenn alle Indikatoren über `-0.01` liegen.
5. Signale werden nur bei abgeschlossenen Kerzen verarbeitet.

Das Ordervolumen wird aus der Strategie-Eigenschaft `Volume` entnommen. Bestehende entgegengesetzte Positionen werden automatisch durch eine Market-Order der erforderlichen Größe geschlossen.

## Parameter

| Name | Beschreibung | Standard |
|------|--------------|---------|
| `WprPeriod1` | Länge des ersten Williams-%R-Indikators | 26 |
| `WprPeriod2` | Länge des zweiten Williams-%R-Indikators | 27 |
| `WprPeriod3` | Länge des dritten Williams-%R-Indikators | 29 |
| `WprPeriod4` | Länge des vierten Williams-%R-Indikators | 30 |
| `CandleType` | Typ und Zeitrahmen der Kerzen | 1-Minuten-Kerzen |

## Hinweise

* Die Strategie verwendet die High-Level-API mit `Bind` für die Indikatorverarbeitung.
* Keine Stop-Loss- oder Take-Profit-Levels enthalten; die Position wird bei entgegengesetzten Signalen umgekehrt.
