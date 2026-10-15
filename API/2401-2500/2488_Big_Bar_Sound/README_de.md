# Strategie „Großer Balken mit Ton"
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Strategie „Großer Balken mit Ton"** repliziert das Verhalten des MetaTrader Expert Advisors „BigBarSound". Der Algorithmus beobachtet abgeschlossene Kerzen eines konfigurierbaren Zeitrahmens und meldet, wenn die Kerzenspanne breit genug ist, um als „großer Balken" zu gelten. Anstatt eine Audiodatei abzuspielen, schreibt er detaillierte Lognachrichten, die an jedes von StockSharp unterstützte Benachrichtigungssubsystem weitergeleitet werden können.

Die Strategie ist rein informativ – sie sendet keine Orders und verwaltet keine Positionen. Sie ist dafür ausgelegt, als Alarmierungskomponente innerhalb eines größeren automatisierten oder diskretionären Handelsworkflows verwendet zu werden.

## Verhalten
1. Die Strategie abonniert die durch den Parameter **Kerzentyp** angegebene Kerzenserie.
2. Für jede abgeschlossene Kerze misst sie die Balkengröße gemäß dem gewählten **Differenzmodus**:
   - **OpenClose** – absolute Differenz zwischen Schlusskurs und Eröffnungskurs.
   - **HighLow** – absolute Differenz zwischen Hoch und Tief des Balkens.
3. Der gemessene Wert wird gegen den **Punkte-Schwellenwert** multipliziert mit dem `PriceStep` des Instruments verglichen. Wenn die Balkengröße größer oder gleich diesem Schwellenwert ist, erfasst die Strategie einen Log-Eintrag, der das Abspielen der konfigurierten Sounddatei simuliert.
4. Wenn **Alarm anzeigen** aktiviert ist, wird eine zusätzliche Alarmlog-Meldung geschrieben, um das Ereignis hervorzuheben.

Da die Implementierung nur abgeschlossene Kerzen verarbeitet, kann jeder Balken höchstens einmal auslösen, was das Einzelschuss-Verhalten des ursprünglichen MQL Expert Advisors widerspiegelt.

## Parameter
- **Punkte-Schwellenwert (`BarPoint`)** – Anzahl der Preisschritte, die überschritten werden müssen, bevor ein Alarm ausgelöst wird. Der Standardwert von 200 entspricht dem ursprünglichen Skript. Optimierungsgrenzen (50–500 mit Schritt 50) werden zur Vereinfachung bereitgestellt.
- **Differenzmodus (`DifferenceMode`)** – wählt, wie die Kerzengröße gemessen wird: Öffnungs-/Schluss-Abstand oder vollständige Hoch-/Tief-Spanne.
- **Sound-Datei (`SoundFile`)** – Name der WAV-Datei, die abgespielt werden soll. Die Strategie protokolliert nur diesen Wert, um den MetaTrader `PlaySound`-Aufruf zu emulieren.
- **Alarm anzeigen (`ShowAlert`)** – wenn aktiviert, gibt die Strategie eine zusätzliche Lognachricht aus, um das optionale `Alert`-Popup der MQL-Version nachzuahmen.
- **Kerzentyp (`CandleType`)** – Kerzendatentyp (Zeitrahmen), den es zu abonnieren gilt. Standardmäßig verwendet die Strategie 1-Minuten-Kerzen.

## Alarme und Protokollierung
Die Strategie verwendet `LogInfo`, um anzukündigen, dass die Sounddatei abgespielt worden wäre, und `AddInfoLog`, um eine separate Alarmmeldung bereitzustellen. Diese Einträge enthalten die Instrumentenkennung, den Kerzen-Zeitstempel und die gemessene Balkengröße, was die Integration mit den Logging-Viewern oder Benachrichtigungssenken von StockSharp erleichtert.

Wenn der Broker keinen gültigen `PriceStep` liefert, wird ein Fallback-Wert von `1` verwendet, damit die Strategie betriebsbereit bleibt. Passen Sie den **Punkte-Schwellenwert** entsprechend an, um die tatsächliche Tick-Größe des Instruments widerzuspiegeln.

## Verwendungshinweise
- Fügen Sie die Strategie an jedes Instrument an, das Kerzendaten bereitstellt. Der Alarm funktioniert gleich gut bei Forex, Futures, Aktien oder Krypto-Assets.
- Kombinieren Sie sie mit anderen Handelsstrategien, indem Sie ihre Log-Ausgabe abonnieren oder die Klasse erweitern, um Ereignisse an benutzerdefinierte Handler weiterzuleiten.
- Da die Implementierung keine Orders generiert, werden `Volume` und positionsbezogene Parameter ignoriert.
- Um hörbare Benachrichtigungen zu erzeugen, verbinden Sie das Logging-Subsystem von StockSharp mit einem Sound-Notifier oder erweitern Sie den Code, um plattformspezifische Audio-APIs aufzurufen.

## Unterschiede zum ursprünglichen MQL Expert Advisor
- Das ursprüngliche Skript arbeitete mit Tick-Daten und verfolgte Balkenänderungen manuell. Der StockSharp-Port verarbeitet abgeschlossene Kerzen direkt, was genau einen Alarm pro Balken ohne separate Trigger-Flag garantiert.
- Audio-Wiedergabe wird durch Lognachrichten ersetzt, damit das Verhalten innerhalb der StockSharp-Umgebung plattformübergreifend bleibt.
- Parameternamen folgen StockSharp-Konventionen, behalten aber dieselbe Semantik: Schwellengröße in Punkten, Messmodus, optionaler Alarm und Sound-Name.

## Anforderungen
Es sind keine zusätzlichen Indikatoren erforderlich. Stellen Sie einfach sicher, dass der ausgewählte `CandleType` von der verbundenen Datenquelle unterstützt wird, damit die Strategie abgeschlossene Kerzen zur Verarbeitung erhält.
