# Three EMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie reproduziert den MetaTrader-Expertenberater "ThreeEMA" durch das Stapeln von drei exponentiellen gleitenden Durchschnitten (EMAs). Sie sucht nach direktionaler Ausrichtung zwischen einem schnellen, mittleren und langsamen EMA im selben Zeitrahmen. Wenn die Durchschnitte streng aufsteigend geordnet sind (schnell über mittel über langsam), öffnet oder hält die Strategie eine Long-Position. Wenn die Reihenfolge umkippt (schnell unter mittel unter langsam), öffnet oder hält sie eine Short-Position. Schützende Stop-Loss- und Take-Profit-Abstände spiegeln die ursprünglichen MQL-Parameter wider und werden in Preispunkten relativ zur Tick-Größe des Instruments ausgedrückt.

## Ursprüngliches MQL-Verhalten
Die MQL-Version instanziierte drei EMA-Indikatoren (`FastPeriod`, `MediumPeriod`, `SlowPeriod`) und erzeugte Handelssignale basierend auf ihrer relativen Reihenfolge auf dem zuletzt geschlossenen Balken:

- **Long eröffnen / Short schließen** wenn `FastEMA > MediumEMA > SlowEMA`.
- **Short eröffnen / Long schließen** wenn `FastEMA < MediumEMA < SlowEMA`.
- Stop-Loss und Take-Profit wurden als feste Abstände in Punkten vom Einstiegspreis angewendet.

Orders wurden mit Marktausführung eingereicht und der Money-Management-Block verwendete eine feste Lot-Größe. Das Trailing-Modul war deaktiviert.

## StockSharp-Implementierungsdetails
- Verwendet die High-Level-Kerzenabonnement-API. Drei `ExponentialMovingAverage`-Indikatoren sind an das Hauptzeitrahmen-Abonnement gebunden, sodass jede fertige Kerze alle EMA-Werte gleichzeitig liefert.
- Handelsentscheidungen werden nur auf vollständig geformten Kerzen bewertet, um Intrabar-Rauschen zu vermeiden.
- Wenn ein direktionaler Stack erscheint, storniert die Strategie alle laufenden Orders, schließt das entgegengesetzte Exposure falls nötig und öffnet eine neue Marktposition in der erforderlichen Richtung.
- `StartProtection` konvertiert die konfigurierten punktbasierten Stop-Loss- und Take-Profit-Abstände in tatsächliche Preisoffsets unter Verwendung des `PriceStep` des Instruments. Dies spiegelt das Schutzverhalten des ursprünglichen EA wider.
- Die Chart-Integration zeichnet Kerzen und alle drei EMAs, wenn ein Chartbereich verfügbar ist, was die visuelle Validierung von Signalen erleichtert.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `CandleType` | 1-Minuten-Zeitrahmen | Zeitrahmen des Kerzenabonnements für EMAs. |
| `FastPeriod` | 5 | Länge des schnellen EMA. Muss kleiner als `MediumPeriod` sein. |
| `MediumPeriod` | 12 | Länge des mittleren EMA. Muss zwischen dem schnellen und langsamen Zeitraum liegen. |
| `SlowPeriod` | 24 | Länge des langsamen EMA. Muss der höchste Periodenwert sein. |
| `StopLossPoints` | 400 | Schützender Stop-Loss-Abstand in Instrumentenpunkten (in Preis umgerechnet mit `PriceStep`). Null zum Deaktivieren. |
| `TakeProfitPoints` | 900 | Take-Profit-Abstand in Instrumentenpunkten (in Preis umgerechnet mit `PriceStep`). Null zum Deaktivieren. |

## Verwendungshinweise
1. Konfigurieren Sie `Volume` vor dem Starten der Strategie, um die gewünschte Order-Größe widerzuspiegeln (das originale EA verwendete feste Lots).
2. Stellen Sie sicher, dass die EMA-Perioden streng steigend bleiben; andernfalls wird während `OnStarted` eine Ausnahme ausgelöst, die der Validierung im MQL-Quellcode entspricht.
3. Da die Logik Positionen immer umkehrt, wenn der EMA-Stack sich umkehrt, ist die Strategie kontinuierlich dem Markt ausgesetzt, wenn die Bedingungen zwischen bullischen und bearischen Ausrichtungen wechseln.
