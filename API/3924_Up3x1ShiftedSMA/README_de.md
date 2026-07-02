# Up3x1 Shifted SMA-Strategie (MT4-Konvertierung)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader 4 Fachberaters `up3x1.mq4` mit Sitz in `MQL/8097`.
- Implementiert den dreifachen einfachen gleitenden Durchschnitt-Crossover mit einer positiven Diagrammverschiebung genau wie im Originalskript.
- Verarbeitet nur abgeschlossene Kerzen, um den `Volume[0] > 1`-Schutz zu emulieren, der den Experten dazu zwang, einmal pro Balken zu bewerten.
- Zu den Risikomanagementfunktionen gehören Take-Profit, Stop-Loss, dynamische Lot-Reduzierung nach verlorenen Trades und ein optionaler Trailing-Stop.

## Handelslogik
1. **Indikatoren**
   - Drei einfache gleitende Durchschnitte mit einer Diagrammverschiebung von 6 Balken (standardmäßig schnell = 24, mittel = 60, langsam = 120).
2. **Langer Eintrag**
   - Vorheriger Balken: `SMAfast₍t-1₎ < SMAmedium₍t-1₎ < SMAslow₍t-1₎`.
   - Aktueller Balken: `SMAmedium₍t₎ < SMAfast₍t₎ < SMAslow₍t₎`.
   - Bedingung repliziert `ma1 < ma2 < ma3 && ma5 < ma4 < ma6` von MQL.
3. **Kurzer Eintrag**
   - Vorheriger Balken: `SMAfast₍t-1₎ > SMAmedium₍t-1₎ > SMAslow₍t-1₎`.
   - Aktueller Balken: `SMAmedium₍t₎ > SMAfast₍t₎ > SMAslow₍t₎`.
4. **Ausgangsregeln**
   - Take-Profit und Stop-Loss respektieren den konfigurierten Punktabstand multipliziert mit `Security.PriceStep` (oder werden direkt verwendet, wenn der Schritt unbekannt ist).
   - Der Trailing-Stop sperrt Gewinne, sobald der Preis um mehr als `TrailingStopPoints` ansteigt, und folgt dem nach dem Einstieg erreichten Extremwert.
   - Ausfallsicherer Ausstieg, wenn die gleitenden Durchschnitte in die entgegengesetzte Reihenfolge wechseln, was die ursprüngliche `OrderClose`-Logik widerspiegelt.

## Positionsgrößen
- Das Standardvolumen beträgt `BaseVolume` (0,1 Lot), wenn keine Portfoliokennzahlen verfügbar sind.
- Wenn `Portfolio.CurrentValue` vorhanden ist, multipliziert die Strategie es mit `RiskFraction` (Standard `0.00002`, äquivalent zur MQL-Formel `FreeMargin * 0.02 / 1000`).
- Nach mehr als einem verlorenen Ausgang wird die Lautstärke um `volume * losses / 3` reduziert, genau wie bei der `LotsOptimized`-Routine.
- Das Volumen wird auf `Security.VolumeStep` abgerundet und auf Null gesenkt, wenn es `Security.MinVolume` nicht erfüllen kann.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `FastPeriod` | 24 | Länge des am schnellsten verschobenen SMA. |
| `MediumPeriod` | 60 | Länge des um SMA verschobenen Mediums. |
| `SlowPeriod` | 120 | Länge des langsam verschobenen SMA. |
| `TakeProfitPoints` | 150 | Abstand in Preispunkten zwischen dem Einstiegspreis und dem Take-Profit. |
| `StopLossPoints` | 100 | Abstand in Preispunkten zwischen dem Einstiegspreis und dem Stop-Loss. |
| `TrailingStopPoints` | 100 | Optionaler Trailing-Stop-Abstand in Punkten (zum Deaktivieren auf 0 setzen). |
| `BaseVolume` | 0,1 | Fallback-Handelsgröße und Mindestvolumen nach Reduzierungen. |
| `RiskFraction` | 0,00002 | Bruchteil des Portfoliowerts, der zur Berechnung des dynamischen Volumens verwendet wird. |
| `CandleType` | Zeitrahmen 1 Stunde | Kerzenserie zur Versorgung von Indikatoren. |

## Konvertierungshinweise
- Die Strategie verwendet die übergeordnete API (`SubscribeCandles` + `Bind`) und vermeidet manuelle Verlaufspuffer.
- Indikatorwerte werden zwischen Aufrufen gespeichert, um den Parameter `shift` ohne direkten Indexzugriff nachzuahmen.
- Schutzexits werden mit Marktbefehlen auf dem erkannten Preisniveau ausgeführt, um mit der StockSharp-Abstraktion kompatibel zu bleiben.
- Alle Inline-Kommentare sind in englischer Sprache verfasst und entsprechen den Projektrichtlinien.

## Nutzung
1. Hängen Sie die Strategie in StockSharp Designer oder Code an ein Wertpapier und Portfolio an.
2. Wählen Sie eine Kerzenserie (`CandleType`) aus, die Ihrem MT4-Zeitrahmen (standardmäßig H1) entspricht.
3. Überprüfen Sie die punktbasierten Risikoparameter, um sie an die Tick-Größe des Instruments anzupassen (z. B. 0,0001 für die meisten Forex-Paare).
4. Setzen Sie `TrailingStopPoints` auf Null, wenn kein Trailing erforderlich ist.
5. Überwachen Sie Protokolle auf Meldungen wie „Enter long“ und „Exit short“, die die MQL-Diagnose widerspiegeln.

## Repository-Struktur
„
API/3924/
├── CS/Up3x1ShiftedSmaStrategy.cs # Konvertierte C#-Strategie mit englischen Kommentaren
├── README.md # Englische Dokumentation (diese Datei)
├── README_zh.md # Chinesische Übersetzung
└── README_ru.md # Russische Übersetzung
„

## Haftungsausschluss
Der Handel ist mit erheblichen Risiken verbunden. Die Strategie wird zu Bildungszwecken bereitgestellt und muss vor dem Live-Handel anhand historischer und simulierter Daten validiert werden.
