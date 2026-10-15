# Auto-Trading-Publish-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie portiert das MetaTrader-4-Hilfsprogramm **"Auto Trading Publish"** nach StockSharp. Statt Marktorders zu senden, konzentriert sie sich darauf, zu steuern, wann Handel erlaubt ist. Sie überwacht die Marktuhr über ein Kerzenabonnement und schaltet das Flag `AutoTradingActive`, sobald die konfigurierte Start- oder Stoppstunde erreicht wird. Das Flag spiegelt das Verhalten des ursprünglichen Hilfsprogramms wider, das den MT4-"AutoTrading"-Button programmatisch umschaltete.

## Handelslogik
- Einen leichten Kerzenstrom abonnieren (standardmäßig Ein-Minuten-Kerzen), um die Marktzeit auch ohne Trades zu verfolgen.
- Wenn eine abgeschlossene Kerze die konfigurierte `StartHour` meldet, das Flag `AutoTradingActive` aktivieren und das Ereignis protokollieren.
- Wenn eine abgeschlossene Kerze die konfigurierte `StopHour` meldet, das Flag `AutoTradingActive` deaktivieren und das Ereignis protokollieren.
- Doppelte Umschaltungen innerhalb derselben Stunde unterdrücken, damit das Log nicht überläuft, wenn mehrere Kerzen oder Ticks in dieser Stunde eintreffen.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `StartHour` | Tagesstunde (0-23), die Auto-Trading aktiviert. |
| `StopHour` | Tagesstunde (0-23), die Auto-Trading deaktiviert. |
| `CandleType` | Zeitrahmen zur Abfrage der Marktuhr. Kleinere Frames reagieren schneller. |

## Nutzungshinweise
- Die Strategie sendet keine Orders; sie stellt nur die Eigenschaft `AutoTradingActive` bereit, die andere Strategien oder Bedienpanels beobachten können, um zu entscheiden, wann Trades gesendet werden.
- Wenn Start- und Stoppstunde gleich sind, läuft das Stop-Ereignis nach dem Start-Ereignis und lässt den Handel deaktiviert, identisch zum ursprünglichen Expert Advisor.
- Wählen Sie einen Kerzenzeitrahmen passend zur gewünschten Umschaltgeschwindigkeit. Ein Ein-Minuten-Zeitrahmen ist ein guter Kompromiss zwischen Reaktionsfähigkeit und Ressourcenverbrauch.

## Unterschiede zur MetaTrader-Version
- MT4 schaltete über Windows-Nachrichten einen globalen Plattformbutton um. StockSharp stellt stattdessen ein Strategie-Flag bereit, wodurch das Verhalten leichter in komplexe Setups integrierbar ist.
- Der StockSharp-Port läuft vollständig in der High-Level-API und lässt sich ohne Low-Level-Message-Hooks leicht mit Charting oder anderen Hilfsstrategien kombinieren.
