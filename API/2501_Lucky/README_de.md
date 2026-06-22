# Lucky-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Lucky-Strategie ist ein Ausbruchs-Scalper, der schnelle Änderungen zwischen den besten Bid- und Ask-Preisen überwacht. Sie kauft, wenn der Ask-Preis um eine konfigurierbare Anzahl von Pips nach oben springt, und verkauft, wenn der Bid um denselben Betrag fällt. Positionen werden sofort geschlossen, sobald sie profitabel werden, oder wenn sich der Preis nachteilig über eine Schutzschwelle hinaus bewegt.

## Daten und Ausführung

- **Marktdaten**: erfordert Level-1-Quotes für den Zugang zum besten Bid- und Ask-Stream.
- **Order-Typen**: verwendet Market Orders für alle Ein- und Ausstiege, um schnell auf Quote-Schocks zu reagieren.
- **Positionsmodus**: für Hedging-Konten ausgelegt, funktioniert aber auch mit Netting-Konten durch Akkumulation der Netto-Exposition.

## Parameter

- **Shift points** – minimaler Pip-Abstand zwischen aufeinanderfolgenden Quotes, der einen neuen Trade auslöst. Ein höherer Wert filtert Rauschen heraus, während ein niedrigerer selbst auf winzige Sprünge reagiert.
- **Limit points** – maximale nachteilige Bewegung (in Pips), die toleriert wird, bevor eine offene Position zwangsgeschlossen wird. Skaliert auch mit der Instrument-Tick-Größe.
- **Reverse mode** – kehrt die Handelsrichtung um. Wenn aktiviert, öffnen aufwärts gerichtete Ask-Schocks Shorts und abwärts gerichtete Bid-Schocks öffnen Longs.

## Handelslogik

1. **Initialisierung**
   - Konvertiert die punktbasierten Parameter in tatsächliche Preisabstände unter Verwendung der Instrument-Tick-Größe.
   - Abonniert Level-1-Daten und setzt interne Puffer für vorherige Bid- und Ask-Preise zurück.
2. **Einstieg**
   - Wenn der Ask um mindestens den konfigurierten Shift relativ zum vorherigen Ask steigt, öffnet die Strategie einen Long (oder Short im Reverse-Modus).
   - Wenn der Bid um mindestens den Shift relativ zum vorherigen Bid fällt, öffnet die Strategie einen Short (oder Long im Reverse-Modus).
3. **Volumen-Sizing**
   - Die Standard-Ordermengen kommen von der `Volume`-Eigenschaft der Strategie.
   - Wenn Portfolio-Eigenkapital verfügbar ist, emuliert es die MetaTrader-Logik durch Zuweisung von ungefähr `FreeMargin / 10.000`, gerundet auf einen Dezimalot, um sicherzustellen, dass größere Konten mit größeren Größen handeln.
4. **Ausstieg**
   - Long-Positionen werden geschlossen, sobald der Bid den durchschnittlichen Einstiegspreis überschreitet oder der Ask unter den Einstieg um das konfigurierte Limit fällt.
   - Short-Positionen werden geschlossen, wenn der Ask unter den Einstieg fällt oder der Bid über den Einstieg um das Limit steigt.

## Hinweise und Verwendungstipps

- Funktioniert am besten bei hochliquiden FX-Paaren oder Index-CFDs mit deutlichen Quote-Sprüngen.
- Beim Live-Testen mit zusätzlichem Risikomanagement wie Portfolio-Level-Stop-Outs kombinieren.
- **Reverse mode** aktivieren, um den Ausbruch ohne Änderung anderer Parameter in eine Fade-Strategie zu verwandeln.
- Da die Strategie auf jede qualifizierende Quote-Aktualisierung reagiert, erwägen Sie das Drosseln eingehender Daten oder das Erhöhen des Shift-Schwellenwerts bei rauschenden Feeds.
