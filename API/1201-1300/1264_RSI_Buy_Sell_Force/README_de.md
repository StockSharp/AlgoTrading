# RSI Kauf-/Verkaufsdruck-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie berechnet RSI auf den eingehenden Kerzen und glättet ihn mit einer EMA.
Sie leitet zwei Linien ab, `cc` und `bb`, die den Kauf- und Verkaufsdruck darstellen.
Eine Long-Position wird eröffnet, wenn `cc` über `bb` kreuzt, während eine Short-Position eröffnet wird, wenn `cc` unter `bb` kreuzt.
