# Bärische Docht-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kauft, wenn eine bärische Kerze einen langen unteren Docht bildet, der eine benutzerdefinierte Prozentschwelle überschreitet. Ein optionaler EMA-Filter verlangt, dass der Schlusskurs über einem gleitenden Durchschnitt liegt, um die Trendrichtung zu bestätigen. Positionen werden geschlossen, wenn der Preis über dem Hoch der vorherigen Kerze schließt.

## Details

- **Einstiegskriterien:** Bärische Kerze mit unterem Docht <= Schwellenwert und innerhalb des Handelsfensters; optional Preis über EMA.
- **Long/Short:** Nur Long.
- **Ausstiegskriterien:** Schlusskurs > vorheriges Hoch.
- **Stops:** Keine.
- **Standardwerte:**
  - Schwellenwert = -1 (%)
  - EMA-Filter deaktiviert, EMA-Periode = 200
  - Startzeit = 2014-01-01, Endzeit = 2099-01-01
  - Kerzen-Zeitrahmen = 1 Minute
- **Filter:**
  - Kategorie: Umkehr
  - Richtung: Long
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
