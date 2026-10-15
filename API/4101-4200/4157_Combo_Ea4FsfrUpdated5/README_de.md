# Combo EA4 FSF R aktualisiert 5 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Konvertierung des MetaTrader-Expertenberaters „Combo_EA4FSFrUpdated5“. Es kombiniert fünf verschiedene technische Module – gleitende Durchschnitte, RSI, stochastischer Oszillator, parabolischer SAR und Zero-Lag MACD – um jede Handelsentscheidung zu validieren. Eine Position wird nur eröffnet, wenn **alle** aktivierten Module in die gleiche Richtung zeigen, wodurch die strikte Konsenslogik des ursprünglichen EA wiederhergestellt wird. Optionales Nachlaufmanagement, automatische signalbasierte Ausfahrten und die Möglichkeit, nach dem Schließen in die Gegenrichtung zu kippen, bleiben ebenfalls erhalten.

## Indikatorstapel
- **Gleitende Durchschnitte** – Drei konfigurierbare Durchschnitte (MA1, MA2, MA3) mit ATR-basierten Puffern, die falsche Crossover-Signale reduzieren. Fünf verschiedene Aggregationsmodi replizieren die „MA_MODE“-Optionen von EA.
- **Relative Strength Index (RSI)** – Mehrere Bestätigungsmodi, einschließlich klassischer Überkauft/Überverkauft, steigungsbasierter Trenderkennung, einem kombinierten Modus und zonenbasierter Validierung.
- **Stochastic-Oszillator** – Schnelle/langsame/verlangsamte Längen mit optionaler Hoch-/Tiefbandfilterung.
- **Parabolic SAR** – Bietet eine Trendpolaritätsprüfung im Vergleich zum vorherigen Kerzenschluss.
- **Zero-Lag MACD** – Verwendet exponentielle gleitende Durchschnitte ohne Verzögerung, um mit dem gebündelten `ZeroLag_MACD.mq4`-Indikator übereinzustimmen. Unterstützt drei Signalmodi (Trendstruktur, Nulllinienkreuz oder kombiniert).
- **Average True Range (ATR)** – Steuert Stop-Loss-/Take-Profit-Abstände und die MA-Crossover-Puffer.

## Handelslogik
### Teilnahmebedingungen
1. Die Anzeigewerte für alle aktivierten Module müssen verfügbar sein (die Strategie wartet automatisch auf das Aufwärmen).
2. Für jedes aktivierte Modul wird je nach Modus eine bullische oder bärische Richtung berechnet:
   - **Gleitende Durchschnitte** – MA1/MA2/MA3-Kombinationen mit ATR Puffern zur Bestätigung von Richtungsänderungen.
   - **RSI** – Vier Modi für Schwellenwerte, Impuls und Zonenlogik.
   - **Stochastic** – K/D-Kreuzbestätigung mit optionalen Hoch/Niedrig-Filtern.
   - **Parabolic SAR** – Erfordert, dass der Preis über/unter dem SAR-Wert der vorherigen Kerze liegt.
   - **Zero-lag MACD** – Entweder Trendausrichtung, Nulllinienkreuzbestätigung oder beides.
3. Wenn **jedes** aktivierte Modul `Buy` zurückgibt, sendet die Strategie eine Marktkauforder. Wenn jedes Modul `Sell` zurückgibt, wird ein Marktverkaufsauftrag erteilt. Ansonsten wird kein Handel eröffnet.

### Ausstiegsbedingungen
- **Signalbasierte Exits** – Wenn `AutoClose` aktiviert ist, wird dieselbe Konsenslogik mithilfe der dedizierten Exit-Flags (`UseMaClosing`, `UseMacdClosing` usw.) ausgewertet. Eine Long-Position wird geschlossen, wenn sich alle aktivierten Exit-Module auf ein rückläufiges Signal einigen; Eine Short-Position wird geschlossen, wenn sie sich auf ein bullisches Signal einigen. Wenn `OpenOppositeAfterClose` wahr ist, wird die gegenüberliegende Position unmittelbar nach der abschließenden Füllung in die Warteschlange gestellt.
- **Schutzniveaus** – Die anfänglichen Stop-Loss- und Take-Profit-Niveaus werden aus dem aktuellen ATR-Wert (`AtrPeriod`) multipliziert mit `AtrMultiplier` abgeleitet. Der Pip-Puffer des EA wird mit der Schrittgröße des Instruments emuliert. Long-Trades verwenden `ATR × multiplier − buffer` für Stops und `ATR × multiplier + buffer` für Ziele (gespiegelt für Shorts).
- **Trailing Stop** – Wenn `UseTrailingStop` aktiviert ist, wird der Stop-Preis bei jeder fertigen Kerze unter Verwendung des konfigurierten Punktabstands (`TrailingStop`) angepasst.
- **Hard Exits** – Wenn der Preis den Stop-Loss oder Take-Profit-Intrabar erreicht, wird die Position sofort geschlossen und es wird kein entgegengesetzter Einstieg ausgelöst.

