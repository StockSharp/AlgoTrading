# Manuelle Trading-Lightweight-Utility-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der ursprüngliche Expert Advisor „Manual Trading Lightweight Utility“ ist ein kompaktes MetaTrader-Panel, das Schaltflächen zum Wechseln zwischen Markt-, Limit- und Stop-Orders bereitstellt, Volumina unabhängig für Kauf- und Verkaufsaktionen anpasst und automatisch Stop-Loss- und Take-Profit-Offsets hinzufügt. Dieser C#-Port stellt den gleichen Workflow in StockSharp wieder her, indem er jede Bedienfeldschaltfläche als Strategieparameter darstellt. Die Strategie erzeugt keine autonomen Signale; Es wartet auf Ihre manuellen Anweisungen und führt dann die angeforderte Aktion mithilfe des übergeordneten API aus, während es Schutzausgänge überwacht.

## Neu erstellte Funktionalität
- **Einmalige Kauf- und Verkaufsanfragen.** Zwei boolesche Umschalter emulieren die Bedienfeldtasten. Wenn Sie `BuyRequest` oder `SellRequest` auf `true` setzen, wird basierend auf dem ausgewählten Modus genau eine Markt-, Limit- oder Stop-Order ausgelöst und der Schalter wird sofort auf `false` zurückgesetzt.
- **Automatisch oder manuell ausstehende Preise.** Jede Seite kann entweder die MetaTrader-Offsets (`LimitOrderPoints` und `StopOrderPoints`) wiederverwenden oder einen manuellen absoluten Preis akzeptieren. Bei der automatischen Preisgestaltung wird der aktuell beste Geld-/Briefkurs oder der letzte Schlusskurs der Kerze verwendet, wenn keine Kurse verfügbar sind.
- **Unabhängige Volumes.** Sie können ein Standard-Volume zwischen beiden Seiten teilen oder Volumes pro Seite aktivieren, um den Lot Control-Schalter der MQL-Version widerzuspiegeln.
- **Punktbasierter Schutz.** `TakeProfitPoints` und `StopLossPoints` übersetzen die MetaTrader Punktabstände mithilfe des Instruments `PriceStep` in Preisversätze. Die Strategie überwacht abgeschlossene Kerzen und schließt die Position mit einer Marktorder, wenn ein Schutzniveau durchbrochen wird.
- **Kommentar-Feedback.** Bei jeder manuellen Aktion wird ein Protokolleintrag geschrieben, der den konfigurierten `OrderComment` enthält, wodurch es einfach ist, den ausgeführten Befehlen ohne visuelle Anzeige zu folgen.

## Strategieablauf
1. Die Strategie abonniert den von `CandleType` ausgewählten Kerzentyp. Fertige Kerzen liefern die Referenzpreise für Offsets und Risikoüberwachung.
2. Für jede fertige Kerze gilt die Strategie:
   - Aktualisiert die Basisklasse `Volume` mit `DefaultVolume` (nützlich für die visuelle Inspektion in StockSharp).
   - Erkennt Änderungen in `BuyRequest` und `SellRequest` und markiert sie als ausstehende Aktionen.
   - Sobald die Marktdaten bereit sind (`IsFormedAndOnlineAndAllowTrading()`), führt es die angeforderten Aktionen aus, löst Preise für ausstehende Aufträge auf und protokolliert das Ergebnis.
   - Ruft den Risikomanager auf, der den Einstiegspreis aufzeichnet, wenn sich die Nettoposition ändert, und Marktausstiege ausgibt, wenn Stop-Loss- oder Take-Profit-Schwellenwerte überschritten werden.
3. Wenn die Position wieder flach ist, werden alle internen Zustände zurückgesetzt, sodass die nächste manuelle Anforderung mit einem sauberen Schiefer beginnt.

## Parameter
- **`CandleType`** – Marktdatenreihen, die für Preisreferenzen und Risikomanagement verwendet werden.
- **`BuyOrderMode` / `SellOrderMode`** – Wählen Sie zwischen `MarketExecution`, `PendingLimit` oder `PendingStop` für jede Seite.
- **`UseAutomaticBuyPrice` / `UseAutomaticSellPrice`** – automatische Offset-Preisgestaltung aktivieren. Deaktivieren Sie die Angabe eines festen absoluten Preises.
- **`BuyManualPrice` / `SellManualPrice`** – Manuelle Preise für ausstehende Orders werden angewendet, wenn die automatische Preisberechnung deaktiviert ist (zum Ignorieren auf `0` setzen).
- **`DefaultVolume`** – gemeinsames Bestellvolumen, wenn einzelne Bände deaktiviert sind.
- **`UseIndividualVolumes`** – schaltet das Analogon zur Chargenkontrolle um. Wenn diese Option aktiviert ist, überschreiben die nächsten beiden Parameter das gemeinsam genutzte Volume.
- **`BuyVolume` / `SellVolume`** – Volumen pro Seite.
- **`TakeProfitPoints` / `StopLossPoints`** – Schutzabstände ausgedrückt in MetaTrader Punkten. Null deaktiviert die entsprechende Funktion.
- **`LimitOrderPoints` / `StopOrderPoints`** – Offsets, die auf automatische Limit- und Stop-Preise angewendet werden, ebenfalls gemessen in Punkten.
- **`BuyRequest` / `SellRequest`** – kurzzeitige Schalter, die die Bedienfeldtasten emulieren. Sie werden nach Bearbeitung der Anfrage automatisch zurückgesetzt.
- **`OrderComment`** – Freiformtext, der an das Protokoll angehängt wird, wenn eine Aktion ausgeführt wird.

