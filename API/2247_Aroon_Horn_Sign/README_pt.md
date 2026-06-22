# Aroon Horn Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Aroon Horn Sign** procura reversões de tendência usando o indicador Aroon.
Ela monitora as linhas Aroon Up e Aroon Down em velas de períodos superiores. Quando a
linha Aroon Up cruza acima da linha Aroon Down e permanece acima do nível 50,
isso sinaliza uma possível reversão de alta. A estratégia fecha qualquer posição vendida
e abre uma nova posição comprada. Por outro lado, quando Aroon Down domina acima de 50,
qualquer posição comprada existente é fechada e uma posição vendida é iniciada.

A abordagem usa níveis fixos de take-profit e stop-loss expressos em unidades de preço.
Esses níveis são ativados através do módulo de proteção de risco incorporado.
Como a lógica depende apenas dos valores do Aroon, funciona em diferentes
mercados e períodos sem filtros adicionais.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: `Aroon Up` > `Aroon Down` e `Aroon Up` >= 50.
  - **Vendido**: `Aroon Down` > `Aroon Up` e `Aroon Down` >= 50.
- **Critérios de saída**:
  - Posições compradas fecham quando aparece uma condição de entrada vendida.
  - Posições vendidas fecham quando aparece uma condição de entrada comprada.
- **Stops**: Stop-loss e take-profit fixos usando `StartProtection`.
- **Valores padrão**:
  - `AroonPeriod` = 9
  - `CandleType` = velas de 4 horas
  - `TakeProfit` = 2000 (unidades de preço)
  - `StopLoss` = 1000 (unidades de preço)
- **Filtros**:
  - Categoria: Reversão de tendência
  - Direção: Comprado e Vendido
  - Indicadores: Aroon
  - Complexidade: Simples
  - Nível de risco: Médio
