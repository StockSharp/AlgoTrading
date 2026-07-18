# Estratégia de Canal de Arredondamento de MA Exponencial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia arredonda uma média móvel para um passo de tick fixo e constrói um canal baseado em ATR ao redor dela. Quando o candle anterior fecha acima da banda superior, a estratégia abre uma posição comprada. Quando o candle anterior fecha abaixo da banda inferior, abre uma posição vendida. Sinais opostos fecham as posições existentes. O stop loss e o take profit são definidos em ticks e gerenciados automaticamente.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O fechamento anterior está acima da banda superior arredondada.
  - **Vendido**: O fechamento anterior está abaixo da banda inferior arredondada.
- **Critérios de saída**:
  - **Comprado**: O fechamento anterior está abaixo da banda inferior.
  - **Vendido**: O fechamento anterior está acima da banda superior.
- **Indicadores**:
  - Média Móvel Exponencial.
  - Average True Range para a largura do canal.
- **Stops**: Sim, stop loss e take profit fixos em ticks.
- **Valores padrão**:
  - `MA period` = 12.
  - `ATR period` = 12.
  - `ATR factor` = 1.
  - `MA round` = 500 ticks.
  - `Stop loss` = 1000 ticks.
  - `Take profit` = 2000 ticks.
  - `Timeframe` = 4 horas.

## Filtros

- Categoria: Seguidor de tendência
- Direção: Ambos
- Indicadores: Múltiplos
- Stops: Sim
- Complexidade: Moderado
- Período: Médio prazo
- Sazonalidade: Não
- Redes neurais: Não
- Divergência: Não
- Nível de risco: Moderado
