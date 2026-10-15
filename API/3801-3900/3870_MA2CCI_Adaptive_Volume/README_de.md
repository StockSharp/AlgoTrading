# MA2CCI Adaptive Volume-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die MA2CCI-Strategie ist eine direkte Portierung des Expert Advisors MetaTrader 4, der ursprünglich als „MA2CCI.mq4“ vertrieben wurde. Das System kombiniert einen schnellen/langsamen einfachen gleitenden Durchschnitt (SMA) mit einer Bestätigung der Nulllinie des Commodity Channel Index (CCI). Jeder validierte Crossover eröffnet eine einzelne Marktposition und platziert sofort einen auf dem Average True Range (ATR) basierenden Schutzstopp. Die Positionsgrößenbestimmung folgt der ursprünglichen Money-Management-Logik, indem die Ordergröße im Verhältnis zum Eigenkapital skaliert und nach Phasen von Verlusten bei Trades reduziert wird.

## Indikatoren und Daten
- **Schnell SMA (FMa)** und **Langsam SMA (SMa)** im konfigurierten Zeitrahmen, um Trendumkehrungen zu erkennen.
- **Commodity Channel Index (CCI)** mit demselben Preisstrom zur Bestätigung der Momentumrichtung durch Nullliniendurchgänge.
- **Average True Range (ATR)** zur Messung der jüngsten Volatilität und zur Ableitung der Stop-Loss-Distanz.
- **Kerzen** des gewählten Zeitrahmens (Standard 15 Minuten) liefern die Eingabereihen für alle Indikatoren.

## Handelsregeln
- **Langer Einstieg**: Der schnelle SMA kreuzt den langsamen SMA, während CCI auf demselben Balken von negativ nach positiv kreuzt, keine Position offen ist und Handel erlaubt ist. Eine Marktkauforder wird gesendet und ein Stop-Loss wird bei `close − ATR × AtrMultiplier` aktiviert.
- **Kurzer Einstieg**: Der schnelle SMA kreuzt den langsamen SMA, während CCI von positiv nach negativ kreuzt, es ist keine Position offen. Eine Market-Sell-Order wird mit einem Stop-Loss bei `close + ATR × AtrMultiplier` platziert.
- **Ausstieg bei Long-Positionen**: Wenn der schnelle SMA den langsamen SMA wieder unterschreitet, wird die gesamte Long-Position zum Marktwert geschlossen. Auch der Schutzstopp wird aufgehoben.
- **Ausstieg bei Shorts**: Wenn der schnelle SMA den langsamen SMA wieder überschreitet, wird die Short-Position zum Marktwert gedeckt und der Stop aufgehoben.
- **Stop-Loss**: Jede neue Position stellt einen Volatilitätsstopp wieder her, der die MetaTrader-Logik widerspiegelt. Stopps werden nur bei neuen Einträgen neu berechnet und als separate bedingte Orders gespeichert.

## Positionsgrößen
- Die Basislosgröße beginnt beim Parameter `BaseVolume` (Standard: 0,1 Los).
- Wenn `RiskFraction` positiv ist, berechnet die Strategie eine zusätzliche Größe mit `equity × RiskFraction / 1000`, ahmt die ursprüngliche `AccountFreeMargin`-Formel nach und verwendet das Maximum zwischen beiden Werten.
- Nach zwei oder mehr aufeinanderfolgenden Verlustgeschäften wird die Losgröße um `volume × losses / DecreaseFactor` reduziert, wodurch die Drawdown-Kontrolle von `DcF` repliziert wird.
- Die Lautstärken werden auf den `VolumeStep` des Instruments normalisiert.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `FastMaPeriod` | 4 | Kurzer Lookback-Zeitraum von SMA. |
| `SlowMaPeriod` | 8 | Langsamer Lookback-Zeitraum von SMA. |
| `CciPeriod` | 4 | Zeitraum des Commodity Channel Index. |
| `AtrPeriod` | 4 | Durchschnittliche True-Range-Periode, die für die Stoppdistanz verwendet wird. |
| `AtrMultiplier` | 1,0 | Der Multiplikator wird vor der Platzierung des Stop-Loss auf ATR angewendet. |
| `BaseVolume` | 0,1 | Mindesthandelsgröße vor Risikoanpassungen. |
| `RiskFraction` | 0,02 | Anteil des pro Trade riskierten Eigenkapitals (pro 1000 Währungseinheiten). |
| `DecreaseFactor` | 3 | Divisor, der steuert, wie schnell die Größe nach Verlusten schrumpft. |
| `CandleType` | 15-Minuten-Kerzen | Für Indikatoren und Signale verwendeter Zeitrahmen. |

## Notizen
- E-Mail-Benachrichtigungen des ursprünglichen Fachberaters (`SndMl`) werden bewusst weggelassen.
- Es kann jeweils nur eine Position offen sein, entsprechend dem MetaTrader-Verhalten des Quellcodes.
- Schutzstopps werden immer dann neu erstellt, wenn die Position umkippt oder geschlossen wird, um zu verhindern, dass verwaiste Aufträge im Buch verbleiben.
