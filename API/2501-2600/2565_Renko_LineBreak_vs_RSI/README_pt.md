# Estratégia Renko Line Break vs RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o especialista MetaTrader "RenkoLineBreak vs RSI" usando a API de alto nível do StockSharp. Combina a detecção de tendência Renko com um filtro de retrocesso RSI e entra a mercado assim que uma estrutura de preço de três velas confirma a configuração. Os tijolos Renko são calculados dentro da própria estratégia a partir dos fechamentos das velas temporais, portanto uma única subscrição de velas alimenta tudo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A tendência Renko permanece altista e o RSI cai até `50 - RsiShift` ou abaixo. A configuração é validada contra um nível de referência igual à máxima da vela de três barras atrás mais `IndentFromHighLow`, e uma ordem de compra a mercado é enviada no fechamento da vela de sinal.
  - **Vendido**: A tendência Renko permanece baixista e o RSI sobe até `50 + RsiShift` ou acima. A configuração é validada contra um nível de referência igual à mínima da vela de três barras atrás menos `IndentFromHighLow`, e uma ordem de venda a mercado é enviada no fechamento da vela de sinal.
  - Nenhuma nova entrada é feita enquanto a tendência Renko está em um estado de transição (`ToUp` / `ToDown`); a configuração armazenada é descartada.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Saídas de mercado quando a transição Renko oposta aparece (`ToDown` para comprados, `ToUp` para vendidos).
  - O RSI cruza de volta pelo ponto médio (`50 ± RsiShift`).
  - Intervalos de velas atingindo os níveis de stop loss ou take profit planejados.
- **Stops**:
  - O stop loss está ancorado ao extremo das últimas três velas mais `IndentFromHighLow`.
  - O take profit está a `TakeProfit` unidades de preço do nível de rompimento de referência (opcional quando definido como zero).
- **Valores padrão**:
  - `BoxSize` = 100m.
  - `RsiPeriod` = 4.
  - `RsiShift` = 10m.
  - `TakeProfit` = 1000m.
  - `IndentFromHighLow` = 50m.
  - `Volume` = 1m.
  - `CandleType` = período de 2 horas.
- **Filtros**:
  - Categoria: Seguidor de tendência.
  - Direção: Ambos.
  - Indicadores: Renko, RSI.
  - Stops: Stop fixo e take profit.
  - Complexidade: Intermediário.
  - Período: Um único período (os tijolos Renko são derivados dos fechamentos das velas).
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.

## Como funciona

1. Os tijolos Renko são construídos dentro da estratégia a partir dos fechamentos das velas temporais: um tijolo que continua a direção atual é gerado assim que o fechamento se afasta um `BoxSize` completo da âncora atual, enquanto um tijolo que inverte a direção exige dois `BoxSize`. Antes de o primeiro tijolo definir uma direção, basta um box em qualquer sentido. São gerados tantos tijolos quantos o movimento cobrir, e a âncora acompanha o movimento. Quando um tijolo muda de direção, o estado de tendência é definido como `ToUp` ou `ToDown` por um passo para imitar o comportamento do indicador original.
2. O mesmo fluxo de velas alimenta o indicador RSI e fornece as últimas três máximas/mínimas usadas para os níveis de ruptura, de modo que a estratégia abre exatamente uma subscrição de dados de mercado.
3. Quando as condições de tendência Renko e RSI se alinham, a estratégia envia uma ordem a mercado (compra ou venda). Os níveis planejados de stop loss e take profit são armazenados e monitorados assim que a posição é aberta.
4. Assim que a posição é aberta, os níveis de proteção armazenados tornam-se ativos. As velas subsequentes verificam se o preço atinge os intervalos de stop ou alvo; se sim, a posição é fechada a mercado.
5. Se o impulso diminui (RSI cruza de volta pelo ponto médio) ou a tendência Renko muda, a posição é fechada antecipadamente.

## Indicadores utilizados

- **Tijolos Renko** derivados dos fechamentos das velas temporais com o passo `BoxSize`, para inferir o viés direcional e detectar transições entre estados de alta e baixa.
- **Relative Strength Index (RSI)** para qualificar entradas exigindo retrocessos contra a tendência.

## Notas adicionais

- `IndentFromHighLow` modela o buffer do especialista original que mantém o nível de rompimento de referência e o stop loss afastados das máximas e mínimas recentes.
- `TakeProfit` pode ser definido como zero para desabilitar o alvo de lucro enquanto mantém a lógica de stop loss intacta.
- A estratégia mantém apenas uma posição de cada vez: uma nova entrada só é considerada quando ela está fora do mercado, e a configuração armazenada é descartada assim que as condições do mercado a invalidam.
