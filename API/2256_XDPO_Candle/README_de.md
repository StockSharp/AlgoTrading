# XDPO Kerzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Konvertierung des originalen MQL5-Expertenberaters **Exp_XDPOCandle**. Sie erstellt synthetische Kerzen, indem zwei aufeinanderfolgende exponentielle gleitende Durchschnitte auf die Eröffnungs- und Schlusskurse angewendet werden. Die Farbe der resultierenden Kerze (bullish, bearish oder neutral) bestimmt die Handelsentscheidungen.

## Strategielogik

1. Jede eingehende Marktkerze wird zweimal geglättet:
   - Die erste Glättung verwendet eine EMA der Länge `FastLength`.
   - Die zweite Glättung wendet eine weitere EMA der Länge `SlowLength` auf das Ergebnis der ersten an.
2. Wenn der geglättete Schlusskurs über dem geglätteten Eröffnungskurs liegt, gilt die Kerze als *bullish*.
3. Wenn der geglättete Schlusskurs unter dem geglätteten Eröffnungskurs liegt, gilt die Kerze als *bearish*.
4. Die Strategie eröffnet eine Long-Position, wenn eine bullische Kerze nach einer nicht-bullischen erscheint. Sie eröffnet eine Short-Position, wenn eine bearische Kerze nach einer nicht-bearischen erscheint.
5. Bestehende entgegengesetzte Positionen werden automatisch durch Umkehrung über Marktorders geschlossen.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `FastLength` | Länge der ersten EMA, die auf die Preise angewendet wird. |
| `SlowLength` | Länge der zweiten EMA, die auf das Ergebnis der ersten EMA angewendet wird. |
| `CandleType` | Der Zeitrahmen und Kerzentyp für die Berechnung. |

## Verwendung

1. Verbinden Sie die Strategie mit einem Instrument in der StockSharp-Umgebung.
2. Konfigurieren Sie die Parameter bei Bedarf. Standardwerte sind auf die Originaleinstellungen des Experten abgestimmt.
3. Starten Sie die Strategie. Sie abonniert den angegebenen Kerzentyp und handelt bei Farbwechseln der geglätteten Kerzen.

## Hinweise

- Das Risikomanagement wird durch `StartProtection()` mit Standardeinstellungen gehandhabt. Passen Sie `Volume` und Schutzparameter extern nach Bedarf an.
- Dieses Repository enthält derzeit nur die C#-Version; der Python-Port ist nicht verfügbar.
