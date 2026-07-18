# Parabolic SAR Estratégia do Bug 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Parabolic SAR Estratégia Bug 2** é a StockSharp conversão de alto nível do MetaTrader consultor especialista `pSAR_bug2` da pasta `MQL/9503`. O EA original reage ao primeiro Parabolic SAR ponto que aparece no lado oposto do preço. Quando o ponto fica abaixo do fechamento, o sistema fecha todas as negociações curtas e abre imediatamente uma posição longa; quando o ponto salta acima do fechamento, a lógica reflete o comportamento no lado vendido. Os níveis protetores de stop-loss e take-profit são calculados em preços brutos, exatamente como em MetaTrader, onde os valores são multiplicados pelo tamanho do instrumento `Point`.

A porta StockSharp mantém a mesma intenção enquanto aproveita o API de alto nível da estrutura. Ele assina velas finalizadas, vincula um indicador Parabolic SAR com parâmetros de aceleração configuráveis, monitora reversões de pontos e envia ordens de mercado dimensionadas para nivelar a exposição anterior e estabelecer a nova negociação.

## Lógica de negociação
1. **Preparação de indicadores**. A estratégia assina um tipo de vela definido pelo usuário (período de 15 minutos por padrão) e vincula um Parabolic SAR com etapa de aceleração `SarStep` e aceleração máxima `SarMaximum`.
2. **Rastreamento de estado**. Na primeira vela concluída, o algoritmo registra se o valor SAR está acima ou abaixo do fechamento. Cada nova vela compara a nova posição SAR com o estado armazenado anteriormente.
3. **Regras de entrada**.
   - **Entrada longa**: acionada quando o SAR se move de cima para baixo do fechamento. O volume da ordem é calculado como `TradeVolume + |Position|`, portanto, uma posição curta existente é fechada e revertida em uma única ordem de mercado. Após a entrada, os níveis de stop-loss e take-profit são armazenados em relação ao fechamento da vela.
   - **Entrada curta**: acionada quando o SAR se move de baixo para cima do fechamento. Qualquer posição longa existente é achatada e uma nova negociação curta é inserida no mercado com a mesma fórmula de tamanho combinado.
4. **Saídas de proteção**. Em cada vela concluída, os níveis de stop-loss e take-profit armazenados são comparados com o máximo/mínimo. Se o preço ultrapassar um nível de proteção, a estratégia envia uma ordem de mercado para fechar a posição aberta e redefine os valores de stop e take em cache.

## Gestão de risco
- As distâncias de stop-loss e take-profit são calculadas em preços brutos, multiplicando o `StopLossPoints` ou `TakeProfitPoints` configurado pela etapa de preço do título. Um fallback conservador de `0.0001` é usado quando o instrumento não publica uma etapa de preço.
- A estratégia verifica `IsFormedAndOnlineAndAllowTrading()` antes de enviar pedidos, garantindo que os dados do mercado estejam online e que a negociação seja permitida.
- As entradas de reversão sempre incluem o tamanho absoluto da posição atual, garantindo que a nova ordem nivele a exposição anterior antes de estabelecer a negociação oposta.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volume base do pedido em lotes. O mesmo valor é atribuído à propriedade interna `Strategy.Volume`. |
| `StopLossPoints` | `90` | Distância de stop-loss em faixas de preço. A distância é multiplicada pela etapa de preço do instrumento para obter a compensação de preço real. |
| `TakeProfitPoints` | `20` | Distância de take-profit em pontos de preço convertidos através da etapa de preço do instrumento. |
| `SarStep` | `0.001` | Fator de aceleração inicial para o indicador Parabolic SAR. |
| `SarMaximum` | `0.2` | Fator máximo de aceleração para o indicador Parabolic SAR. |
| `CandleType` | `15m time-frame` | Tipo de vela usado para cálculos e avaliação de sinal. |

## Notas sobre a conversão
- As ordens de stop-loss e take-profit do lado do corretor MetaTrader são emuladas monitorando os extremos das velas e enviando saídas de mercado quando os limites são violados.
- O MetaTrader EA exigia gerenciamento manual de `OrdersTotal()` e chamadas `OrderClose()` explícitas. A versão StockSharp atinge o mesmo comportamento enviando uma única ordem de mercado dimensionada como `TradeVolume + |Position|`, que fecha simultaneamente qualquer posição oposta e abre a nova.
- Nenhuma implementação Python é fornecida, correspondendo à solicitação da tarefa. A pasta atualmente contém apenas a versão C# da estratégia.
