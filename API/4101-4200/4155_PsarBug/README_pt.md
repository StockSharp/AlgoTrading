# Estratégia de Bug Psar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Psar Bug Strategy** é uma versão direta do MetaTrader 4 consultor especialista `pSAR_bug.mq4`. Ele reage ao primeiro Parabolic SAR ponto que aparece no lado oposto do preço e inverte imediatamente a posição. A implementação StockSharp assina velas, avalia apenas barras concluídas e usa o API de alto nível para colocar ordens de mercado e gerenciar stops de proteção.

## Lógica de negociação
- Calcule o Parabolic SAR com um passo de aceleração de `0.02` e uma aceleração máxima de `0.2` (ambos configuráveis).
- Aguarde por uma vela finalizada onde o valor Parabolic SAR muda em relação ao fechamento:
  - **Entrada comprada**: o valor atual de SAR está abaixo do preço de fechamento, enquanto o valor anterior de SAR estava acima do preço de fechamento anterior.
  - **Entrada curta**: o valor atual de SAR está acima do preço de fechamento, enquanto o valor anterior de SAR estava abaixo do preço de fechamento anterior.
- Inverta a exposição existente em cada sinal. Quando um sinal de compra aparece, qualquer posição curta aberta é achatada e substituída por uma posição longa do tamanho configurado. O oposto se aplica aos sinais de venda.
- Aplique distâncias fixas de stop-loss e take-profit expressas em etapas de preço do instrumento. A proteção é implementada com `StartProtection` para que os parâmetros de risco sejam automaticamente anexados a cada nova posição.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `TradeVolume` | Volume de pedidos em lotes utilizados para lançamentos. O valor padrão é `0.1` lotes. |
| `StopLossPoints` | Distância do preço de entrada ao stop loss expresso em etapas de preço. Espelha a entrada MetaTrader `StopLoss`. |
| `TakeProfitPoints` | Distância do preço de entrada ao take-profit expresso em etapas de preço. Espelha a entrada MetaTrader `TakeProfit`. |
| `SarAccelerationStep` | Fator de aceleração inicial do indicador Parabolic SAR. |
| `SarAccelerationMax` | Fator de aceleração máximo para o cálculo Parabolic SAR. |
| `CandleType` | Tipo de dados de vela (período de tempo) usado para os cálculos do indicador. Por padrão, a estratégia funciona em velas de 15 minutos. |

## Notas sobre a conversão
- O especialista original faz referência direta ao símbolo e ao período de tempo do gráfico atual. A versão StockSharp expõe o tipo de vela como um parâmetro para que o período de tempo possa ser alterado sem recompilar.
- As paradas de proteção são representadas como compensações de preços absolutos. Eles são inicializados uma vez na inicialização e gerenciados automaticamente pela plataforma.
- O gerenciamento de pedidos depende da lógica de compensação: comprar `Volume + |Position|` lotes fecha a posição curta anterior e abre a nova posição longa, reproduzindo o comportamento MetaTrader de fechar antes de abrir na direção oposta.

## Uso
1. Configure os parâmetros de segurança, prazo (`CandleType`) e risco desejados no StockSharp Designer ou Backtester.
2. Certifique-se de que os dados de mercado estejam disponíveis e inicie a estratégia. Os sinais são avaliados apenas em velas finalizadas.
3. Monitore posições e desempenho por meio das ferramentas padrão StockSharp. Os gráficos exibem velas, o indicador Parabolic SAR e negociações executadas para validação visual dos sinais de reversão.
