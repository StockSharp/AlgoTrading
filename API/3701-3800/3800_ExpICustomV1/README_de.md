# ExpICustomV1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung

Die **ExpICustomV1-Strategie** ist ein StockSharp-Port des MetaTrader-Experten `exp_iCustom_v1`. Die Strategie liest Handelssignale von einer konfigurierbaren Indikatorinstanz und reagiert auf Werte ungleich Null in den ausgewählten Puffern. Wenn der Kaufpuffer ungleich Null ist, eröffnet die Strategie eine Long-Position, während der Verkaufspuffer einen Short-Einstieg auslöst. Schützende Stop-Loss-, Take-Profit-, Trailing- und Break-Even-Logik reproduzieren die Geldverwaltungsoptionen des ursprünglichen Experten.

> **Hinweis:** Es wird nur die C#-Implementierung bereitgestellt. Eine Python-Version ist noch nicht verfügbar.

## Handelslogik

1. Abonnieren Sie den durch **Kerzentyp** angegebenen primären Zeitrahmen und verarbeiten Sie nur geschlossene Kerzen.
2. Instanziieren Sie den durch **Indikatornamen** definierten Indikator und wenden Sie die durch Schrägstriche getrennten **Indikatorparameter** an (unterstützt sowohl `Name=Value`-Paare als auch geordnete numerische Werte).
3. Speichern Sie die endgültigen Indikatorausgaben in einem Verlaufspuffer, damit bei späteren Kerzen auf jede Verschiebung zugegriffen werden kann.
4. Wenn der Kaufpufferwert bei **Indicator Shift** nicht Null ist, eröffnet/behält die Strategie eine Long-Position. Wenn der Verkaufspuffer ungleich Null ist, eröffnet/behält die Strategie eine Short-Position.
5. Wenn beide Puffer gleichzeitig Werte ungleich Null zurückgeben, heben sich die Signale gegenseitig auf, um mehrdeutige Einträge zu vermeiden.
6. Optional kann **Close On Reverse** die aktuelle Position verlassen, bevor auf das entgegengesetzte Signal reagiert wird.
7. Die Schlaflogik erzwingt eine Mindestanzahl von Balken zwischen aufeinanderfolgenden Einträgen in derselben Richtung. Der Timer kann abgebrochen werden, wenn das entgegengesetzte Signal ausgelöst wird, wenn **Schlafmodus abbrechen** aktiviert ist.
8. Positionen sind durch Stop-Loss, Take-Profit, optionalen Trailing-Stop und Break-Even-Sperre geschützt. Alle Entfernungen werden in Preispunkten angegeben.

## Anzeigekonfiguration

* **Indikatorname** – Vollständiger Typname oder kurzer StockSharp-Indikatorname (zum Beispiel `SMA`, `MACD`, `BollingerBands`).
* **Indikatorparameter** – Durch Schrägstriche getrennte Liste, die auf den Indikator angewendet wird. Sowohl `Length=14/Width=2` als auch geordnete Werte wie `14/2/0.7` werden unterstützt.
* **Blöcke überschreiben** – Mit bis zu fünf Ersetzungen können Sie Parameterwerte während der Optimierung nach Index anpassen, ähnlich wie bei den `Opt_X`-Eingaben im ursprünglichen Experten. Indizes sind nullbasiert.

## Risiko- und Geldmanagement

* **Basisvolumen** – Betrag, der mit jeder Marktorder gesendet wird.
* **Stop Loss / Take Profit** – Absolute Abstände in Punkten vom Einstiegspreis.
* **Trailing Stop** – Wird nach dem angegebenen Gewinn aktiviert und behält den konfigurierten Abstand zum Extrempreis bei.
* **Break Even** – Verschiebt den Stop nach dem angegebenen Gewinn in Richtung Einstiegspreis und sperrt optional zusätzliche Punkte.

## Parameterreferenz

| Parameter | Beschreibung |
|-----------|-------------|
| Kerzentyp | Zeitrahmen, der für die Indikator- und Signalauswertung verwendet wird. |
| Indikatorname | Typname der Indikatorinstanz. |
| Indikatorparameter | Durch Schrägstriche getrennte Liste der Indikatorparameter. |
| Puffer kaufen / Puffer verkaufen | Pufferindizes, die die Kauf-/Verkaufsmarkierungen enthalten. |
| Indikatorverschiebung | Beim Lesen von Indikatorpuffern wird eine historische Verschiebung angewendet. |
| Blöcke überschreiben | Ersetzen Sie bestimmte Parameterpositionen zur Laufzeit. |
| Schlafriegel | Mindestbalken zwischen Einträgen in derselben Richtung. |
| Schlafen abbrechen | Setzen Sie den Sleep-Timer nach einem Gegensignal zurück. |
| Bei Rückwärtsgang schließen | Bestehende Position schließen, wenn das entgegengesetzte Signal erscheint. |
| Maximale Bestellungen / Maximaler Kauf / Maximaler Verkauf | Soft Caps, die die Anzahl gleichzeitiger Positionen begrenzen. |
| Stop-Loss / Take-Profit | Entfernung in Punkten für Schutzanordnungen. |
| Trailing Stop-Einstellungen | Parameter, die die Trailing-Stop-Aktivierung und -Distanz steuern. |
| Break Even-Einstellungen | Parameter, die die Break-Even-Aktivierung und den Sperrabstand steuern. |
| Grundvolumen | Volumen jedes Markteintritts. |

## Nutzung

1. Fügen Sie die Strategie Ihrem Strategiecontainer hinzu und legen Sie **Sicherheit** und **Portfolio** fest.
2. Konfigurieren Sie **Indikatorname** und **Indikatorparameter** so, dass sie mit dem benutzerdefinierten Zielindikator übereinstimmen.
3. Passen Sie die Risikoeinstellungen (Stop, Take, Trailing, Break Even) und das Basisauftragsvolumen an.
4. Führen Sie die Strategie aus. Es abonniert den gewählten Zeitrahmen, wertet die Indikatorpuffer aus und sendet Marktaufträge, wenn die Bedingungen erfüllt sind.

## Einschränkungen

* Der Indikator muss als Indikatortyp StockSharp verfügbar sein. Binäre MetaTrader-Indikatoren können nicht direkt geladen werden.
* Geldverwaltungsmodi, die von der freien Marge auf Brokerseite abhängen, werden auf ein festes Basisvolumen vereinfacht.
