# Estratégia certa de inteligência artificial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o consultor especialista MetaTrader 4 **ArtificialIntelligence_Right.mq4**. Ele avalia uma camada única
O perceptron construiu o oscilador de aceleração/desaceleração (AC) para decidir quando a dinâmica do mercado muda de direção. O
O perceptron usa quatro amostras AC atrasadas e as transforma em um sinal assinado que aciona entradas e reversões.

Ao contrário do EA original, a porta StockSharp funciona na vela de alto nível API. As ações de preço são realizadas no fechamento de cada
vela finalizada, que mantém a lógica determinística para backtests e fluxos de trabalho de otimização.

## Indicadores e Cálculos
- **Oscilador de aceleração/desaceleração** é reconstruído a partir do Awesome Oscillator subtraindo um SMA de 5 períodos do AO
valores (5 períodos SMA de `HL2` menos 34 períodos SMA de `HL2`).
- Um buffer circular armazena os 22 valores AC mais recentes para que o perceptron possa acessar os deslocamentos 0, 7, 14 e 21, correspondendo exatamente
a implementação MetaTrader.
- Os pesos do perceptron são deslocados em `-100` antes do produto escalar, reproduzindo a lógica `w = x - 100` do código-fonte.

## Regras de negociação
1. **Condições de entrada**
   - Quando a saída do perceptron é positiva e a estratégia é plana, uma ordem de compra de mercado é enviada.
   - Quando a saída do perceptron é negativa e a estratégia é plana, uma ordem de venda a mercado é enviada.
2. **Gerenciamento de stop-loss**
   - Uma parada de proteção virtual é atribuída após cada entrada a uma distância igual a `StopLossPoints * PriceStep` de distância do
preço de entrada. Isso emula o multiplicador `Point` de MetaTrader.
   - Se o preço de fechamento ultrapassar este nível, a posição é encerrada no mercado para imitar a execução da ordem stop-loss.
3. **Trailing e reversão**
   - Assim que a posição flutuar no lucro em `(2 * StopLossPoints + SpreadPoints)` pontos, o robô original inicia
seguindo o stop pela distância de stop-loss ou reverte se o perceptron mudar seu sinal.
   - A versão StockSharp usa o mesmo gatilho: quando o limite de lucro é atingido, se o perceptron mudar de direção,
é emitida uma ordem de mercado com o dobro da exposição atual para reverter a negociação; caso contrário, a parada virtual será arrastada para
preservar a distância original do fechamento atual.

Todas as reversões são realizadas negociando o dobro do volume aberto para que a posição resultante espelhe o MetaTrader `OrderCloseBy`
comportamento, terminando na direção oposta, mas com o mesmo tamanho de lote.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `X1` … `X4` | Pesos de perceptron. Os padrões replicam a origem `.mq4` (135, 127, 16, 93). |
| `StopLoss` | Distância de stop-loss expressa em MetaTrader pontos. É multiplicado pelo instrumento `PriceStep` para obter uma compensação de preço real. |
| `Spread` | Buffer de propagação adicional (padrão 3 pontos) usado na condição de disparo final. |
| `Candle Type` | Série de velas usadas para cálculos. O padrão é o período de 1 minuto. |

A propriedade `Volume` é predefinida para 1 lote, espelhando o parâmetro de entrada `lots` do especialista original.

## Notas de implementação
- Os cálculos do indicador e o estado do perceptron são redefinidos sempre que a estratégia é redefinida para evitar que valores obsoletos causem
gatilhos falsos.
- Se a segurança não fornecer um `PriceStep`, a estratégia volta para um valor de ponto de `1`, mantendo a compatibilidade
com instrumentos genéricos de backtesting.
- Nenhuma ordem real de stop é registrada; em vez disso, a lógica de parada é executada por meio de ordens de mercado no manipulador de velas. Isto mantém o
comportamento consistente entre corretores e simuladores.
