# Estratégia Heiken Ashi Sem Pavio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera contra candles Heiken Ashi que aparecem sem pavios. Um candle Heiken Ashi de alta cujo corpo é maior que o anterior e não possui sombra inferior aciona uma entrada a descoberto. Um candle de baixa com corpo mais longo e sem sombra superior abre uma posição comprada. As posições são fechadas quando se forma um candle oposto sem o pavio correspondente.

## Detalhes

- **Critérios de entrada**: HA de alta sem pavio inferior e corpo maior que o anterior para vendidos; HA de baixa sem pavio superior e corpo maior que o anterior para comprados
- **Comprado/Vendido**: Comprado e Vendido
- **Critérios de saída**: candle HA de cor oposta sem pavio
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = candles de 15 minutos
- **Filtros**:
  - Categoria: Padrão
  - Direção: Reversão
  - Indicadores: Heikin-Ashi
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
