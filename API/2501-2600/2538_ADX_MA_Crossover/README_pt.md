# Cruzamento ADX MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o Consultor Especialista "ADX & MA" combinando uma média móvel suavizada com um filtro de tendência de Índice Direcional Médio (ADX). A lógica analisa as últimas duas velas concluídas no período selecionado e reage somente após tanto a média móvel quanto o ADX produzirem valores confirmados. É projetada para entradas no estilo de hedge, mas implementada em um modelo de posição líquida, revertendo automaticamente a posição quando sinais opostos aparecem.

A média móvel é calculada sobre o preço mediano de cada vela, correspondendo à versão do MetaTrader que usou uma SMMA construída sobre `(High + Low) / 2`. O limite do ADX evita operações quando a força da tendência é fraca, reduzindo sinais falsos de cruzamentos de curta duração.

## Lógica de entrada
- Aguardar até que tanto a média móvel suavizada quanto o ADX produzam valores finais.
- Avaliar o fechamento da vela anterior (`n-1`) em relação ao valor da MA suavizada tomado na mesma vela.
- Ir comprado quando:
  - O fechamento da vela `n-1` está acima do valor MA de `n-1`.
  - O fechamento da vela `n-2` estava abaixo desse valor MA (cruzamento altista), e
  - O valor ADX da vela `n-1` é maior ou igual a `AdxThreshold`.
- Ir vendido quando as condições inversas ocorrem (cruzamento baixista com confirmação do ADX).
- O tamanho da posição usa o `Volume` da estratégia mais o valor absoluto de qualquer exposição oposta para garantir uma reversão com sinais opostos.

## Lógica de saída
As operações compradas são encerradas quando qualquer uma das seguintes condições é acionada:
- O último fechamento confirmado (`n-1`) cai de volta abaixo da MA suavizada (cruzamento oposto).
- O preço atinge a distância de take-profit longa configurada em pips.
- O preço cai até a distância de stop-loss longa configurada em pips.
- O trailing stop para operações compradas bloqueia lucros assim que o preço se move `TrailingStopBuy` pips além do preço de entrada.

As operações vendidas espelham as mesmas regras com seus respectivos parâmetros e lógica de trailing. Cada vez que um sinal oposto aparece, a estratégia envia uma ordem de mercado grande o suficiente para fechar a posição atual e abrir uma na nova direção.

## Gestão de risco e operações
- As distâncias para take-profit, stop-loss e trailing stop são expressas em **pips**. A estratégia deriva o tamanho do pip de `Security.PriceStep`; quando o símbolo usa 3 ou 5 decimais, o pip é definido como `PriceStep × 10`, correspondendo ao ajuste original do MetaTrader.
- `InitializeLongTargets` e `InitializeShortTargets` calculam níveis de preço absolutos imediatamente após o envio da ordem de mercado, armazenando a aproximação do preço de entrada com base no último fechamento confirmado.
- Quando os trailing stops estão ativados e o preço se move favoravelmente além da distância configurada, o nível de stop é deslocado para preservar o lucro não realizado.
- Ambos os conjuntos de alvos são redefinidos quando a posição é fechada, para que níveis obsoletos nunca sejam reutilizados.

## Parâmetros
- `MaPeriod` – comprimento da média móvel suavizada (padrão 15).
- `AdxPeriod` – período de suavização do ADX (padrão 12).
- `AdxThreshold` – valor ADX mínimo necessário para confirmar uma tendência (padrão 16).
- `TakeProfitBuy` / `StopLossBuy` / `TrailingStopBuy` – distâncias em pips para operações compradas.
- `TakeProfitSell` / `StopLossSell` / `TrailingStopSell` – distâncias em pips para operações vendidas.
- `CandleType` – período para as velas de entrada, padrão 1 minuto.

Configure o `Volume` da estratégia para controlar o tamanho base da ordem. A implementação mantém o comportamento original onde as operações vendidas recebem suas próprias configurações de risco em vez de reutilizar os parâmetros comprados.
