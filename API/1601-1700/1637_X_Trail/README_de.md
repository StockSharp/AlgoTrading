# X-Trail-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erzeugt Trades, wenn ein schneller und ein langsamer einfacher gleitender
Durchschnitt, berechnet auf dem Median-Preis, sich kreuzen. Die Logik spiegelt das
ursprüngliche MQL-Skript **X_trail.mq4** wider, das bei solchen Kreuzungen Alarme auslöste.

Eine Long-Position wird eröffnet, wenn der schnelle MA auf der aktuellen und der vorherigen
Kerze über dem langsamen MA liegt, während er zwei Kerzen zuvor darunter lag. Das
umgekehrte Muster löst eine Short-Position aus. Positionen werden bei jedem neuen Signal umgekehrt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller MA > langsamer MA auf den letzten zwei abgeschlossenen Kerzen und schneller MA lag zwei Kerzen zuvor unter dem langsamen MA.
  - **Short**: Schneller MA < langsamer MA auf den letzten zwei abgeschlossenen Kerzen und schneller MA lag zwei Kerzen zuvor über dem langsamen MA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gegenläufige Kreuzung (Positionsumkehr).
- **Stops**: Keine.
- **Indikatoren**:
  - Zwei einfache gleitende Durchschnitte, berechnet auf dem Median-Preis.
