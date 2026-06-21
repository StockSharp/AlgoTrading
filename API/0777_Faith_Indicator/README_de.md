# Faith Indicator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie bewertet das Markt-"Vertrauen", indem sie die Volumenausweitung misst, wenn der Preis höhere Hochs oder niedrigere Tiefs bildet. Eine positive Bewertung deutet auf eine Dominanz der Käufer hin, während eine negative Bewertung auf Verkäuferübergewicht schließen lässt. Die Strategie handelt bei Übergängen zwischen positiven und negativen Bewertungen.

## Details

- **Einstiegskriterien:** Faith-Bewertung kreuzt über Null → kaufen; kreuzt unter Null → verkaufen.
- **Long/Short:** beide.
- **Ausstiegskriterien:** gegensätzliches Signal.
- **Indikatoren:** Highest, SMA.
