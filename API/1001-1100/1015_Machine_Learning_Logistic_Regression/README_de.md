# Machine Learning Logistische Regression Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie trainiert bei jedem Bar ein einfaches logistisches Regressionsmodell neu.
Das Modell verwendet aktuelle Schlusskurse und eine daraus abgeleitete synthetische Reihe.
Wenn die vorhergesagte Wachstumswahrscheinlichkeit über 0.5 liegt, eröffnet die Strategie eine Long-Position; andernfalls geht sie Short.
Positionen werden für eine feste Anzahl von Bars gehalten.

## Details
- **Einstieg**: Vorhersage > 0.5 → Long, sonst Short.
- **Ausstieg**: Gegensätzliches Signal oder Halteperiode erreicht.
- **Long/Short**: Beide.
- **Zeitrahmen**: Konfigurierbar, Standard 1 Minute.
