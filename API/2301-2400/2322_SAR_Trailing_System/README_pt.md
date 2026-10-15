# Estratégia SAR Sistema de Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que entra em posições compradas ou vendidas aleatórias em intervalos de tempo fixos e gerencia as saídas usando o indicador Parabolic SAR.
O valor do Parabolic SAR atua como um stop trailing: a posição é fechada quando o preço cruza o nível SAR.

## Detalhes

- **Critérios de entrada**:
  - A cada `TimerInterval`, se não houver posição aberta e `UseRandomEntry` estiver habilitado, uma negociação comprada ou vendida aleatória é aberta.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Preço cruzando o Parabolic SAR.
- **Stops**: Stop-loss inicial em ticks com saída trailing Parabolic SAR.
- **Valores padrão**:
  - `TimerInterval` = 300 segundos
  - `StopLossTicks` = 10
  - `AccelerationStep` = 0.02
  - `AccelerationMax` = 0.2
  - `UseRandomEntry` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
