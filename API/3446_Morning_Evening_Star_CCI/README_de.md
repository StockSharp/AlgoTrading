# Morning/Evening Star CCI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den MetaTrader 5 Expert Advisor **Expert_AMS_ES_CCI** unter Verwendung des StockSharp High-Level API. Es sucht nach Drei-Kerzen-Umkehrmustern von Morning Star und Evening Star und erfordert eine Bestätigung durch den Commodity Channel Index (CCI), bevor neue Positionen eröffnet werden. Die Logik funktioniert nur mit fertigen Kerzen und arbeitet mit dem in den Strategieeinstellungen angegebenen primären Wertpapier.

## Handelslogik
- **Morning Star langer Eintrag**
  - Erkennen Sie drei aufeinanderfolgende Kerzen, die ein Morning Star-Muster bilden:
    - Kerze 1: starker bärischer Körper (Körpergröße größer als der durchschnittliche Körper im ausgewählten Fenster).
    - Kerze 2: Kerze mit kleinem Körper und geringerem Abstand als Kerze 1.
    - Kerze 3: schließt über dem Mittelpunkt von Kerze 1.
  - Bestätigen Sie, dass der CCI-Wert in der Signalleiste unter dem negativen Eingabeschwellenwert liegt (Standard: −50).
- **Evening Star Kurzeintrag**
  - Erkennen Sie ein gültiges Evening Star-Muster:
    - Kerze 1: starker bullischer Körper.
    - Kerze 2: Kerze mit kleinem Körper, die über Kerze 1 hinausragt.
    - Kerze 3: schließt unter dem Mittelpunkt von Kerze 1.
  - Bestätigen Sie, dass der CCI-Wert auf der Signalleiste größer als der positive Eingabeschwellenwert ist (Standard +50).
- **Positionsausstiegsregeln**
  - Schließen Sie Short-Positionen, wenn CCI wieder über −NeutralThreshold kreuzt oder unter +NeutralThreshold fällt (Standard ±80).
  - Schließen Sie Long-Positionen, wenn CCI wieder unter +NeutralThreshold fällt oder unter −NeutralThreshold fällt.
  - Es sind keine zusätzlichen Stop-Loss- oder Take-Profit-Regeln eingebettet; Benutzer können bei Bedarf externe Schutzmaßnahmen hinzufügen.

## Indikatoren
- **Commodity Channel Index (CCI)** – Bestätigungsfilter, Standardzeitraum 25.
- **Einfacher gleitender Durchschnitt der Kerzenkörper** – berechnet die durchschnittliche Körpergröße über die letzten *BodyAveragePeriod*-Kerzen (Standard 5), um die Musterstärke zu validieren.

## Parameter
| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `CciPeriod` | Anzahl der Balken, die in der CCI-Berechnung verwendet werden. | 25 | Optimierbar. |
| `BodyAveragePeriod` | Anzahl der Kerzen, die zur Messung der durchschnittlichen Körpergröße verwendet werden. | 5 | Optimierbar. |
| `EntryThreshold` | Absoluter CCI-Wert für neue Trades erforderlich. | 50 | Positiver Wert; Die Strategie prüft ±EntryThreshold. |
| `NeutralThreshold` | Absolutes CCI-Niveau, das die Ausgangszone definiert. | 80 | Positiver Wert; Die Strategie prüft ±NeutralThreshold. |
| `CandleType` | Für die Analyse verwendeter Kerzentyp (Zeitrahmen). | Zeitrahmen 1 Stunde | Ändern Sie die Einstellung, um sie an die gewünschte Auflösung anzupassen. |

## Notizen
- Die Strategie abonniert Kerzenaktualisierungen über `SubscribeCandles` und verwendet `Bind`, um Indikatorwerte zu empfangen.
- Trades werden mit Marktaufträgen unter Verwendung von `BuyMarket` und `SellMarket` ausgeführt.
- Alle Kommentare im Code sind nach Bedarf in Englisch verfasst.
- Um das Risikomanagement zu erweitern, kombinieren Sie die Strategie mit `StartProtection` oder benutzerdefinierten Geldverwaltungsmodulen.
