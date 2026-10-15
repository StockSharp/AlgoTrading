# Strategie zum Schließen bei MA-Kreuzung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie überwacht einen einfachen gleitenden Durchschnitt (MA) und schließt automatisch alle offenen Positionen, wenn der Kerzenschluss die MA-Linie kreuzt. Sie ist für Trader konzipiert, die Einstiege manuell oder mit anderen Systemen verwalten, aber einen automatischen Ausstieg wünschen, wenn sich der Trend umkehrt.

Die Logik verfolgt die Beziehung zwischen dem Schlusskurs und dem MA. Wenn eine neue abgeschlossene Kerze von einer Seite des MA zur anderen wechselt, sendet die Strategie eine Marktorder, um die Position zu schließen. Keine neuen Positionen werden eröffnet.

## Details

- **Einstiegskriterien**: Keine. Positionen müssen extern eröffnet werden.
- **Ausstiegskriterien**:
  - **Long**: Vorheriger Schluss über MA und aktueller Schluss unter MA löst einen Verkauf zum Schließen aus.
  - **Short**: Vorheriger Schluss unter MA und aktueller Schluss über MA löst einen Kauf zum Schließen aus.
- **Long/Short**: Beide Richtungen werden unterstützt.
- **Stops**: Nicht verwendet. Der MA-Crossover dient als Ausstiegssignal.
- **Standardwerte**:
  - `MA Period` = 50.
  - `Candle Type` = Zeitrahmen von 1 Minute.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat

