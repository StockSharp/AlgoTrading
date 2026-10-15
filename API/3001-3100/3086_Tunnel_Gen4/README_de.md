# Tunnel Gen4 Abgesichertes Gitter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Logik des MetaTrader-Experten "Tunnel gen4" mithilfe der StockSharp-High-Level-API. Sie hält eine marktneutrale Absicherung aufrecht, indem ein erstes Kauf-/Verkaufspaar eröffnet wird, die Position in Richtung des Ausbruchs verdoppelt wird, sobald der Preis eine konfigurierbare Anzahl von Pips zurücklegt, und die gesamte Basket-Position geschlossen wird, wenn dieselbe Distanz über den zweiten Anker hinaus zurückgelegt wird.

## Handelslogik

- **Anfängliche Absicherung:** Sobald keine Exposition besteht, sendet die Strategie gleichzeitige Market-Buy- und Sell-Orders mit dem Volumen `StartVolume`. Die erste Ausführung definiert den Referenzpreis für alle nachfolgenden Entscheidungen.
- **Schritt-Erkennung:** Das konfigurierte `StepPips` wird mit der Tick-Größe des Instruments in ein Preisoffset umgerechnet (mit automatischen Anpassungen für Forex-Notierungen mit drei und fünf Dezimalstellen). Best-Bid/Ask-Aktualisierungen aus dem Level-1-Stream werden mit diesem Offset verglichen.
- **Verstärkungsorder:** Wenn der beste Bid mindestens einen Schritt vom ersten Fill steigt, wird eine Verkaufsorder mit dem doppelten Basisvolumen gesendet. Wenn der beste Ask mindestens einen Schritt fällt, wird stattdessen eine Kauforder derselben Größe ausgegeben. Der erste Fill dieser Order wird zum zweiten Anker.
- **Zyklusbeendigung:** Nachdem der zweite Anker aktiv ist, löst jede weitere schrittgroße Bewegung in eine Richtung eine vollständige Liquidation aller offenen Positionen aus. Sobald beide Seiten geschlossen sind, setzt sich der Zustand zurück und ein neuer Zyklus kann beginnen.
- **Volumenvalidierung:** Der Strategiestart überprüft, dass sowohl das anfängliche als auch das verdoppelte Volumen die Mindest-, Maximal- und Schrittanforderungen des Instruments erfüllen, sodass jede an den Connector gesendete Order ausführbar ist.

## Einstiegsbedingungen

### Long-Verstärkung
- Es gibt mindestens eine offene Position aus der anfänglichen Absicherung.
- Der zweite Anker wurde noch nicht erstellt.
- Der aktuelle beste Ask-Preis ist kleiner oder gleich `first_fill_price - StepPips_in_price`.

### Short-Verstärkung
- Es gibt mindestens eine offene Position aus der anfänglichen Absicherung.
- Der zweite Anker wurde noch nicht erstellt.
- Der aktuelle beste Bid-Preis ist größer oder gleich `first_fill_price + StepPips_in_price`.

## Ausstiegsmanagement

- **Basket-Schließung:** Sobald der zweite Anker definiert ist, werden Marktorders eingereicht, um die kumulierte Long- und Short-Exposition zu schließen, wenn der beste Bid über `second_anchor + StepOffset` steigt oder der beste Ask unter `second_anchor - StepOffset` fällt. Schließungsorders werden verfolgt, um sicherzustellen, dass der Zustand erst nach der Bestätigung aller Trades zurückgesetzt wird.
- **Zustandsreset:** Nachdem beide Seiten geschlossen sind und keine Schließungsorders mehr aktiv sind, löscht die Strategie die internen Anker und wartet auf eine neue Absicherungseröffnung.

## Daten und Indikatoren

- Das Level-1-Abonnement liefert die besten Bid- und Ask-Preise, die für Schrittvergleiche verwendet werden.
- Es sind keine zusätzlichen Indikatoren erforderlich; die gesamte Logik arbeitet auf rohen Kurs-Updates.
- Die Preisschritt-Konvertierung imitiert die MetaTrader-Punkt-zu-Pip-Anpassung, sodass Forex-Symbole mit drei oder fünf Dezimalstellen das gleiche Verhalten wie im Quell-Experten zeigen.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `StartVolume` | Volumen der Kauf- und Verkaufsorders, die die anfängliche Absicherung bilden. |
| `StepPips` | Abstand in Pips, der die Verstärkungsorder und den anschließenden Basket-Ausstieg auslöst. |

## Implementierungshinweise

- StockSharp hält eine Nettopotsition pro Wertpapier. Die Strategie hält interne Expositionszähler, um die separaten Long- und Short-Tickets des MetaTrader-Experten zu emulieren, und gibt Marktorders mit den akkumulierten Volumina aus, wenn die Basket-Position geschlossen wird.
- Da die Logik von Echtzeit-Spreads abhängt, sind Level-1-Daten sowohl in Backtests als auch in Live-Trading-Sitzungen bereitzustellen. Fehlende Bid/Ask-Informationen deaktivieren die Handelsschleife.
- Stellen Sie sicher, dass das Handelskonto gleichzeitige Kauf- und Verkaufsorders für dasselbe Instrument unterstützt, da der Algorithmus davon ausgeht, dass beide Seiten der Absicherung bis zur Erfüllung der Ausstiegsbedingung koexistieren können.
