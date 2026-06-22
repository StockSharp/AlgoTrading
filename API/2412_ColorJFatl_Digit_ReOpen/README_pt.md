# Estratégia ColorJFatl Digit ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa uma Jurik Moving Average (JMA) para identificar a direção da tendência. Uma posição comprada é aberta quando a JMA vira para cima e todas as posições vendidas são fechadas. Uma posição vendida é aberta quando a JMA vira para baixo e todas as posições compradas são fechadas. Posições adicionais são acrescentadas cada vez que o preço se move um número fixo de pontos na direção da operação, até um máximo.

## Detalhes

- **Entrada**:
  - JMA muda de direção para cima → abrir comprado e fechar vendidos.
  - JMA muda de direção para baixo → abrir vendido e fechar comprados.
- **Re-entrada**:
  - Após a posição inicial, novas posições abrem a cada `PriceStep` pontos na direção da operação até `MaxPositions` ser atingido.
- **Saída**:
  - Virada oposta da JMA fecha as posições atuais.
- **Parâmetros**:
  - `JmaLength` – período da JMA.
  - `PriceStep` – movimento de preço em pontos necessário para re-entrada.
  - `MaxPositions` – número máximo de posições simultâneas.
  - `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – habilitar ou desabilitar ações.
  - `CandleType` – período para cálculos.
- **Indicador**: Jurik Moving Average.
- **Tipo**: Seguidor de tendência.
- **Período**: 4 horas por padrão.