### Positionsgrößenbestimmung
- **Statischer Modus** – Wenn `UseStaticVolume` wahr ist, werden Trades mit dem festen Parameter `StaticVolume` platziert.
- **Dynamischer Modus** – Andernfalls leitet die Strategie eine ungefähre Größe aus dem aktuellen Wert des Portfolios und `RiskPercent` ab und greift auf die Basis `Volume` zurück, wenn Portfolio- oder Preisdaten nicht verfügbar sind.

## Parameter
| Gruppe | Parameter | Beschreibung |
|-------|-----------|-------------|
| Einträge | `UseMa` | Aktivieren Sie die Bestätigung des gleitenden Durchschnitts. |
| Einträge | `MaMode` | Wählt die MA-Kombination (schnell/mittel, mittel/langsam, kombiniert usw.). |
| Indikatoren | `Ma1Period`, `Ma2Period`, `Ma3Period` | Perioden der drei gleitenden Durchschnitte. |
| Indikatoren | `Ma1BufferPeriod`, `Ma2BufferPeriod` | ATR Zeiträume, die als Puffer für MA-Gegenprüfungen verwendet werden. |
| Indikatoren | `Ma1Method`, `Ma2Method`, `Ma3Method` | Berechnungsarten des gleitenden Durchschnitts (SMA, EMA, SMMA, LWMA). |
| Indikatoren | `Ma1Price`, `Ma2Price`, `Ma3Price` | Angewandter Preis für jeden gleitenden Durchschnitt. |
| Einträge | `UseRsi` | Aktivieren Sie die RSI-Bestätigung. |
| Indikatoren | `RsiPeriod` | RSI Berechnungszeitraum. |
| Einträge | `RsiMode` | RSI Bestätigungsmodus (überkauft/überverkauft, Trend, kombiniert, Zone). |
| Einträge | `RsiBuyLevel`, `RsiSellLevel` | Schwellenwerte für die Logik „Überverkauft/Überkauft“. |
| Einträge | `RsiBuyZone`, `RsiSellZone` | Zonenschwellenwerte für Modus 4. |
| Einträge | `UseStochastic` | Aktivieren Sie die stochastische Bestätigung. |
| Indikatoren | `StochasticK`, `StochasticD`, `StochasticSlowing` | K/D/langsame Parameter. |
| Einträge | `UseStochasticHighLow` | Stochastik erforderlich, um konfigurierte Hoch-/Tief-Bänder zu durchbrechen. |
| Einträge | `StochasticHigh`, `StochasticLow` | Obere und untere stochastische Schwellenwerte. |
| Einträge | `UseSar` | Aktivieren Sie die parabolische SAR-Bestätigung. |
| Indikatoren | `SarStep`, `SarMax` | SAR Beschleunigungseinstellungen. |
| Einträge | `UseMacd` | Aktivieren Sie die Zero-Lag-Bestätigung MACD. |
| Indikatoren | `MacdFast`, `MacdSlow`, `MacdSignal` | MACD Parameter. |
| Indikatoren | `MacdPrice` | Angewendeter Preis für MACD. |
| Einträge | `MacdMode` | MACD Bestätigungsmodus. |
| Risiko | `UseTrailingStop`, `TrailingStop` | Trailing-Stop-Umschaltung und Distanz (in Punkten). |
| Risiko | `UseStaticVolume`, `StaticVolume`, `RiskPercent` | Steuerelemente zur Positionsgröße. |
| Risiko | `AtrPeriod`, `AtrMultiplier` | ATR Einstellungen für das Risikomanagement. |
| Ausgänge | `AutoClose` | Aktivieren Sie die Exit-Konsenslogik. |
| Ausgänge | `OpenOppositeAfterClose` | Kehren Sie nach einem signalbasierten Ausstieg in die entgegengesetzte Richtung um. |
| Ausgänge | `UseMaClosing`, `MaModeClosing` | Ausgangskonfiguration mit gleitendem Durchschnitt. |
| Ausgänge | `UseMacdClosing`, `MacdModeClosing` | MACD Exit-Konfiguration. |
| Ausgänge | `UseRsiClosing`, `RsiModeClosing` | RSI Exit-Konfiguration. |
| Ausgänge | `UseStochasticClosing` | Stochastic Beenden umschalten. |
| Ausgänge | `UseSarClosing` | SAR Beenden umschalten. |
| Allgemein | `CandleType` | Primärer Zeitrahmen (Standard-5-Minuten-Kerzen). |

## Notizen
- Die Strategie betreibt jeweils eine Nettoposition (Long, Short oder Flat) und spiegelt die Beschränkung „maximal gleiche Aufträge“ von MetaTrader mit einem einfacheren, StockSharp-freundlichen Ansatz wider.
- Ausstehende Gegeneinträge werden nur für signalbasierte Ausstiege in die Warteschlange gestellt und übersprungen, wenn ein Stop-Loss oder Take-Profit den Handel schließt.
- Da die Margin-Anforderungen für das Konto Broker-spezifisch sind, verwendet die dynamische Positionsgröße eine ungefähre risikobasierte Formel. Überprüfen Sie das resultierende Volumen vor der Live-Bereitstellung.
- Stellen Sie sicher, dass die Zero-Lag-Indikatoren MACD und ATR über eine ausreichende Aufwärmhistorie verfügen, bevor Trades erwartet werden, genau wie beim ursprünglichen EA.
