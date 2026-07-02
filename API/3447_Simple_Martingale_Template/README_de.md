# Einfache Martingale-Vorlagenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert die ursprüngliche MetaTrader-Idee „Einfache Martingale-Vorlage“ in StockSharp. Es analysiert fertige Kerzen eines konfigurierbaren Zeitrahmens mithilfe eines Paares einfacher gleitender Durchschnitte (SMA). Ein Breakout-Filter prüft, ob der Schlusskurs der vorherigen Kerze das Hoch oder Tief einer noch früheren Kerze durchbricht, um die Richtung zu bestätigen. Die Positionsgröße folgt einer Martingal-Sequenz: Nach jedem Verlustzyklus wird das nächste Handelsvolumen multipliziert, während profitable Zyklen das Volumen auf die konfigurierte Basisgröße zurücksetzen.

## Handelslogik
1. Abonnieren Sie Kerzen des Zeitrahmens `CandleType`. An der Signalgenerierung nehmen nur fertige Kerzen teil.
2. Berechnen Sie einen schnellen SMA und einen langsamen SMA beim Kerzenschluss.
3. Erzeugen Sie ein **Kaufsignal**, wenn:
   - der letzte abgeschlossene Kerzenschluss liegt über dem Fast SMA,
   - das schnelle SMA liegt über dem langsamen SMA,
   - Bei der vorherigen Kerze lag der schnelle SMA unter dem langsamen SMA und
   - Der letzte Schlusskurs der Kerze liegt über dem Hoch der Kerze vor zwei Balken.
4. Erzeugen Sie ein **Verkaufssignal**, wenn die symmetrischen Bedingungen nach unten eintreten, einschließlich eines Schlusskurses, der unter dem Tief der Kerze vor zwei Balken liegt.
5. Wenn ein Signal ausgelöst wird und keine offenen Positionen oder aktiven Aufträge vorhanden sind, senden Sie einen Marktauftrag mit dem aktuell berechneten Martingalvolumen.
6. Fügen Sie synthetische Stop-Loss- und Take-Profit-Levels hinzu, indem Sie zukünftige Kerzen überwachen. Wenn der Preis eines der beiden Niveaus erreicht, schließen Sie die offene Position.
7. Nachdem eine Position geschlossen und der Portfoliosaldo aktualisiert wurde:
   - Wenn das Guthaben gestiegen ist, setzen Sie die Lautstärke auf den Wert `BaseVolume` zurück.
   - Wenn der Saldo gesunken ist, multiplizieren Sie das letzte Handelsvolumen mit `Multiplier` und passen Sie es an den Volumenschritt des Instruments an.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `StopLossPoints` | Abstand vom Einstieg bis zum Schutzstopp in Preispunkten. |
| `TakeProfitPoints` | Abstand vom Einstieg bis zum Gewinnziel in Preispunkten. |
| `BaseVolume` | Anfängliche Losgröße für den Martingalzyklus. |
| `Multiplier` | Faktor, der nach einem Verlust auf die vorherige Losgröße angewendet wird. |
| `FastPeriod` | Länge des schnellen SMA, der für die Richtungsabweichung verwendet wird. |
| `SlowPeriod` | Länge des langsamen SMA zur Trendbestätigung. |
| `CandleType` | Zeitrahmen der von der Strategie verarbeiteten Kerzen. |

## Money-Management
- Die Martingalleiter reagiert strikt auf realisierte Gleichgewichtsänderungen. Kleine Schwankungen (±0,01 Geldeinheiten) werden ignoriert, um Rauschen zu vermeiden.
- Die Volumina sind auf die Instrumente `VolumeStep`, `MinVolume` und `MaxVolume` abgestimmt, um gültige Bestellgrößen sicherzustellen.
- Stop-Loss- und Take-Profit-Niveaus werden auf Candle-Extremen (Hoch/Tief) überwacht, anstatt Börsenaufträge zu erteilen, was die ursprüngliche MQL-Implementierung widerspiegelt, die Marktausstiege nutzte.

## Nutzungshinweise
- Wählen Sie eine Zeitrahmen- und Symbolkombination, die genügend historische Kerzen erzeugt, damit sich beide SMAs bilden können, bevor der Handel aktiviert wird.
- Passen Sie `StopLossPoints` und `TakeProfitPoints` an die Teilstrichgröße des Symbols an. Sie stellen die Anzahl der Punkte dar, keine Preiseinheiten.
- Erwägen Sie das Testen verschiedener Multiplikatoren und Basisvolumina, um den Kapitalbedarf zu kontrollieren, da Martingalsequenzen schnell wachsen.
- Die Strategie fordert zu Beginn die Integration von `StartProtection()` in die standardmäßigen Risikomanagementfunktionen von StockSharp auf.
