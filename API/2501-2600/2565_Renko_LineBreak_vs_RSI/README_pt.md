# Estratégia Renko Line Break vs RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o especialista MetaTrader "RenkoLineBreak vs RSI" usando a API de alto nível do StockSharp. Combina a detecção de tendência Renko com um filtro de retrocesso RSI e executa operações através de ordens stop pendentes localizadas em torno de uma estrutura de preço de três velas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A tendência Renko permanece altista e o RSI cai até `50 - RsiShift` ou abaixo. Uma ordem stop de compra é colocada na máxima da vela de três barras atrás mais `IndentFromHighLow`.
  - **Vendido**: A tendência Renko permanece baixista e o RSI sobe até `50 + RsiShift` ou acima. Uma ordem stop de venda é colocada na mínima da vela de três barras atrás menos `IndentFromHighLow`.
  - As ordens pendentes são canceladas quando a tendência Renko muda de direção (`ToUp` / `ToDown`).
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Saídas de mercado quando a transição Renko oposta aparece (`ToDown` para comprados, `ToUp` para vendidos).
  - O RSI cruza de volta pelo ponto médio (`50 ± RsiShift`).
  - Intervalos de velas atingindo os níveis de stop loss ou take profit planejados.
- **Stops**:
  - O stop loss está ancorado ao extremo das últimas três velas mais `IndentFromHighLow`.
  - O take profit está a `TakeProfit` unidades de preço da entrada pretendida (opcional quando definido como zero).
- **Valores padrão**:
  - `BoxSize` = 500m.
  - `RsiPeriod` = 4.
  - `RsiShift` = 20m.
  - `TakeProfit` = 1000m.
  - `IndentFromHighLow` = 50m.
  - `Volume` = 1m.
  - `CandleType` = período de 5 minutos.
- **Filtros**:
  - Categoria: Seguidor de tendência.
  - Direção: Ambos.
  - Indicadores: Renko, RSI.
  - Stops: Stop fixo e take profit.
  - Complexidade: Intermediário.
  - Período: Híbrido (Renko + velas temporais).
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.

## Como funciona

1. Uma subscrição Renko (`RenkoCandleMessage`) estima a direção da tendência. Quando um tijolo Renko muda de direção, o estado de tendência é definido como `ToUp` ou `ToDown` por uma barra para imitar o comportamento do indicador original.
2. Simultaneamente, um fluxo de velas baseado em tempo alimenta o indicador RSI e fornece as últimas três máximas/mínimas usadas para os níveis de ruptura.
3. Quando as condições de tendência Renko e RSI se alinham, a estratégia registra uma ordem stop (compra ou venda). Os níveis planejados de stop loss e take profit são armazenados e monitorados após o disparo da ordem.
4. Após a execução da ordem, os níveis de proteção armazenados tornam-se ativos. As velas subsequentes verificam se o preço atinge os intervalos de stop ou alvo; se sim, a posição é fechada a mercado.
5. Se o impulso diminui (RSI cruza de volta pelo ponto médio) ou a tendência Renko muda, a posição é fechada antecipadamente.

## Indicadores utilizados

- **Tijolos Renko** para inferir o viés direcional e detectar transições entre estados de alta e baixa.
- **Relative Strength Index (RSI)** para qualificar entradas exigindo retrocessos contra a tendência.

## Notas adicionais

- `IndentFromHighLow` modela o buffer do especialista original que mantém as ordens de entrada e stop afastadas das máximas e mínimas recentes.
- `TakeProfit` pode ser definido como zero para desabilitar o alvo de lucro enquanto mantém a lógica de stop loss intacta.
- A estratégia mantém apenas uma ordem pendente de cada vez e a cancela automaticamente quando as condições do mercado invalidam a configuração.
