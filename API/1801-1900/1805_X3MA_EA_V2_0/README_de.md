# Strategie mit dreifachem gleitendem Durchschnitt-Crossover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis der Beziehung zwischen drei gleitenden Durchschnitten: schnell, mittel und langsam. Sie ist eine Konvertierung des MQL-Experten **X3MA_EA_V2_0**.

## Handelslogik

* **Einstieg**
  * Wenn *EnableEntryMediumSlowCross* wahr ist, wird eine Long-Position eröffnet, wenn der mittlere gleitende Durchschnitt den langsamen von unten kreuzt. Der umgekehrte Crossover löst einen Short-Einstieg aus.
  * Wenn die Option falsch ist, wartet die Strategie darauf, dass der schnelle Durchschnitt den mittleren kreuzt, während beide auf derselben Seite des langsamen bleiben. Long-Positionen erfordern `fast > medium > slow`, Short-Positionen erfordern `fast < medium < slow`.
* **Ausstieg**
  * Wenn *EnableExitFastSlowCross* wahr ist, werden offene Positionen geschlossen, wenn sich der schnelle und langsame Durchschnitt in die entgegengesetzte Richtung kreuzen.

Alle Signale werden an abgeschlossenen Kerzen ausgewertet.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `FastMaLength` | Periode des schnellen gleitenden Durchschnitts. |
| `MediumMaLength` | Periode des mittleren gleitenden Durchschnitts. |
| `SlowMaLength` | Periode des langsamen gleitenden Durchschnitts. |
| `EnableEntryMediumSlowCross` | Einstiege bei Mittel-/Langsam-Crossover erlauben. |
| `EnableExitFastSlowCross` | Positionen bei Schnell-/Langsam-Crossover schließen. |
| `CandleType` | Zeitrahmen der Kerzen. |

## Hinweise

Die Strategie verwendet die High-Level-API mit `SubscribeCandles` und `Bind`. Indikatorwerte werden über den `ProcessCandle`-Callback abgerufen, ohne `GetValue` zu verwenden. Schutzlogik wird mit `StartProtection()` in `OnStarted` aktiviert.
