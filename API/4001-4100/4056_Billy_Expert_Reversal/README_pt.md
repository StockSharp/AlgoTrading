# Estratégia do Especialista Billy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Convertido do MetaTrader 4 especialista original "Billy_expert.mq4".
- Estratégia de momentum longo que espera por quatro máximas descendentes consecutivas e abre antes de entrar.
- Usa dois osciladores estocásticos (rápido no período de negociação, lento em um período de tempo mais alto) para confirmar que o impulso está mudando para cima.
- Projetado para pares spot FX, mas pode ser aplicado a qualquer instrumento que forneça velas baseadas em minutos.

## Lógica de sinal
### Filtro de ação de preço
1. Avalie as velas concluídas no primeiro período.
2. São necessárias quatro velas consecutivas onde tanto a máxima quanto a abertura diminuem. Isso recria as verificações MT4 `High[0] < High[1] < High[2] < High[3]` e `Open[0] < Open[1] < Open[2] < Open[3]`.
3. O padrão sugere um movimento de baixa exausto e prepara a estratégia para uma negociação de reversão.

### Confirmação do oscilador
1. Calcule um oscilador estocástico rápido no período de negociação e um estocástico lento no período de confirmação.
2. Para cada oscilador, exija que a linha %K esteja acima da linha %D na vela concluída atual e anterior (`%K(0) > %D(0)` e `%K(1) > %D(1)`).
3. A negociação é acionada apenas quando ambos os osciladores confirmam simultaneamente a dinâmica de alta.

## Gerenciamento de pedidos
- Entradas: compras de mercado dimensionadas pelo parâmetro da estratégia `Volume` (caso exista posição vendida ela é fechada e revertida automaticamente).
- Stop loss: distância fixa abaixo do preço de preenchimento usando o parâmetro `Stop Loss (pts)`. Um valor de `0` desativa a parada.
- Take Profit: distância fixa acima do preço de preenchimento usando o parâmetro `Take Profit (pts)`. Um valor de `0` desativa o destino.
- Limite de posição: `Max Orders` limita quantas entradas longas podem estar ativas ao mesmo tempo. Como StockSharp mantém uma posição líquida, a estratégia aproxima o comportamento do MT4 contando quantos blocos `Volume` estão abertos no momento.
- Parada móvel: o EA original declarou uma entrada de parada móvel, mas não a implementou. A versão convertida também omite a lógica final para paridade.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Trading Candle` | Período primário para padrão de preço e estocástico rápido. | 1 minuto |
| `Slow Stochastic Candle` | Prazo maior utilizado para confirmação estocástica. | 5 minutos |
| `Stochastic Length` | Janela de lookback para %K. | 5 |
| `%K Smoothing` | Suavização aplicada à linha %K. | 3 |
| `%D Period` | Suavização aplicada à linha %D. | 3 |
| `Slowing` | Fator de suavização adicional para %K. | 3 |
| `Stop Loss (pts)` | Pare a distância de perda nas etapas de preço. | 0 |
| `Take Profit (pts)` | Calcule a distância do lucro nas etapas de preço. | 12 |
| `Max Orders` | Máximo de entradas longas simultâneas. | 1 |

## Notas de uso
- Defina a propriedade `Volume` antes de iniciar a estratégia; StockSharp é padronizado como `0`, o que bloquearia a colocação de pedidos.
- A etapa de preço é lida em `Security.PriceStep` (volta para `Security.Step` ou `1`). Certifique-se de que os metadados do seu instrumento estejam configurados corretamente para obter níveis precisos de parada/alvo.
- Quando o período de confirmação difere do período de negociação, a vela lenta concluída mais recentemente é reutilizada até que uma nova apareça, correspondendo ao comportamento do script MT4 original.
- O EA não administrou saídas além do stop loss e do take-profit do lado da corretora. A conversão reflete esse comportamento, enviando ordens de mercado protetoras quando os níveis são atingidos.
- Como StockSharp agrega posições, `Max Orders > 1` funciona melhor quando cada entrada usa o mesmo tamanho de `Volume`.

## Diferenças da versão MT4
- Verificação de segurança para informações ausentes de etapas de preço com um aviso de registro em vez de usar silenciosamente `Point`.
- Adicionadas cláusulas de guarda para garantir que a estratégia seja negociada somente quando todos os dados necessários (histórico de preços e ambos os osciladores estocásticos) estiverem disponíveis.
- A estratégia é executada apenas em velas finalizadas, enquanto o MT4 processa ticks, mas é acelerado pelo tempo da barra. Essa alteração evita avaliações duplicadas e mantém a lógica determinística.
