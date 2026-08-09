# Aftershock Playbook-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Aftershock Playbook**-Strategie wertet eine ungewöhnlich große Preisbewegung innerhalb einer einzelnen Kerze als Näherung für eine Gewinnüberraschung und folgt dem anschließenden Drift. Sie verwendet nur Marktkerzen und benötigt keinen externen Ergebnisdaten-Feed.

- **Signal**: Bei jeder abgeschlossenen `CandleType`-Kerze wird die Veränderung zwischen zwei Schlusskursen mit dem über `AtrLength` berechneten ATR verglichen.
- **Einstieg oder Umkehr**: Ein Anstieg über `ATR × SurpriseThreshold` eröffnet eine Long-Position oder dreht auf Long; ein entsprechender Rückgang eröffnet eine Short-Position oder dreht auf Short.
- **Ausstieg**: Eine ungünstige Bewegung über `ATR × AtrMultiplier` schließt die aktuelle Position. Erreicht die Bewegung zugleich die Einstiegsschwelle, hat die Positionsumkehr Vorrang.
- **Abkühlphase**: Nach Einstieg, Umkehr oder Ausstieg werden für `CooldownBars` abgeschlossene Kerzen alle Signale übersprungen.