## Nutzungsrichtlinien
1. Konfigurieren Sie `CandleType` entsprechend der Granularität, die Sie für Offsets und Risikoprüfungen verwenden möchten. Der standardmäßige Zeitrahmen von einer Minute ähnelt dem tickgesteuerten Verhalten des MetaTrader-Skripts und bleibt gleichzeitig mit historischen Backtests kompatibel.
2. Wählen Sie, ob Sie mit einem einzelnen `DefaultVolume` arbeiten oder `UseIndividualVolumes` aktivieren möchten, um Kauf- und Verkaufsvolumina separat zu steuern. Die Volumina müssen positiv bleiben.
3. Entscheiden Sie, wie ausstehende Preise berechnet werden sollen. Lassen Sie `UseAutomatic*Price` aktiviert, um die Punktversätze von MetaTrader zu replizieren, oder deaktivieren Sie es und geben Sie die Werte `BuyManualPrice` / `SellManualPrice` explizit an.
4. Legen Sie `TakeProfitPoints` und `StopLossPoints` nach Bedarf fest. Wenn sie größer als Null sind, rechnet die Strategie sie mit dem Instrument `PriceStep` in Preisabstände um und schließt die Position mit einer Marktorder, sobald eine Kerze den entsprechenden Schwellenwert überschreitet. Fehlt dem Symbol ein konfigurierter `PriceStep`, wird eine Warnung protokolliert und Schutzabstände übersprungen.
5. Um eine Bestellung aufzugeben, ändern Sie `BuyRequest` oder `SellRequest` von `false` in `true`. Die Strategie löst die Anfrage bei der nächsten fertigen Kerze auf, sendet den gewählten Auftragstyp, schreibt einen Protokolleintrag und setzt das Flag zurück, damit die Aktion nicht automatisch wiederholt wird.
6. Führen Sie eine Aktion erneut aus, indem Sie den entsprechenden Parameter erneut umschalten. Anfragen bleiben inaktiv, wenn der erforderliche Preis nicht gelöst werden kann (z. B. weil ein manueller Preis Null ist); Korrigieren Sie die Konfiguration und schalten Sie erneut um, um es erneut zu versuchen.

## Unterschiede zum ursprünglichen MQL-Dienstprogramm
- Die MetaTrader-Diagrammobjekte werden durch StockSharp-Parameter ersetzt. Jede Schaltfläche und jeder Schalter des ursprünglichen Bedienfelds ist jetzt eine bearbeitbare Eigenschaft, die über die Benutzeroberfläche oder über Automatisierungsskripte gesteuert werden kann.
- Schutzniveaus werden bei Überschreitung mit Marktaufträgen ausgeführt, anstatt separate Stop/Limit-Schutzaufträge zu registrieren. Dadurch bleibt die Implementierung auf der übergeordneten Ebene API und die manuelle Verwaltung von Auftragslebenszyklen wird vermieden.
- Automatische Preise fallen auf den letzten Kerzenschluss zurück, wenn die besten Geld-/Briefkurse nicht verfügbar sind, wodurch ein deterministisches Verhalten bei Backtests gewährleistet wird, bei denen Auftragsbuchdaten fehlen könnten.

## Notizen
- Die Strategie speichert den Einstiegspreis immer dann, wenn sich die Nettoposition ändert. Wenn Sie in einen Trade einsteigen, verankern sich die schützenden Offsets wieder beim Kerzenschluss, der die neue Größe widerspiegelt.
- Die Spread-Kompensation wird in die Stop-Loss-Berechnung einbezogen, indem der beste bekannte Spread (oder ein Preisschritt, wenn Quotes fehlen) zum konfigurierten Punktabstand addiert wird, was die MQL-Logik widerspiegelt, die die Verkaufsstopps um den aktuellen Spread erweitert.
- Protokolleinträge enthalten den konfigurierten Kommentar, den Auftragstyp, den Preis (für ausstehende Aufträge) und das Volumen und bieten so einen präzisen Prüfpfad für jede manuelle Aktion.
