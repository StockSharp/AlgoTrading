# Signalzählung mit Array-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie reproduziert die Logik des MetaTrader 4 Expert Advisors `Signal-COunt-with array.mq4`.
Sie überwacht Donchian-Kanal-Extreme für eine konfigurierbare Menge von Preisverschiebungen und zählt, wie oft
die Indikatorausgabe sich ändert, leer wird oder zu einem Signalwert zurückkehrt. Die Implementierung behält
den Diagnosefokus des ursprünglichen Skripts bei: Es werden keine Trades ausgeführt. Stattdessen druckt die Strategie
detaillierte Statistiken, wenn ein neues Hoch/Tief registriert wird oder wenn das Kerzen-Logging aktiviert ist.

## Konzept

- Die ursprüngliche `iCustom`-Suche von `super_signals_v2_alert` durch einen Donchian-Kanal ersetzen, der
  das höchste Hoch und das niedrigste Tief über `ChannelPeriod` Kerzen bereitstellt.
- Ein Raster von Verschiebungen (`GapStart`, `GapStep`, `GapCount`) auswerten, das die mehrfachen Indikatorkonfigurationen emuliert,
  die vom MQL-Skript getestet wurden.
- Für jede Verschiebung sechs Zähler verfolgen, die die ursprünglichen Arrays widerspiegeln, einschließlich Übergängen in und
  aus dem Sentinel-Wert (`2147483647` für leere obere Lesungen und `-2147483646` für leere untere Lesungen).
- Eine Texttabelle mit den akkumulierten Zählern ausgeben, damit der Benutzer prüfen kann, wie oft jeder Buffer
  ein neues Signal erzeugt, zu leer zurückkehrt oder den Standard-Nullzustand verlässt.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | 5-Minuten-Zeitrahmen | Kerzenserie für die Donchian-Berechnungen. |
| `ChannelPeriod` | 24 | Anzahl der Kerzen zur Bestimmung der Donchian-Hochs und -Tiefs. |
| `GapStart` | 0 | Erste Verschiebung (in Vielfachen des Kursschritts), die auf die virtuellen Signalwerte angewendet wird. |
| `GapStep` | 1 | Schrittgröße (in Kursschritten) zwischen aufeinanderfolgenden Verschiebungen. |
| `GapCount` | 8 | Anzahl der auszuwertenden Verschiebungen (entspricht der ursprünglichen 0..7-Schleife). |
| `LogOnEachCandle` | false | Wenn aktiviert, erzwingt Logging nach jeder abgeschlossenen Kerze. |

## Zähler

Jede Verschiebung hält zwei Zeilen: Index `0` repräsentiert den oberen Donchian-Buffer (bullisches Signal) und
Index `1` repräsentiert den unteren Buffer (bärisches Signal). Folgende Statistiken werden gesammelt:

- **Changed** – wird inkrementiert, wenn sich der Rohindikatorwert von der vorherigen Beobachtung unterscheidet.
- **Empty** – zählt, wie oft der Buffer den positiven Sentinel (`2147483647`) zurückgegeben hat.
- **NegEmpty** – zählt Vorkommen des negativen Sentinels (`-2147483646`), hauptsächlich für den unteren Buffer.
- **Zero** – verfolgt Übergänge vom Standard-Nullzustand zu einem Nicht-Null-Wert.
- **NewFromEmpty** – wird inkrementiert, wenn ein echtes preisbasiertes Signal den Sentinel-Wert ersetzt.
- **BackToEmpty** – wird inkrementiert, wenn der Buffer nach einem Nicht-Sentinel-Wert zu seinem Sentinel zurückkehrt.

Diese Zähler entsprechen eins zu eins den Arrays, die im ursprünglichen Expert Advisor gehalten werden
(`GetInd_iCustom_changed`, `GetInd_iCustom_maxInt`, `GetInd_iCustom_minInt`, etc.).

## Logging

Die Strategie druckt Diagnostik über `AddInfoLog` in zwei Situationen:

1. Wann immer das obere Donchian-Band steigt oder das untere Band fällt und ein neues Extrem anzeigt.
2. Bei jeder abgeschlossenen Kerze, wenn `LogOnEachCandle` auf `true` gesetzt ist.

Jeder Logeintrag beginnt mit der Kerzenzeit und listet dann die Zähler für jede Verschiebung auf, was es einfach macht,
das Verhalten verschiedener virtueller Indikatorkonfigurationen zu vergleichen.

## Verwendungshinweise

- Die Strategie an ein beliebiges Wertpapier anhängen; sie basiert nur auf historischen Kerzen und gibt keine Orders auf.
- `ChannelPeriod` an die Volatilität des untersuchten Instruments anpassen. Eine längere Periode
  imitiert eine breitere Swing-Erkennung ähnlich dem MT4-Indikator.
- `GapCount` erhöhen, wenn mehr Verschiebungen beobachtet werden müssen. Die Arrays werden beim Start automatisch angepasst.
- Die Diagnostik mit Chart-Zeichnungen (Kerzen plus Donchian-Kanal) kombinieren, um die
  gedruckten Statistiken visuell mit der Marktstruktur auszurichten.
