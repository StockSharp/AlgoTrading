# Suffic369-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Suffic369-Strategie ist ein trendfolgendes Breakout-System, das zwei kurze gleitende Durchschnitte mit breiten Bollinger-Bändern kombiniert. Der Fachberater geht Long-Positionen ein, wenn der schnelle einfache gleitende Durchschnitt (SMA) der Schlusskurse die SMA der jüngsten Höchststände überschreitet, während der Markt nahe dem unteren Bollinger-Band handelt. Short-Positionen werden eröffnet, wenn der schnelle SMA den SMA der jüngsten Tiefststände unterschreitet, während der Preis gegen das obere Band drückt. Die konvertierte StockSharp-Version behält die ursprüngliche MQL-Logik bei, drückt sie jedoch mit Kerzenabonnements und Indikatorbindungen auf hoher Ebene aus.

## Indikatoren
- **Fast SMA (Close, Länge = 3)** – misst die kurzfristige Richtung des Schlusskurses.
- **Hoch SMA (Hoch, Länge = 5)** – bildet den Durchschnitt der jüngsten Höchststände und dient als bullische Widerstandsreferenz.
- **Tief SMA (Tief, Länge = 5)** – bildet den Durchschnitt der jüngsten Tiefststände und liefert die bärische Unterstützungsreferenz.
- **Bollinger Bänder (Länge = 156, Abweichung = 1)** – identifiziert Preisextreme im Verhältnis zur Volatilität.

Alle Indikatoren werden bei abgeschlossenen Kerzen aktualisiert. Vorherige Werte werden zwischengespeichert, um die im ursprünglichen MetaTrader-Programm verwendete Verschiebung um einen Takt zu reproduzieren.

## Handelsregeln
### Langer Eintrag
1. Der vorherige schnelle SMA (Schlusskurs) liegt unter dem vorherigen Höchstwert SMA.
2. Der aktuelle Höchstkurs SMA (Schlusskurs) überschreitet das aktuelle Hoch SMA.
3. Der Schlusskurs der Kerze liegt unter dem unteren Bollinger-Band.

### Kurzer Eintrag
1. Der vorherige schnelle SMA (Schlusskurs) liegt über dem vorherigen Tief SMA.
2. Das aktuelle Hoch SMA (Schlusskurs) kreuzt das aktuelle Tief SMA.
3. Der Schlusskurs der Kerze liegt über dem oberen Bollinger-Band.

### Exit-Logik
- **Gegenteiliges Signal:** Eine Long-Position wird geschlossen, wenn ein neues Short-Einstiegssignal auftritt, und umgekehrt.
- **Stop-Loss:** Optionaler, auf Preisschritten basierender Stop, der die Position schützt, sobald er aktiviert ist.
- **Take-Profit:** Optionales preisschrittbasiertes Ziel, das den ursprünglichen TakeProfit-Parameter widerspiegelt.
- **Trailing Stop:** Optionaler Trailing Stop, der hinter profitablen Trades genau wie die MQL-Logik enger wird (verwendet den aktuellen Schlusskurs, um den Stop nur zu verschieben, wenn der Gewinn die konfigurierte Distanz überschreitet).

Die Strategie hält jeweils höchstens eine Position. Nachdem ein Stopp-, Ziel- oder Gegensignal den Handel schließt, wird bis zur nächsten abgeschlossenen Kerze kein neuer Eintrag ausgewertet.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `FastMaLength` | 3 | Dauer des Fastens SMA basierend auf den Schlusskursen. |
| `HighMaLength` | 5 | Länge des SMA, berechnet auf Basis der Kerzenhöchststände. |
| `LowMaLength` | 5 | Länge des SMA, berechnet auf Basis der Kerzentiefs. |
| `BollingerLength` | 156 | Fenstergröße der Bollinger-Bänder. |
| `BollingerDeviation` | 1 | Standardabweichungsmultiplikator für die Bänder. |
| `UseStopLoss` | wahr | Aktiviert den Stop-Loss-Block. |
| `StopLossPoints` | 30 | Stoppdistanz in Instrumentenpreisschritten. |
| `UseTakeProfit` | wahr | Aktiviert den Take-Profit-Block. |
| `TakeProfitPoints` | 60 | Gewinnzielentfernung in Preisschritten. |
| `UseTrailingStop` | wahr | Aktiviert die Trailing-Stop-Verwaltung. |
| `TrailingStopPoints` | 30 | Nachlaufender Offset in Preisschritten. |
| `CandleType` | 15-minütiger Zeitrahmen | Für Berechnungen verwendeter Kerzentyp. |

Alle numerischen Parameter werden als `StrategyParam<T>`-Instanzen bereitgestellt, sodass sie direkt in StockSharp optimiert werden können.

## Risikomanagement
- Stop-Loss, Take-Profit und Trailing-Stops nutzen den Instrumentpreisschritt (`Security.PriceStep`), um Punktabstände in absolute Preise umzuwandeln.
- Trailing-Stops folgen profitablen Bewegungen nur dann, wenn der Preis um mehr als die konfigurierte Distanz gestiegen ist, und reproduzieren so die ursprüngliche Orderänderungslogik.
- `StartProtection()` wird beim Start aufgerufen, um die integrierten Schutzfunktionen von StockSharp zu aktivieren.

## Nutzungshinweise
- Abonnieren Sie die Strategie für ein Instrument, das den ausgewählten Kerzentyp unterstützt.
- Stellen Sie sicher, dass die Eigenschaft `Volume` auf die gewünschte Handelsgröße eingestellt ist, bevor Sie mit der Strategie beginnen.
- Die Strategie wartet auf vollständig gebildete Indikatorwerte, bevor sie Aufträge erteilt. Anfangskerzen werden verwendet, um den Verlauf des Indikators zu ermitteln.
