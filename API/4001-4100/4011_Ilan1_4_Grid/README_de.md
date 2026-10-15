# Ilan 1.4 Basket Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Ilan 1.4 ist ein klassisches Mittelungsgittersystem. Die umgewandelte Strategie abonniert eine einzelne Kerzenserie und eröffnet eine anfängliche Marktposition basierend auf der Richtung der letzten beiden abgeschlossenen Kerzen: Wenn der neuere Schlusskurs unter dem älteren liegt, beginnt der Korb mit einem Verkauf, andernfalls eröffnet er einen Kauf. Wenn sich der Preis um den konfigurierten **Pip-Schritt** gegen den aktiven Korb bewegt, fügt die Strategie optional eine neue Position in die gleiche Richtung hinzu und berechnet den gewichteten durchschnittlichen Einstiegspreis neu.

Alle Geschäfte im Korb werden mit Marktaufträgen ausgeführt. Wenn der Schlusskurs den durchschnittlichen Einstiegspreis zuzüglich der **Take-Profit-Distanz** erreicht, wird der gesamte Korb geschlossen. Ein Trailing Stop, ein fester Stop-Loss, ein aktienbasierter Notstopp und ein Maximum-Life-Time-Schutz reproduzieren die Schutzblöcke des ursprünglichen MetaTrader-Experten.

## Handelsregeln
1. Warten Sie auf die nächste fertige Kerze und bewerten Sie die letzten beiden Abschlüsse.
2. Wenn kein Risiko besteht, eröffnen Sie einen Long-Basket, wenn der letzte Schlusskurs höher als der vorherige ist, und andernfalls einen Short-Basket.
3. Verfolgen Sie den letzten Füllpreis und den gewichteten durchschnittlichen Einstiegspreis des aktiven Warenkorbs.
4. Wenn **Hinzufügen verwenden** aktiviert ist und sich der Preis gegenüber der Position um **Pip Step**-Punkte bewegt, berechnen Sie die nächste Lotgröße und eröffnen Sie einen zusätzlichen Markthandel. Wenn **Vor dem Hinzufügen schließen** aktiviert ist, wird zunächst der vorhandene Warenkorb geschlossen und mit dem skalierten Volumen wieder geöffnet.
5. Berechnen Sie den durchschnittlichen Einstiegspreis nach jeder Füllung neu. Der Korb wird liquidiert, sobald der Preis das durchschnittliche Take-Profit-Niveau erreicht oder wenn eine der Risikoregeln ausgelöst wird.
6. Sobald ein Korb geschlossen ist, bereitet die Logik sofort ein neues Signal vor, das die letzten beiden Kerzenschlüsse verwendet.

## Geldverwaltungsmodi
Der Parameter **Money Management** reproduziert den ursprünglichen `MMType`-Schalter:
- **Behoben** – jede neue Bestellung verwendet das konfigurierte **Anfangsvolumen**.
- **Geometrisch** – Folgeaufträge multiplizieren das Basisvolumen mit `LotExponent^n`, wobei `n` der aktuellen Anzahl offener Trades entspricht.
- **RecoverLastLoss** – nach einem Verlustkorb verwendet die nächste Position das Volumen des letzten geschlossenen Handels multipliziert mit **Lot Exponent**; Bei profitablen Körben wird das Volumen wieder auf den Basiswert zurückgesetzt.

Handelsvolumina werden entsprechend **Volumenziffern** und der Wertpapiervolumenstufe gerundet. Wenn das Runden Null ergeben würde, wird stattdessen das ungerundete Eingabevolumen verwendet.

## Risikokontrollen
- **Take Profit** – schließt den gesamten Warenkorb, sobald der Preis den durchschnittlichen Einstiegspreis ± konfigurierte Punkte erreicht.
- **Stop Loss** – schließt den Korb, wenn sich der Preis um die angegebene Anzahl von Punkten gegenüber dem durchschnittlichen Einstiegspreis bewegt.
- **Verwenden Sie Trailing Stop** mit **Trail Start** und **Trail Stop** – aktiviert ein Trailing-Level, sobald der Korb genügend Punkte gesammelt hat; Der nachlaufende Offset folgt dem Preis, um den Gewinn zu schützen.
- **Verwenden Sie Equity Stop** mit **Equity Risk %** – überwacht den Portfoliowert und schließt den Korb, wenn der variable Verlust den gewählten Prozentsatz des aufgezeichneten Aktienhöchstwerts übersteigt.
- **Timeout verwenden** mit **Max. Öffnungsstunden** – schließt den Warenkorb zwangsweise, wenn er länger als die zulässige Anzahl von Stunden geöffnet bleibt.

## Parameter
- **Kerzentyp** – Zeitrahmen, der zur Generierung von Handelssignalen verwendet wird.
- **Anfangsvolumen** – Anfangslosgröße für einen neuen Korb.
- **Volumenziffern** – Genauigkeit, die beim Runden berechneter Volumina verwendet wird.
- **Geldverwaltung** – Volumenberechnungsmodus (`Fixed`, `Geometric`, `RecoverLastLoss`).
- **Lot-Exponent** – Multiplikator, der von den geometrischen und Wiederherstellungsschemata angewendet wird.
- **Vor dem Hinzufügen schließen** – Schließen Sie alle offenen Geschäfte, bevor Sie die nächste Durchschnittsorder aufgeben.
- **Hinzufügen verwenden** – Mittelungsaufträge insgesamt aktivieren oder deaktivieren.
- **Pip Step** – minimale negative Bewegung (in Preisschritten) vor dem Hinzufügen eines neuen Handels.
- **Take Profit** – Gewinnziel aus dem durchschnittlichen Einstiegspreis.
- **Stop-Loss** – maximal zulässige negative Abweichung vom durchschnittlichen Einstiegspreis.
- **Verwenden Sie Trailing Stop / Trail Start / Trail Stop** – Trailing-Stop-Konfiguration.
- **Max Trades** – maximale Anzahl zulässiger Durchschnitts-Trades in einem Korb.
- **Verwenden Sie Equity Stop / Equity Risk %** – Parameter des Floating-Loss-Schutzes.
- **Verwenden Sie Timeout / Max Open Hours** – Lebensdauerkontrolle für jeden Korb.

## Konvertierungshinweise
- MetaTrader ausstehende Order-Helfer wurden durch direkte Markt-Orders ersetzt, da die Durchschnittslogik im Originalcode immer sofort ausgeführt wurde.
- Der Trailing-Block funktioniert jetzt für den aggregierten Warenkorb, anstatt jede Bestellung einzeln zu ändern; Die Auslöseabstände entsprechen den ursprünglichen Standardwerten.
- Das Portfolioeigenkapital wird über das Portfolioobjekt StockSharp überwacht, um die Equity-Stop-Routine des Experten zu emulieren.
- Positionsdurchschnitte und Warenkorbstatistiken werden innerhalb der Strategie berechnet, ohne dass Sammlungen pro Trade gespeichert werden, wobei die übergeordneten API-Richtlinien eingehalten werden.
