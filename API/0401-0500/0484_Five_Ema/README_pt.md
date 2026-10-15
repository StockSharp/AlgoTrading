# Estratégia 5 EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia 5 EMA marca um candle que fecha completamente abaixo ou acima da EMA de 5 períodos. Se o preço romper o extremo do candle de sinal dentro de três barras e fora da janela de bloqueio, a estratégia entra na direção do rompimento. Os alvos são baseados em uma relação risco-recompensa definida pelo usuário e as operações podem ser fechadas forçosamente em um horário específico.

## Detalhes

- **Critérios de entrada**:
  - Fechamento do candle e máxima abaixo da EMA → marcar para comprado; comprar quando o preço cruzar acima da máxima do sinal dentro de 3 barras.
  - Fechamento do candle e mínima acima da EMA → marcar para vendido; vender quando o preço cruzar abaixo da mínima do sinal dentro de 3 barras.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Stop no extremo oposto do candle de sinal.
  - Alvo em `TargetRR` × risco.
  - Saída opcional em horário personalizado (`ExitHour`, `ExitMinute`).
- **Stops**: Sim.
- **Valores padrão**:
  - `EmaLength` = 5
  - `TargetRR` = 3.0
  - `ExitHour` = 15, `ExitMinute` = 30
  - `BlockStartHour` = 15, `BlockStartMinute` = 0
  - `BlockEndHour` = 15, `BlockEndMinute` = 30
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado/Vendido
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Baixo
  - Período: 5 minutos
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
