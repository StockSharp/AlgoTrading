# Lucky Shift Limit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Lucky Shift Limit**-Strategie ist eine direkte Umsetzung des MetaTrader 4 Expertenberaters `Lucky_acnl6p6j89zn91fa.mq4`. Es beobachtet die besten Geld-/Briefkurse in Echtzeit und reagiert auf plötzliche Sprünge, gemessen in MetaTrader „Punkten“ (Pips). Wenn der Briefkurs um die konfigurierte Verschiebungsdistanz nach oben beschleunigt, dämpft die Strategie die Bewegung durch Verkaufen, während ein starker Rückgang des Geldkurses einen konträren Kauf auslöst. Alle offenen Geschäfte werden ständig überwacht und geschlossen, sobald sie profitabel werden oder wenn der schwebende Verlust einen Sicherheitsschwellenwert überschreitet, der mit der ursprünglichen MQ4-Logik identisch ist.

## Daten- und Ausführungsanforderungen

- **Marktdaten** – abonniert nur Kurse der Stufe 1; Es sind keine Kerzen oder Markttiefe erforderlich.
- **Ausführungsstil** – Ein- und Ausstiege basieren auf Marktaufträgen, um die unmittelbaren `OrderSend`-Aufrufe von MetaTrader nachzuahmen.
- **Kontomodus** – funktioniert sowohl mit Hedging- als auch mit Netting-Konten. Bei Netting-Konten akkumuliert die Strategie das Engagement in einer einzigen Position und das Exit-Modul reduziert es.
- **Volumengröße** – Die Standard-Ordergröße stammt von `Strategy.Volume`, aber der Helfer emuliert `AccountFreeMargin/10000` von MetaTrader, wenn der Portfoliowert verfügbar ist.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Shift points` | 3 | Mindestanzahl von MetaTrader Punkten zwischen aufeinanderfolgenden Anfragen/Geboten, die eine neue Bestellung auslösen. Größere Werte filtern Rauschen heraus, kleinere Werte reagieren schneller. |
| `Limit points` | 18 | Maximal zulässige nachteilige Abweichung für einen offenen Handel. Wenn sich der Preis um so viele Punkte gegen die Position bewegt, wird der Handel zwangsweise geschlossen. |

Beide Parameter werden in MetaTrader Punkten ausgedrückt und intern unter Verwendung der Tick-Größe des Instruments in absolute Preis-Offsets umgewandelt. Die Optimierungsgrenzen in der Benutzeroberfläche entsprechen den praktischen Bereichen der MQ4-Version.

## Handelslogik

1. **Initialisierung**
   - Konvertiert die punktbasierten Einstellungen mithilfe von `Security.PriceStep` in tatsächliche Preisentfernungen.
   - Setzt zwischengespeicherte Bid/Ask-Kurse zurück und startet ein Level-1-Abonnement mit `Bind`-Verarbeitung auf hoher Ebene.
2. **Eintrittsbedingungen**
   - Wenn der Briefkurs im Vergleich zum vorherigen Briefkurs um mindestens `Shift points` steigt, sendet die Strategie einen Marktverkaufsauftrag (der die Spitze abschwächt) mit einer Protokollnotiz, die den Auslöser erläutert.
   - Wenn das Gebot im Vergleich zum vorherigen Gebot um mindestens den gleichen Abstand fällt, wird ein Marktkauf eröffnet.
   - Signale können mehrmals hintereinander ausgelöst werden, genau wie beim ursprünglichen Experten, bei dem die Anzahl der gleichzeitigen Positionen nicht eingeschränkt war.
3. **Exit-Management**
   - Jeder Anführungsstrich ruft `TryClosePosition()` auf. Long-Positionen werden sofort geschlossen, wenn der Geldkurs über dem durchschnittlichen Einstiegswert liegt (realisierter Gewinn) oder wenn der Briefkurs um `Limit points` unter dem Einstiegswert liegt (Verlustobergrenze).
   - Short-Positionen spiegeln diese Logik wider und schließen bei profitablen Briefkursen oder wenn der Geldkurs den Einstiegskurs um das konfigurierte Limit übersteigt.
   - Alle Exits verwenden Marktaufträge, um `OrderClose` zu replizieren und sicherzustellen, dass die Position im selben Tick abgeflacht wird.
4. **Positionsgröße**
   - Berechnet das Standardvolumen aus Portfolio-Eigenkapital (`equity / 10,000`, auf eine Dezimalstelle gerundet), sofern verfügbar, passend zum MQ4-Helfer `GetLots()`.
   - Fällt auf die Strategieeigenschaft `Volume` zurück, wenn Aktiendaten fehlen.

## Hinweise zur Implementierung

- Verwendet nur StockSharp-APIs auf hoher Ebene: `SubscribeLevel1().Bind(ProcessLevel1)` macht manuelle Zitat-Listener überflüssig.
- Es werden keine benutzerdefinierten Sammlungen gespeichert. Bisherige Geld-/Briefwerte werden gemäß den Richtlinien in einfachen Nullable-Variablen gespeichert.
- Die Verlustobergrenze hängt von der Tick-Größe des Instruments ab, sodass exotische Symbole mit gebrochenen Pip-Schritten automatisch dem richtigen Preisdelta zugeordnet werden.
- Parameteränderungen während der Laufzeit werden berücksichtigt – die Strategie berechnet die Schwellenwerte neu, wenn Daten der Ebene 1 eintreffen.
- Protokollierungsanweisungen dokumentieren jeden Ein- und Ausstiegsgrund, was Backtesting und Live-Diagnosen vereinfacht.

## Anwendungstipps

- Ideal für hochliquide FX-Paare oder Indizes, bei denen es häufig zu Bid-Ask-Schocks kommt.
- Erwägen Sie die Kombination der Strategie mit Schutzmaßnahmen auf Portfolioebene (`StartProtection`), wenn zusätzliche Stop-Loss- oder Drawdown-Limite erforderlich sind.
- Erhöhen Sie `Shift points` bei verrauschten Feeds, um übermäßigen Handel zu reduzieren, oder verringern Sie ihn, um ultrakurzfristige Bewegungen zu erfassen.
- Die Logik ist von Natur aus konträr; Wenn ein Ausbruchsverhalten gewünscht wird, stellen Sie einfach `Shift points` hoch genug ein oder kombinieren Sie es mit einem anderen Filterindikator.
