# Multik SMA Exp-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie implementiert einen konträren Ansatz basierend auf der Steigung eines einfachen gleitenden Durchschnitts (SMA). Sie wurde vom MetaTrader 5-Expertenberater "Multik_SMA_Exp" portiert.

Die Strategie überwacht die letzten drei SMA-Werte. Wenn der SMA in den beiden zuletzt abgeschlossenen Segmenten gefallen ist, eröffnet die Strategie eine Long-Position. Wenn der SMA in den beiden Segmenten gestiegen ist, öffnet sie eine Short-Position. Positionen werden geschlossen, wenn sich die Steigung des SMA umkehrt.

## Parameter
- **MA Period** – Länge des einfachen gleitenden Durchschnitts. Standard: 50.
- **Candle Type** – Art der für Berechnungen verwendeten Kerzen. Standard: 1-Minuten-Zeitrahmen.

## Handelsregeln
1. Bei jeder abgeschlossenen Kerze den SMA berechnen.
2. Steigungen bestimmen:
   - `dsma1 = SMA[n-1] - SMA[n-2]`
   - `dsma2 = SMA[n-2] - SMA[n-3]`
3. Einstieg:
   - Wenn `dsma1 < 0` und `dsma2 < 0` und keine Long-Position vorhanden, kaufen.
   - Wenn `dsma1 > 0` und `dsma2 > 0` und keine Short-Position vorhanden, verkaufen.
4. Ausstieg:
   - Wenn eine Long-Position gehalten wird und `dsma1 > 0`, Long schließen.
   - Wenn eine Short-Position gehalten wird und `dsma1 < 0`, Short schließen.

Das Volumen neuer Orders verwendet das `Volume` der Strategie plus den Absolutwert der aktuellen Position, um bei Bedarf vollständig umzukehren.
