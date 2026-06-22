# Estratégia Fisher Transform X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o indicador Fisher Transform em dois períodos diferentes. O período superior define a tendência geral, enquanto o inferior gera entradas quando Fisher cruza seu valor anterior contra essa tendência. Parâmetros opcionais permitem fechar posições em mudança de tendência ou em sinais de cruzamento.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Fisher de tendência subindo` && `Fisher de sinal cruza abaixo do seu valor anterior`
  - **Vendido**: `Fisher de tendência caindo` && `Fisher de sinal cruza acima do seu valor anterior`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Fechamento opcional em reversão de tendência
  - Fechamento opcional em cruzamento oposto do Fisher no período de sinal
- **Stops**: Take profit e stop loss em pontos
- **Valores padrão**:
  - `Trend Length` = 10
  - `Signal Length` = 10
  - `Trend Timeframe` = 6 horas
  - `Signal Timeframe` = 30 minutos
  - `Take Profit` = 2000 pontos
  - `Stop Loss` = 1000 pontos
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Fisher Transform
  - Stops: Sim
  - Complexidade: Médio
  - Período: Multi-timeframe
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
