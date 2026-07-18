# Glückscode-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Lucky Code ist ein kurzfristiger Breakout-Scalper, der vom ursprünglichen MetaTrader „Lucky_code“ Expert Advisor abgeleitet wurde. Die Strategie beobachtet die Spread-Extreme und reagiert, wenn der beste Brief um einen konfigurierbaren Abstand über den vorherigen Kurs springt oder der beste Geldkurs unter diesen fällt. Alle Geschäfte werden aggressiv geschlossen: Gewinne werden sofort mitgenommen, sobald sich der Preis positiv entwickelt, während Verluste reduziert werden, wenn eine ungünstige Abweichung eine Schutzgrenze überschreitet.

## Daten und Ausführung

- **Marktdaten**: erfordert einen stetigen Strom von Level-1-Kursen, um die neuesten besten Geld- und Briefwerte zu lesen.
- **Auftragstypen**: Verwendet Marktaufträge für jeden Ein- und Ausstieg, um die Tick-basierte Ausführung der MQL-Version widerzuspiegeln.
- **Positionsmodus**: Unterstützt sowohl Netting- als auch Hedging-Konten. Mehrere Füllungen akkumulieren sich zu einer einzigen Nettoposition, die als Block verwaltet wird.

## Parameter

- **Punkte verschieben** – Mindestpunktzahl (Pips) zwischen aufeinanderfolgenden Anführungszeichen, die einen neuen Eintrag freischaltet. Höhere Werte verringern die Handelsfrequenz und die Lärmempfindlichkeit.
- **Grenzpunkte** – maximal zulässiger Gegenabstand, bevor Positionen zwangsweise geschlossen werden. Der Wert wird mit der Tick-Größe des Instruments in Preiseinheiten umgerechnet.

## Handelslogik

1. **Initialisierung**
   - Konvertiert punktbasierte Parameter mithilfe der Tick-Größe des Wertpapiers in reale Preisversätze.
   - Abonniert Daten der Ebene 1 und setzt die internen Puffer für den zuletzt gesehenen Geld- und Briefkurs zurück.
2. **Eintrittsregeln**
   - Wenn der beste Brief um mindestens die konfigurierte Verschiebung über den vorherigen Brief steigt, eröffnet die Strategie eine Short-Position (entspricht dem ursprünglichen EA-Verhalten, das nach Aufwärtsspitzen verkauft).
   - Wenn das beste Gebot um mindestens die gleiche Verschiebung unter das vorherige Gebot fällt, eröffnet die Strategie eine Long-Position, um die Erholung zu nutzen.
3. **Volumendimensionierung**
   - Beginnt mit der Strategieeigenschaft `Volume`.
   - Wenn der Portfoliowert verfügbar ist, wird die Größe auf `round(Equity / 10,000, 1)` Lots erhöht, wodurch die auf der Marge basierende Größenbestimmung von MetaTrader nachgeahmt wird.
4. **Ausgangsregeln**
   - Das Long-Engagement wird sofort geschlossen, sobald der Bid den durchschnittlichen Einstiegspreis übersteigt oder der Ask um das konfigurierte Verlustlimit sinkt.
   - Das Short-Engagement wird geschlossen, sobald der Brief unter den Einstiegspreis fällt oder der Geldkurs um die Verlustgrenze darüber steigt.

## Hinweise zur Implementierung

- Die Strategie reagiert auf jede Angebotsaktualisierung. Erwägen Sie daher, verrauschte Feeds zu drosseln oder den Verschiebungsparameter in Produktionsumgebungen zu erhöhen.
- Da Marktaufträge sowohl für Eröffnungs- als auch für Schlussgeschäfte verwendet werden, stellen Sie sicher, dass ausreichend Liquidität vorhanden ist, um Slippage-Spitzen bei schnellen Kurssprüngen zu vermeiden.
- Bei der Live-Ausführung der Strategie werden zusätzliche Risikokontrollen auf Portfolioebene (täglicher Stopp, maximaler Drawdown usw.) empfohlen.
