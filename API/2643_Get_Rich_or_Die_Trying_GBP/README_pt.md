# Estratégia Get Rich or Die Trying GBP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia StockSharp reproduz o comportamento do especialista MetaTrader «Get Rich or Die Trying GBP». Ela se concentra na sobreposição ativa entre as sessões de Nova York e Londres e aguarda uma breve explosão de desequilíbrio direcional em velas de 1 minuto. O algoritmo conta quantas das últimas barras fecharam abaixo de sua abertura (rotuladas como "up" no código original) versus o número que fechou acima de sua abertura. Quando os contadores divergem, a estratégia procura uma oportunidade de operar contra o lado mais fraco durante os primeiros cinco minutos das janelas de tempo escolhidas.

O sistema sempre opera uma única posição por vez. Ele impõe um resfriamento de 61 segundos após cada entrada, carrega tanto um take-profit primário fixo quanto um objetivo secundário mais restrito, e opcionalmente segue o stop assim que o preço se move suficientemente a favor. Todas as distâncias são expressas em pips, convertidas internamente usando o passo de preço do ativo (com um multiplicador ×10 para cotações de 3 e 5 decimais) para que a lógica corresponda à implementação MT5 original.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Mais velas com `Open > Close` do que com `Open < Close` sobre as últimas `CountBars` velas de 1 minuto, tempo atual dentro dos primeiros cinco minutos de `22:00 + AdditionalHour` ou `19:00 + AdditionalHour`, sem posição aberta e o resfriamento de 61 segundos decorrido.
  - **Vendido**: Mais velas com `Open < Close` do que com `Open > Close` sob as mesmas restrições de tempo e resfriamento.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Take-profit primário em `TakeProfitPips` a partir da entrada e stop-loss em `StopLossPips`.
  - Saída antecipada quando o lucro flutuante atinge `SecondaryTakeProfitPips`.
  - Stop trailing opcional que se ativa quando o preço avança além de `TrailingStopPips + TrailingStepPips`, deslocando o stop por `TrailingStopPips` respeitando o passo de trailing.
- **Stops**: Stop-loss fixo, take-profit fixo, take-profit secundário e stop trailing opcional.
- **Filtro de tempo**: Opera apenas durante os primeiros cinco minutos após as horas ajustadas 19:00 e 22:00.
- **Resfriamento**: Aguarda pelo menos 61 segundos após cada entrada antes de permitir uma nova operação.
- **Valores padrão**:
  - `StopLossPips` = 100
  - `TakeProfitPips` = 100
  - `SecondaryTakeProfitPips` = 40
  - `TrailingStopPips` = 30
  - `TrailingStepPips` = 5
  - `CountBars` = 18
  - `AdditionalHour` = 2
  - `MaxPositions` = 1000
  - `CandleType` = período de 1 minuto
- **Notas**:
  - `MaxPositions` é preservado por compatibilidade com o especialista original, mas este port mantém apenas uma posição ativa por vez.
  - A conversão de pips se adapta automaticamente a símbolos FX de 3 e 5 decimais multiplicando o passo de preço por 10.
  - A lógica do stop trailing espelha a versão MT5: não se move até que o preço melhore além da distância de trailing e do passo de trailing.
