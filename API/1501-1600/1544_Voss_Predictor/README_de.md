# Voss Predictor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert John Ehlers' prädiktiven Voss-Filter mit einem Bandpassfilter, um Preisbewegungen vorauszusehen. Eine Long-Position wird eröffnet, wenn der prädiktive Filter über den Bandpassausgang steigt, während eine Short-Position eröffnet wird, wenn er darunter fällt.

## Details

- **Einstieg**: Der prädiktive Voss-Filter kreuzt den Bandpassfilter nach oben.
- **Ausstieg**: Der prädiktive Voss-Filter kreuzt den Bandpassfilter nach unten.
- **Typ**: Trendfolge.
- **Stops**: Keine.
