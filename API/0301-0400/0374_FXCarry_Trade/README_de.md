# FX Carry Trade

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Monatliche Carry = beobachteter jährlicher BaseRate − QuoteRate. Die TopK höchsten Differenzen kaufen und TopK niedrigsten leerverkaufen; mindestens 2×TopK geeignete Paare verhindern Überschneidungen. Jede Seite erhält die Hälfte des USD-Bruttobudgets zu gleichen Teilen. Mit dem ausdrücklich beobachteten UsdPerUnit in native Paareinheiten umrechnen, auch wenn die Kurswährung nicht USD ist. Ausgeschiedene Titel schließen und verbleibende Bestände beim nächsten Termin abgleichen.

Nur abgeschlossene Kerzen mit gleichem Zeitstempel werden gemeinsam ausgewertet. Statistiken zählen verschiedene UTC-Daten; weitere Intraday-Kerzen ersetzen den Tageswert. Die erste geeignete gemeinsame Kerze jedes Monats löst genau eine Umschichtung aus. Unvollständige Daten verschieben sie. Gleiche Werte werden nach Security.Id sortiert.

Alle Beobachtungen müssen bis zum Schluss der Signalkerze veröffentlicht sein. Intraday zählen nur Tageskurse vor dem aktuellen UTC-Datum; eine abgeschlossene Tageskerze zählt ihr eigenes Datum. Zukünftige CSV-Daten dürfen keine Entscheidung vorwärmen.

Die Veröffentlichungsgrenze ist die späteste tatsächliche CloseTime der synchronisierten abgeschlossenen Kerzen. Eine Handelssitzung kann vor Ablauf des eingestellten Kerzenintervalls enden.

## Eingabedaten

RatesFile ist eine erforderliche CSV: `SecurityId,PublishedAt,BaseRate,QuoteRate,UsdPerUnit`. PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`; beide Zinssätze in gleichen jährlichen Dezimaleinheiten. UsdPerUnit ist der positive USD-Wert einer nativen Handelseinheit. Nur der jüngste veröffentlichte Stand bis MaxRateAgeDays ist gültig. Doppelte Titel/Publikationszeit-Paare werden abgelehnt. Zinssätze werden nicht aus Kerzen erzeugt.

## Parameter

| Parameter | Standard |
|---|---|
| `Universe` | erforderlich |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `TopK` | `3` |
| `RatesFile` | erforderlich |
| `MaxRateAgeDays` | `45` |

## Ausführung

Capital ist ein festes USD-Budget; Leverage steuert die Bruttoexposition. RatesFile.UsdPerUnit für Mengen und MinTradeUsd verwenden. Zu null auf VolumeStep runden, tatsächliche Ausführungen abgleichen und offene Orders abwarten. MinVolume/MaxVolume beachten und große Anpassungen auf spätere Kerzen verteilen. Vollständige Schließung umgeht MinTradeUsd. Keine Stop-Loss- oder Gewinnmitnahmeorders.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Ausführungsdaten sowie expliziten Zins-, Finanz-, Bewertungs-, Text- und Aufwärmfixtures geprüft. Kalenderfixtures verschieben Archiv-OHLC/Volumina und setzen Ausführungskurse; dies ist keine mehrjährige Markthistorie. Unabhängige Formeln, Orderrichtung/-menge, echte Ausführungen, Restpositionen und gesperrte Termine werden geprüft. Zwei Gruppen, K=1 und AssetDecile=1 sind deklarierte Mechanikprofile, kein Nachweis für ein vollständiges Handelsuniversum. Keine Renditezusage.
