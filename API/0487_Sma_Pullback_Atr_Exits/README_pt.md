# Estratégia de Pullback SMA + Saídas ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em pullbacks quando uma média móvel de curto prazo está acima ou abaixo de uma média de tendência de longo prazo. Posições compradas são abertas quando o preço cai abaixo da SMA rápida enquanto ela permanece acima da SMA lenta. Posições vendidas são abertas quando o preço sobe acima da SMA rápida enquanto ela permanece abaixo da SMA lenta. As saídas utilizam múltiplos do Average True Range a partir do preço de entrada.

## Detalhes

- **Critérios de entrada**:
  - Comprado: close < SMA rápida e SMA rápida > SMA lenta.
  - Vendido: close > SMA rápida e SMA rápida < SMA lenta.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O preço atinge o stop loss ou take profit baseado em ATR.
- **Stops**: Múltiplos de ATR para stop loss e take profit.
- **Valores padrão**:
  - `FastSmaLength` = 8
  - `SlowSmaLength` = 30
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 1.2
  - `AtrMultiplierTp` = 2.0
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, ATR
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
