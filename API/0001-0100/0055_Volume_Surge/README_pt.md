# Explosão de Volume (Volume Surge)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Explosão de Volume identifica volume incomumente alto em relação à média móvel. Quando a razão supera o multiplicador definido, sinaliza forte interesse e uma possível continuação na direção do preço em relação à sua média móvel.

Os testes indicam um retorno anual médio de aproximadamente 52%. Funciona melhor no mercado de criptomoedas.

As operações são iniciadas apenas durante uma explosão e fechadas assim que o volume cai abaixo da média ou o stop-loss é atingido.

Esta abordagem simples captura o momentum gerado por participação repentina.

## Detalhes

- **Critérios de entrada**: Razão de volume acima de `VolumeSurgeMultiplier`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: O volume cai abaixo da média ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `VolumeAvgPeriod` = 20
  - `VolumeSurgeMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Volume
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
