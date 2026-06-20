# Estratégia de Zonas de Trading AO AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o conceito "AO/AC Trading Zones". Combina o Awesome Oscillator (AO), Acceleration/Deceleration (AC) e os fractais de Bill Williams para construir uma pirâmide de posições compradas quando o momentum acelera acima da linha dos dentes do Alligator.

## Detalhes

- **Entrada**: Pelo menos duas barras consecutivas com `close > teeth`, `AO > AO[1]`, `AC > AC[1]` e `close > EMA`.
- **Piramidação**: Adiciona até cinco posições compradas enquanto as condições forem válidas.
- **Saída**: Reversão de tendência por fractais ou preço caindo abaixo do nível de stop.
- **Indicadores**: SMMA (dentes), AO, AC, EMA.
- **Stop**: Mínima da quinta barra verde.
