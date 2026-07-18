# Strategie für potenzielle Einträge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Potential Entries Strategy** repliziert die Logik des ursprünglichen `EA_PotentialEntries.mq5`-Expertenberaters. Es analysiert Paare der zuletzt abgeschlossenen Kerzen und gibt Geschäfte aus, wenn bestimmte Zwei-Kerzen-Umkehr- oder Momentummuster auftreten. Die Strategie arbeitet jeweils in eine Richtung (bullisch oder bärisch), wählbar über den Parameter `Pattern Side`. Schutzstoppniveaus werden bei jedem Einstieg neu berechnet, um die ursprüngliche MetaTrader-Stoppplatzierung am äußersten Ende des analysierten Kerzenpaars widerzuspiegeln.

Die Implementierung verwendet StockSharps High-Level-API: Sie abonniert den konfigurierten Kerzentyp, verarbeitet den Stream innerhalb von `ProcessCandle`, eröffnet Positionen mit `BuyMarket`/`SellMarket` und schließt Trades durch Marktausgänge, wenn der intern verfolgte Stop-Preis verletzt wird. Diagramme stellen die Kerzenserien zusammen mit den Strategiegeschäften dar, um eine schnelle visuelle Überprüfung zu ermöglichen.

## Daten und Parameter
| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| Allgemein | Musterseite | Richtung des Musterscans: `Bullish` sucht nach bullischen Umkehrungen, `Bearish` sucht nach bärischen Umkehrungen. |
| Handel | Handelsvolumen | Marktauftragsgröße, die für jeden Eintrag verwendet wird. Die Strategie reduziert das entgegengesetzte Risiko, bevor eine neue Position eröffnet wird. |
| Allgemein | Kerzentyp | Zur Mustererkennung verwendete Kerzenserie (Standard: stündliche Kerzen). |

## Handelslogik
Die Strategie wertet die zuletzt abgeschlossene Kerze (`C1`) zusammen mit der vorherigen Kerze (`C2`) aus. Alle Docht- und Körpermaße werden in Preiseinheiten berechnet.

### Bullischer Modus
Bei `Pattern Side = Bullish` lösen die folgenden Setups einen langen Eintrag aus:
1. **Bullischer Hammer**
   - `C1` schließt über seinem Eröffnungskurs, während `C2` bärisch ist.
   - Der untere Docht von `C1` ist mindestens doppelt so groß wie der Körper und mehr als dreimal so groß wie der obere Docht.
   - Es wird eine Market-Buy-Order gesendet und der Stop-Level auf den niedrigeren der Tiefststände von `C1` und `C2` gesetzt.
2. **Bullischer umgekehrter Hammer**
   - `C1` ist bullisch und `C2` ist bärisch.
   - Der obere Docht von `C1` ist mindestens doppelt so groß wie der Körper und mindestens dreimal so groß wie der untere Docht.
   - Führt den gleichen Befehl und die gleiche Stopplogik aus wie beim Hammer-Setup.
3. **Aufwärtsgerichteter Momentum-Builder**
   - `C1` und `C2` sind beide bullisch.
   - Der Bereich von `C1` ist größer als der Bereich von `C2` und der Hauptteil von `C1` ist mindestens doppelt so groß wie der Hauptteil von `C2`.
   - Eröffnet eine Long-Position, wobei der Stopp unter dem Mindesttief des Paares liegt.

### Bärischer Modus
Bei `Pattern Side = Bearish` lösen die folgenden Setups einen kurzen Eintrag aus:
1. **Sternschnuppe**
   - `C1` schließt unter seinem Eröffnungskurs, während `C2` bullisch ist.
   - Der obere Docht von `C1` ist mindestens doppelt so groß wie der Körper und mindestens dreimal so groß wie der untere Docht.
   - Eine Marktverkaufsorder wird gesendet, wobei der Stop über dem höheren Hoch von `C1` und `C2` platziert wird.
2. **Hängender Mann**
   - `C1` ist bärisch und `C2` ist bullisch.
   - Der untere Docht von `C1` ist mindestens doppelt so groß wie der Körper und mehr als dreimal so groß wie der obere Docht.
   - Eröffnet eine Short-Position und verwendet die gleiche Stopplogik wie der Shooting Star.
3. **Bearish Momentum Builder**
   - `C1` und `C2` sind bärisch.
   - Der Hauptteil von `C1` ist größer als der Hauptteil von `C2` und der Bereich von `C1` ist mindestens doppelt so groß wie der Bereich von `C2`.
   - Geht Short ein und speichert den Stop über dem maximalen Hoch der analysierten Kerzen.

### Stoppmanagement und Positionsverwaltung
- Es ist jeweils nur ein Richtungsmodus aktiv. Vor dem Eingehen eines Handels schließt die Strategie jede Position in die entgegengesetzte Richtung.
- Jeder Eintrag zeichnet einen Stop-Preis am äußersten Ende des Kerzenpaares auf. Beim Eintreffen jeder neuen fertigen Kerze prüft die Strategie, ob das Tief (für Long-Positionen) oder das Hoch (für Short-Positionen) das gespeicherte Niveau verletzt und schließt die Position bei Auslösung mit einer Marktorder.
- Wenn keine Position offen ist, wird der gespeicherte Stoppwert gelöscht, wodurch sichergestellt wird, dass veraltete Level nie wieder verwendet werden.

## Nutzungshinweise
- Wählen Sie den Modus `Bullish` oder `Bearish`, je nachdem, ob Sie nach langen oder kurzen Gelegenheiten suchen möchten.
- Die standardmäßigen stündlichen Kerzen können durch jeden anderen verfügbaren Kerzendatentyp ersetzt werden.
- Wie gewünscht gibt es noch keinen Python-Port. Es wird nur die C#-Implementierung bereitgestellt.
- Die Strategie sieht keine Gewinnziele vor. Exits basieren ausschließlich auf der kerzenbasierten Stopplogik oder manuellen Eingriffen.
