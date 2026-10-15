# Schwellenwertstrategie für Warnsysteme
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Alerting System Threshold Strategy** ist ein StockSharp-Port des MetaTrader 5-Expertenberaters „AlertingSystem“ (MQL-Ordner `31843`). Das Original EA zeichnet zwei horizontale Linien und gibt einen Ton aus, wenn der Geldkurs über der oberen Linie oder der Briefkurs unter der unteren Linie liegt. Bei dieser C#-Konvertierung bleibt das Benachrichtigungsverhalten erhalten, während das übergeordnete API von StockSharp für den Datenzugriff und die Benachrichtigungsprotokollierung verwendet wird.

## Kernidee

* Hören Sie sich Echtzeit-Marktdaten der Stufe 1 an (bester Geld- und Briefkurs).
* Lösen Sie einmalige Benachrichtigungen aus, wenn das Gebot größer oder gleich einem konfigurierbaren oberen Schwellenwert ist.
* Lösen Sie einmalige Benachrichtigungen aus, wenn die Nachfrage kleiner oder gleich einem konfigurierbaren unteren Schwellenwert ist.
* Setzen Sie die Alarmflaggen zurück, wenn sich die Preise wieder innerhalb des Bandes bewegen, damit der nächste Ausbruch erkannt werden kann.

Im Gegensatz zur MQL-Implementierung, die bei jedem Tick wiederholt einen Ton abspielt, sendet die StockSharp-Version einen einzelnen informativen Protokolleintrag für jedes Breakout-Ereignis. Dies vermeidet eine Protokollüberflutung und benachrichtigt den Betreiber dennoch, wenn Preisziele erreicht werden.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `UpperPrice` | Gebotsniveau, das den bullischen Alarm aktiviert. Zum Deaktivieren auf `0` setzen. | `0` |
| `LowerPrice` | Briefkursniveau, das den bärischen Alarm aktiviert. Zum Deaktivieren auf `0` setzen. | `0` |

Bei beiden Parametern handelt es sich um Standardwerte `StrategyParam<decimal>`, die zur Laufzeit optimiert oder angepasst werden können. Sie können die Schwellenwerte während des Live-Handels verschieben, genauso wie Sie die horizontalen Linien in MetaTrader neu positionieren würden.

## Datenabonnements und Workflow

1. Wenn die Strategie startet, abonniert sie Level-1-Daten über `SubscribeLevel1().Bind(ProcessLevel1).Start()`.
2. Eingehende `Level1ChangeMessage`-Objekte aktualisieren die zwischengespeicherten besten Gebots- und besten Briefwerte.
3. Jedes Update ruft die Alarmprüfungen auf:
   * **Oberer Alarm** – wird einmal ausgelöst, wenn `BestBid >= UpperPrice` und der Preis zuvor unter dem Niveau lag.
   * **Unterer Alarm** – wird einmal ausgelöst, wenn `BestAsk <= LowerPrice` und der Preis zuvor über dem Niveau lag.
4. Alarme werden automatisch zurückgesetzt, wenn der Markt wieder innerhalb des Korridors handelt.

## Protokollierung und Benachrichtigungen

Benachrichtigungen werden mit `AddInfoLog` geschrieben und enthalten die aktuellen Gebots-/Briefwerte und die konfigurierten Ebenen. Integrieren Sie Ihre eigene Benachrichtigungspipeline (E-Mails, Messenger, benutzerdefinierte Sounds), indem Sie `OnInfo` überschreiben oder die Protokollereignisse der Strategie in Ihrer Hosting-Anwendung abonnieren.

## Nutzungstipps

* Legen Sie nur die Schwellenwerte fest, die Ihnen wichtig sind – die anderen können bei `0` bleiben, um deaktiviert zu bleiben.
* Kombinieren Sie die Strategie mit anderen Modulen, die auf `Info`-Protokolle reagieren, wenn Sie akustische oder Push-Benachrichtigungen reproduzieren möchten.
* Da die Strategie niemals Bestellungen aufgibt, besteht keine Notwendigkeit, `StartProtection()` anzurufen.

## Unterschiede zum Original EA

* Die StockSharp-Version verwendet Level-1-Daten, anstatt Diagrammobjekte zu erstellen.
* Um das Protokoll sauber zu halten, werden pro Ausbruch einmalig Warnungen ausgegeben.
* Alles andere (Parameter, logische Schwellenwerte, Bedingungen) entspricht der Referenz MQL.

## Dateien

* `CS/AlertingSystemStrategy.cs` – C#-Strategieimplementierung.
* `README.md` – Englische Dokumentation (diese Datei).
* `README_ru.md` – Russische Übersetzung mit zusätzlicher Erklärung.
* `README_zh.md` – Vereinfachte chinesische Übersetzung.
