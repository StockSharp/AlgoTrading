# Micro-Trend-Breakouts-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Micro-Trend-Breakouts**-Strategie ist eine Umwandlung des MetaTrader Expert Advisors "Micro Trend Breakouts" auf die High-Level-API von StockSharp. Sie erkennt kurzlebige Ausbruchsmuster mithilfe linear gewichteter gleitender Durchschnitte, Momentum-Spitzen und MACD-Ausrichtung. Die Strategie eröffnet höchstens eine Position gleichzeitig und verwendet Kerzenschlusskurse, um Einstiege und Ausstiege auszulösen.

## Indikatoren
- **Linear gewichtete gleitende Durchschnitte (LWMA)** - Schnelle und langsame Durchschnitte auf dem Analysezeitrahmen filtern die dominante Marktrichtung.
- **Momentum** - Absolute Momentum-Werte der letzten drei abgeschlossenen Kerzen müssen einen konfigurierbaren Schwellenwert überschreiten, um zu bestätigen, dass der Preis in Ausbruchsrichtung beschleunigt.
- **MACD** - Das klassische MACD-Histogramm wird als Richtungsfilter genutzt (Hauptlinie über der Signallinie für Longs und darunter für Shorts).

## Einstiegslogik
1. Auf eine abgeschlossene Kerze des konfigurierten Zeitrahmens warten.
2. Für Longs muss die schnelle LWMA über der langsamen LWMA liegen (für Shorts darunter).
3. Eine kleine Ausbruchsstruktur bestätigen: Das Tief der Kerze vor zwei Bars muss für Longs unter dem Hoch der vorherigen Kerze liegen (für Shorts gespiegelt).
4. Momentum-Beschleunigung verlangen: Einer der letzten drei absoluten Momentum-Werte muss den konfigurierten Schwellenwert überschreiten.
5. MACD-Ausrichtung validieren:
   - Longs: Die MACD-Hauptlinie muss über der Signallinie liegen, unabhängig davon, ob sie über oder unter null liegt.
   - Shorts: Die MACD-Hauptlinie muss unter der Signallinie liegen, unabhängig von der Position zur Nulllinie.

Wenn alle Prüfungen übereinstimmen, gibt die Strategie eine Marktorder mit dem Standard-Volumenparameter auf.

## Ausstiegslogik und Risikomanagement
- Anfangs-Stop-Loss- und Take-Profit-Niveaus werden in Preisschritten ausgedrückt und beim Einstieg berechnet. Ein Wert von null deaktiviert das jeweilige Niveau.
- Ein optionales Breakeven-Modul verschiebt den Stop zum Einstiegspreis, nachdem der Preis um eine konfigurierte Anzahl von Schritten vorangekommen ist, optional mit zusätzlichem Sicherheitspuffer.
- Trailing-Schutz kann den Stop nach einer profitablen Bewegung enger ziehen. Sobald der Gewinn die Aktivierungsschwelle überschreitet, wird der Stop in Trailing-Distanz vom höchsten (für Longs) oder niedrigsten (für Shorts) seit Einstieg gesehenen Kerzenpreis gezogen.
- Positionsausstiege werden auf jeder abgeschlossenen Kerze bewertet. Wenn der Preis Stop-Loss- oder Take-Profit-Niveaus erreicht, schließt die Strategie die Position per Marktorder und setzt den internen Zustand zurück.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Order Volume` | Marktordervolumen für Einstiege. | `1` |
| `Candle Type` | Zeitrahmen für die Preisanalyse. | `15m time frame` |
| `Fast LWMA` | Periode des schnellen linear gewichteten gleitenden Durchschnitts. | `6` |
| `Slow LWMA` | Periode des langsamen linear gewichteten gleitenden Durchschnitts. | `85` |
| `Momentum Period` | Rückblick des Momentum-Indikators. | `14` |
| `Momentum Threshold` | Minimales absolutes Momentum der letzten drei Kerzen. | `0.3` |
| `MACD Fast / Slow / Signal` | Von MACD verwendete gleitende Durchschnittsperioden. | `12 / 26 / 9` |
| `Stop Loss` | Stop-Distanz in Preisschritten. `0` deaktiviert den Stop. | `20` |
| `Take Profit` | Zieldistanz in Preisschritten. `0` deaktiviert das Ziel. | `50` |
| `Use Trailing` | Aktiviert Trailing-Stop-Logik. | `true` |
| `Trail Activation` | Gewinn in Schritten, der vor Aktivierung des Trailing Stops erforderlich ist. | `40` |
| `Trail Step` | Distanz zwischen Extrem und Trailing Stop in Schritten. | `40` |
| `Use Breakeven` | Aktiviert Breakeven-Stop-Anpassung. | `true` |
| `Breakeven Trigger` | Gewinn in Schritten, der das Breakeven-Modul scharf schaltet. | `30` |
| `Breakeven Padding` | Zusätzliche Schritte beim Verschieben des Stops auf Breakeven. | `30` |

## Hinweise
- Die Strategie abonniert einen einzelnen Kerzenstrom und vermeidet Low-Level-API-Aufrufe, wodurch sie innerhalb der Anforderungen des High-Level-Frameworks bleibt.
- Schutzorders werden nicht direkt an Trades angehängt; stattdessen nutzt die Strategie kerzenbasierte Überwachung kombiniert mit `StartProtection()`, damit die Basisklasse offene Positionen überwacht.
- Alle Inline-Kommentare im C#-Code sind gemäß den Konvertierungsrichtlinien auf Englisch geschrieben.
