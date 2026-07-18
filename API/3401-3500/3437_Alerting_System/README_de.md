# Strategie für Warnsysteme
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Alerting System**-Strategie ist eine getreue StockSharp-Umsetzung des MetaTrader 4-Expertenberaters `AlertingSystem.mq4`. Das Originalskript zeichnet zwei horizontale Linien und spielt einen Ton ab, wann immer der Markt sie berührt. Die StockSharp-Version erreicht das gleiche Ziel, indem sie Kurse der Stufe 1 (bester Geld-/Briefkurs) abonniert und Journalmeldungen druckt, wenn eine der konfigurierbaren Alarmstufen überschritten wird.

## Kernidee

1. Registrieren Sie einen Level1-Datenstrom, damit die Strategie Tick-für-Tick-Aktualisierungen von Geboten und Briefen erhält und den Handler MQL `OnTick` widerspiegelt.
2. Lesen Sie die benutzerdefinierten Ebenen `UpperPrice` und `LowerPrice`. Ein Wert von `0` deaktiviert die entsprechende Warnung, genau wie das Entfernen der horizontalen Linie in MetaTrader.
3. Vergleichen Sie jedes eingehende Gebot mit der oberen Ebene und jeden Brief mit der unteren Ebene.
4. Senden Sie eine einzelne Protokollbenachrichtigung, wenn der Preis ein aktives Niveau überschreitet, und warten Sie, bis der Markt in die sichere Zone zurückkehrt, bevor Sie die Warnung erneut aktivieren. Dies verhindert laute Doppelwarnungen und behält gleichzeitig die Absicht des ursprünglichen Tonauslösers bei.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `UpperPrice` | `0` | Obere horizontale Alarmstufe. Auf `0` setzen, um die Prüfung zu deaktivieren. |
| `LowerPrice` | `0` | Niedrigere horizontale Alarmstufe. Auf `0` setzen, um die Prüfung zu deaktivieren. |

Beide Parameter werden über die Designer-Benutzeroberfläche verfügbar gemacht. Sie können vor dem Start oder während der Strategieausführung geändert werden; Bei der nächsten Angebotsaktualisierung werden die neuen Ebenen verwendet.

## Laufzeitverhalten

- **Datenabonnement**: `GetWorkingSecurities` fordert Level1-Daten an und stellt so sicher, dass die Strategie auch ohne Kerzen oder Trades Gebots-/Briefaktualisierungen erhält.
- **Initialisierung**: Wenn `OnStarted` ausgelöst wird, protokolliert die Strategie die aktuell konfigurierten Ebenen, damit der Bediener die Einrichtung überprüfen kann.
- **Alarmerkennung**: Hilfsmethoden (`CheckUpperAlert` und `CheckLowerAlert`) speichern interne Flags, um sicherzustellen, dass jeder Verstoß genau eine Benachrichtigung erzeugt, bis der Markt wieder über den Schwellenwert hinausgeht.
- **Kein Handel**: Bei der Konvertierung werden keine Aufträge versendet. Es handelt sich lediglich um ein Warndienstprogramm, das dem Verhalten des Skripts MetaTrader entspricht, das nur einen Ton abspielte.
- **Reset-Behandlung**: `OnReseted` löscht die internen Flags, sodass der nächste Lauf mit neuen Alarmzuständen beginnt.

## Typische Verwendungsschritte

1. Wählen Sie im StockSharp Designer das gewünschte Instrument aus und hängen Sie `AlertingSystemStrategy` an.
2. Geben Sie die obere und/oder untere Alarmstufe an. Belassen Sie einen Wert bei `0`, um diese Seite zu ignorieren.
3. Starten Sie die Strategie. Im Protokoll werden Einträge angezeigt, die bestätigen, welche Warnungen aktiv sind.
4. Überwachen Sie das Journalfenster. Wenn der Geldkurs über das obere Niveau steigt oder der Briefkurs unter das untere Niveau fällt, zeichnet die Strategie eine beschreibende Nachricht auf.

## Konvertierungshinweise

- Der ursprüngliche MetaTrader-Advisor hat zwei verschiebbare horizontale Linien erstellt. StockSharp verwendet stattdessen numerische Parameter, wodurch der Workflow deterministisch bleibt und besser für die algorithmische Ausführung geeignet ist.
- MetaTrader löste bei jedem qualifizierenden Tick die Funktion `PlaySound` aus. Um eine Überlastung des Protokolls zu vermeiden, entprellt die Konvertierung Warnungen, bis der Preis wieder in den akzeptablen Bereich fällt.
- Die Logik bleibt bewusst indikatorfrei: Es sind nur Rohkurse erforderlich, sodass die Strategie für jeden Zeitrahmen und jedes Instrument funktioniert, das Level-1-Daten liefert.

## Klassifizierung

- **Kategorie**: Dienstprogramme / Warnungen
- **Handelsrichtung**: Keine
- **Ausführungsstil**: Ereignisgesteuerte Überwachung
- **Datenanforderungen**: Bid/Ask der Stufe 1
- **Komplexität**: Einfach
- **Empfohlener Zeitrahmen**: Beliebig (angebotsorientiert)
- **Risikomanagement**: Nicht anwendbar (keine Positionen eröffnet)

Diese Dokumentation fasst die StockSharp-Implementierung zusammen und hebt die praktischen Schritte hervor, die zum Reproduzieren des MetaTrader-Benachrichtigungsworkflows innerhalb der Plattform erforderlich sind.
