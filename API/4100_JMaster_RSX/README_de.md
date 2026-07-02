# JMaster RSX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die JMaster RSX-Strategie ist eine direkte Konvertierung des MetaTrader 4-Expertenberaters **jMasterRSXv1**. Das System richtet die Jurik RSX-Oszillatorwerte aus, die auf einem schnellen (M5) und einem langsamen (M30) Zeitrahmen berechnet wurden. Wenn der höhere Zeitrahmen in eine bullische oder bärische Richtung weist und der schnelle Oszillator den überverkauften/überkauften Bereich erreicht, geht die Strategie eine Position in die entsprechende Richtung ein. Alle Signale werden beim Öffnen des neuen Balkens unter Verwendung der vorherigen vollständig geschlossenen Kerzen ausgewertet und entsprechen der MT4-Implementierung, die auf `shift = 1`-Werte verwies.

## Indikatoren und Daten
- **Jurik RSX (Länge = `RsxLength`) im schnellen Zeitrahmen** – bewertet den Oszillator auf der durch `FastCandleType` definierten Kerzenserie (Standard-5-Minuten-Balken). Die Konvertierung reproduziert den ursprünglichen rekursiven Filter, der vom benutzerdefinierten `rsx.mq4`-Indikator verwendet wird.
- **Jurik RSX im langsamen Zeitrahmen** – berechnet mit der gleichen Länge auf der durch `SlowCandleType` definierten Kerzenserie (Standard-30-Minuten-Balken). Der zuletzt abgeschlossene langsame Wert wird um einen Balken verzögert, bevor er verwendet wird, was das MT4-Verschiebungsverhalten widerspiegelt.

## Eingabelogik
1. Warten Sie, bis sich eine neue schnelle Kerze öffnet (entspricht der Verarbeitung einer fertigen Kerze in StockSharp).
2. Rufen Sie den vorherigen schnellen RSX-Wert und den vorherigen langsamen RSX-Wert ab (eine langsame Kerze hinter dem aktuellen Schlusskurs).
3. **Langes Setup:** langsamer RSX liegt über `MidlineLevel` (Standard 50) *und* schneller RSX liegt unter `OversoldLevel` (Standard 25).
4. **Kurze Einrichtung:** langsamer RSX liegt unter `MidlineLevel` *und* schneller RSX liegt über `OverboughtLevel` (Standard 75).
5. Eröffnen Sie eine Marktorder mit einem Volumen von `Volume`, wenn derzeit keine Position aktiv ist.

## Exit-Logik
- Schließen Sie eine offene Long-Position, sobald die Short-Bedingungen erfüllt sind (langsamer RSX unter der Mittellinie und schneller RSX über der überkauften Schwelle).
- Schließen Sie eine offene Short-Position, sobald die Long-Bedingungen erfüllt sind (langsamer RSX über der Mittellinie und schneller RSX unter der überverkauften Schwelle).
- Die Strategie stapelt keine Positionen; Es reduziert sich immer auf einen flachen Zustand, bevor ein neuer Eintrag in Betracht gezogen wird.

## Positionsgrößen
- Bestellungen werden mit einem festen Volumen aufgegeben, das durch den Parameter `Volume` gesteuert wird (Standard: `0.1`).
- Es ist keine adaptive Geldverwaltung oder Pyramidenlogik implementiert. Dies spiegelt das Standardverhalten des ursprünglichen EA wider, als `DecreaseFactor` auf Null belassen wurde.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `FastCandleType` | Kerzentyp für die schnelle RSX-Berechnung | `M5` |
| `SlowCandleType` | Kerzentyp für die langsame RSX-Berechnung | `M30` |
| `RsxLength` | Von beiden RSX-Instanzen gemeinsam genutzte Lookback-Länge | `14` |
| `OverboughtLevel` | Schneller RSX-Schwellenwert für kurze Einträge | `75` |
| `OversoldLevel` | Schneller RSX-Schwellenwert für lange Einträge | `25` |
| `MidlineLevel` | Langsame RSX-Mittellinie, die bullische/bärische Regime trennt | `50` |
| `Volume` | Auftragsvolumen für Markteintritte | `0.1` |

## Nutzungshinweise
- Stellen Sie sicher, dass historische Daten fertige Kerzen für beide konfigurierten Zeitrahmen liefern; Die Strategie reagiert erst, nachdem eine Kerze geschlossen wurde.
- Da der langsame RSX-Wert absichtlich um einen Balken verzögert wird, erscheinen Intrabar-Umkehrungen im höheren Zeitrahmen einen Balken später – dies entspricht der Quelle EA und verhindert einen Look-Ahead-Bias.
- Der eingebettete RSX-Indikator gibt Werte im Bereich von 0–100 aus und ermöglicht bei Bedarf einen direkten Vergleich mit anderen Oszillatoren.
