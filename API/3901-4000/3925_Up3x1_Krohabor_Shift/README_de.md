# Up3x1 Krohabor Shift-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie **up3x1 Krohabor D** ist eine Umsetzung des MetaTrader 4 Expert Advisors `up3x1_Krohabor_D.mq4`. Es bleibt bei der ursprünglichen Idee, drei verschobene einfache gleitende Durchschnitte (SMA) auszurichten, um Trendfortsetzungsausbrüche im aktiven Zeitrahmen zu erkennen. Die C#-Implementierung verwendet das High-Level-StockSharp API mit Kerzenabonnements und Indikatorbindungen und passt gleichzeitig das Risiko- und Positionsmanagement an die .NET-Umgebung an.

## Handelslogik
- Drei SMAs werden auf der Grundlage der Schlusskurse des Instruments berechnet:
  - Schnell SMA (Standard 24 Balken)
  - Mittel SMA (Standard 60 Balken)
  - Langsam SMA (Standard 120 Balken)
- Jeder gleitende Durchschnitt wird um eine konfigurierbare Anzahl abgeschlossener Kerzen nach vorne verschoben (Standard 6). Die Strategie vergleicht für jeden Durchschnitt den aktuellen verschobenen Wert und den Wert der vorherigen Kerze.
- **Lange Einreise**-Anforderungen:
  - Sowohl die aktuellen als auch die vorherigen langsamen SMA-Werte bleiben unter den aktuellen und vorherigen schnellen/mittleren SMA-Werten, was auf eine bullische Trennung hinweist.
  - Das Medium SMA fällt relativ zum Fasten SMA (vorheriges Medium über vorherigem Fasten, aktuelles Medium unter aktuellem Fasten).
- **Kurzer Eintrag** spiegelt die lange Logik wider, wobei alle Vergleiche umgekehrt sind.
- Es kann jeweils nur eine Position offen sein. Wenn keine Position aktiv ist, wartet die Strategie auf ein neues Einstiegssignal; andernfalls verwaltet es Exits.

## Ausgangsregeln und Schutz
- Anfängliche Schutzbefehle werden durch die Überwachung von Kerzenhochs und -tiefs simuliert:
  - Die Stop-Loss-Distanz wird in Preisschritten ausgedrückt (Standard 110 Punkte) und angewendet, sobald eine Position eröffnet wird.
  - Die Take-Profit-Distanz verwendet dieselbe Darstellung (Standard 5 Punkte).
- Ein Trailing Stop (Standard 10 Punkte) wird aktiviert, sobald der nicht realisierte Gewinn den konfigurierten Schwellenwert überschreitet. Der Stop folgt dem Markt zugunsten der offenen Position, ohne sich jedoch zurückzuziehen.
- Umkehrausgänge mit gleitendem Durchschnitt schließen den Handel, wenn der schnelle SMA durch die mittleren und langsamen Durchschnitte zurückkreuzt, wodurch die Abschlusslogik des ursprünglichen EA nachgeahmt wird.
- Die dynamische Volumenreduzierung nach aufeinanderfolgenden Verlusten reproduziert das Verhalten des MT4-Skripts: Die Handelsgröße verringert sich proportional zur Anzahl der Verlustgeschäfte, wobei eine Mindestvolumenuntergrenze eingehalten wird.

## Parameter
| Name | Beschreibung |
|------|-------------|
| `FastPeriod` | Zeitraum des Fastens SMA. |
| `MediumPeriod` | Zeitraum des Mediums SMA. |
| `SlowPeriod` | Zeitraum der langsamen SMA. |
| `MaShift` | Anzahl der abgeschlossenen Kerzen, die zum Vorwärtsverschieben aller gleitenden Durchschnitte verwendet werden. |
| `Volume` | Grundauftragsvolumen für Neuzugänge. |
| `MinVolume` | Minimal zulässiges Volumen nach verlustbasierten Anpassungen. |
| `LossReductionFactor` | Der Divisor wird angewendet, wenn das Volumen nach aufeinanderfolgenden Verlustgeschäften schrumpft. |
| `StopLossPoints` | Stop-Loss-Distanz gemessen in Preisschritten. |
| `TakeProfitPoints` | Take-Profit-Distanz gemessen in Preisschritten. |
| `TrailingPoints` | Trailing-Stop-Distanz und Aktivierungsschwelle in Preisschritten. |
| `CandleType` | Für die Analyse verwendeter Kerzendatentyp (Zeitrahmen). |

## Notizen
- Die Strategie verwendet `SubscribeCandles` zusammen mit `Bind`, um Indikatorausgaben zu streamen und so den manuellen Abruf von Indikatorwerten zu vermeiden.
- Stop-Loss-, Take-Profit- und Trailing-Verhalten werden innerhalb der Strategieschleife implementiert, um Maklerunabhängig zu bleiben. In Live-Handelsumgebungen können Sie diese Blöcke bei Bedarf durch tatsächliche Schutzaufträge ersetzen.
- Alle Kommentare im Quellcode sind in englischer Sprache verfasst, um den Projektrichtlinien zu entsprechen.
- Es werden keine automatisierten Tests bereitgestellt. Verwenden Sie Backtesting in StockSharp, um Parametersätze für Ihre Instrumente zu validieren.
