# Estratégia Bollinger Band Two MA ZigZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema híbrido seguidor de tendência que combina reversões por Bollinger Band, duas médias móveis de períodos superiores e pontos de swing de um detector ZigZag. Abre duas posições a cada sinal: uma com alvo de take-profit calculado e uma segunda "corredora" que depende de trailing e lógica de break-even.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A barra anterior fechou acima da banda inferior anterior de Bollinger após fechar abaixo dela duas barras atrás, o fechamento atual também está acima dessa banda inferior, e o preço está acima de ambas as médias móveis de períodos superiores.
  - **Vendido**: A barra anterior fechou abaixo da banda superior anterior de Bollinger após fechar acima dela duas barras atrás, o fechamento atual também está abaixo dessa banda superior, e o preço está abaixo de ambas as médias móveis de períodos superiores.
- **Gestão de posição**:
  - Duas posições são abertas por sinal usando `First Volume` (com take-profit) e `Second Volume` (corredora).
  - Os stops são ancorados ao extremo de swing ZigZag mais recente menos/mais `Pivot Offset (pts)`.
  - A proteção de break-even desloca o stop para a entrada mais um offset quando o lucro não realizado supera `Break-even Threshold (pts)` + `Break-even Offset (pts)`.
  - O trailing stop se move após o preço avançar `Trailing Step (pts)` além do stop existente, mantendo uma distância de `Trailing Stop (pts)`.
- **Take Profit**:
  - O take-profit da primeira posição é calculado como uma porcentagem (`Take Profit %`) da distância entre a entrada e o stop.
  - A posição corredora não tem alvo fixo e sai por stop, trailing ou sinais opostos.
- **Lógica adicional**:
  - Sinais opostos fecham imediatamente todas as posições abertas na outra direção antes de abrir novas operações.
  - O processamento de sinais usa velas fechadas; dados parciais são ignorados.
- **Valores padrão**:
  - `First Volume` = 0.1
  - `Second Volume` = 0.1
  - `Take Profit %` = 50
  - `Pivot Offset (pts)` = 10
  - `Use Break-even Move` = true
  - `Break-even Offset (pts)` = 80
  - `Break-even Threshold (pts)` = 10
  - `Trailing Stop (pts)` = 80
  - `Trailing Step (pts)` = 120
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `Base Candle` = velas de 1 hora
  - `MA1 Candle` = velas diárias
  - `MA2 Candle` = velas de 4 horas
  - `MA1 Period` = 20
  - `MA2 Period` = 20
  - `ZigZag Depth` = 12
  - `ZigZag Deviation (pts)` = 5
  - `ZigZag Backstep` = 3
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Moving Averages, ZigZag
  - Stops: Sim (stop por swing, break-even, trailing)
  - Complexidade: Avançado
  - Período: Multi-período (base 1h, filtros Daily + 4h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Notas

- A estratégia requer assinaturas de velas em três períodos distintos para avaliar os filtros e gerenciar as saídas.
- A detecção de swings aproxima a lógica ZigZag do MetaTrader aplicando regras mínimas de profundidade, desvio e backstep antes de atualizar os níveis de pivô.
- Os volumes podem ser ajustados independentemente para calibrar o tamanho do segmento de take-profit em relação ao segmento corredor.
