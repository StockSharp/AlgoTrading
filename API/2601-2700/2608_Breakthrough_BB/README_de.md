# Ausbruch-BB-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Ausbruch-BB-Strategie repliziert den MetaTrader Expert Advisor *Breakthrough_BB* innerhalb der StockSharp High-Level-API. Das System kombiniert Bollinger Bänder mit einem schnellen einfachen gleitenden Durchschnitt, um explosive Ausbrüche zu erfassen, die auftreten, nachdem sich der Preis nahe den Bandgrenzen komprimiert hat. Trades werden ausschließlich auf abgeschlossenen Kerzen generiert, um Signale deterministisch zu halten und das ursprüngliche MQL5-Verhalten widerzuspiegeln.

## Handelslogik
- **Trendfilter:** Ein einfacher gleitender Durchschnitt (SMA) mit konfigurierbarem Zeitraum validiert die Trendrichtung. Die Strategie vergleicht den letzten SMA-Wert mit dem SMA-Wert von vier Kerzen zuvor. Long-Trades erfordern einen steigenden SMA, während Shorts einen fallenden Verlauf erfordern.
- **Bollinger Bänder Ausbruch:** Die Strategie beobachtet, wie der Schlusskurs von vier Kerzen zuvor mit dem oberen oder unteren Bollinger Band interagierte, und vergleicht ihn mit dem jüngsten Schlusskurs. Ein gültiger Ausbruch tritt auf, wenn der Preis zwischen diesen zwei Zeitstempeln von innerhalb des Bandes nach außen wechselt.
- **Einzelpositionsmodell:** Der Algorithmus hält maximal eine offene Position. Jeder offene Trade wird geschlossen, bevor neue Einstiege bewertet werden, um überlappende Engagements zu verhindern.

## Einstiegsbedingungen
### Long-Setup
1. Der Schlusskurs von vier abgeschlossenen Kerzen zuvor lag unter dem oberen Bollinger Band.
2. Der jüngste Schlusskurs schloss über dem aktuellen oberen Bollinger Band.
3. Der auf der letzten Kerze berechnete SMA-Wert ist größer als der SMA-Wert von vier Kerzen zuvor (positive Steigung).
4. Keine Position ist aktuell offen.

### Short-Setup
1. Der Schlusskurs von vier abgeschlossenen Kerzen zuvor lag über dem unteren Bollinger Band.
2. Der jüngste Schlusskurs schloss unter dem aktuellen unteren Bollinger Band.
3. Der auf der letzten Kerze berechnete SMA-Wert ist niedriger als der SMA-Wert von vier Kerzen zuvor (negative Steigung).
4. Keine Position ist aktuell offen.

Wenn eine Einstiegsbedingung erfüllt ist, sendet die Strategie eine Marktorder mit dem konfigurierten Volumenparameter.

## Ausstiegsregeln
- **Long-Position Ausstieg:** Wenn ein Long-Trade aktiv ist und der letzte Schlusskurs unter die Bollinger-Mittellinie fällt, wird die Position sofort mit einer Markt-Verkaufsorder geschlossen.
- **Short-Position Ausstieg:** Wenn ein Short-Trade offen ist und der letzte Schlusskurs über die Bollinger-Mittellinie steigt, wird die Position mit einer Markt-Kauforder gedeckt.

Diese Ausstiegsregeln imitieren den ursprünglichen Expert Advisor, der Trades entfernte, wann immer der Markt zur Bandmittellinie zurückkehrte.

## Indikatoren
- **Einfacher gleitender Durchschnitt (SMA):** Definiert die Richtungsneigung und liefert den Steigungsvergleich über ein Vier-Kerzen-Intervall.
- **Bollinger Bänder:** Liefert die obere, mittlere und untere Hülle, die zur Erkennung von Ausbruchseinstiegen und zur Verwaltung von Ausstiegen verwendet werden.

## Parameter
| Name | Beschreibung | Standard | Optimierbar |
| --- | --- | --- | --- |
| `MaPeriod` | Länge des SMA für den Trendfilter. | `9` | ✔ |
| `BandsPeriod` | Rückblicklänge für Bollinger-Band-Berechnungen. | `28` | ✔ |
| `Deviation` | Standardabweichungsmultiplikator für Bollinger Bänder. | `1.6` | ✔ |
| `Volume` | Ordergröße (in Lots oder Kontrakten, je nach Instrument). | `1` | ✔ |
| `CandleType` | Von der Strategie verarbeiteter Kerzenaggregationstyp. | Zeitrahmen `1 Stunde` | ✖ |

Alle Parameter exponieren StockSharp `StrategyParam`-Metadaten, sodass sie in der UI angepasst oder im Designer optimiert werden können.

## Datenanforderungen
- Funktioniert mit jedem Instrument, das Kerzendaten bereitstellt, die mit dem ausgewählten `CandleType` kompatibel sind.
- Signale werden nur auf abgeschlossenen Kerzen ausgewertet. Unvollständige Kerzen werden ignoriert, um die Logik deterministisch zu halten.
- Die Standardkonfiguration verwendet stündliche Kerzen, aber jeder von der Datenquelle unterstützte Zeitrahmen kann angegeben werden.

## Zusätzliche Hinweise
- Der Algorithmus verzichtet auf Indikator-Historien-Abfragen und pflegt stattdessen einen rollierenden Vier-Kerzen-Cache für Schluss- und SMA-Werte, entsprechend den Projektrichtlinien.
- Schutzfunktionen wie Stop-Loss oder Take-Profit können bei Bedarf über `StartProtection` hinzugefügt werden; sie sind kein Teil der ursprünglichen MQL-Implementierung und werden daher hier weggelassen.
- Da die Strategie Marktorders ausgibt, stellen Sie ausreichende Liquidität auf dem gewählten Instrument sicher, um Slippage zu minimieren.
