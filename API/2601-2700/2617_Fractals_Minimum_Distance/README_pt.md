# Fractals Distância Mínima
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Fractals Distância Mínima replica o consultor especialista do MetaTrader "Fractals minimum distance" usando a API de estratégia de alto nível do StockSharp. O sistema varre a série de candles configurada em busca de padrões fractais de cinco barras no estilo Bill Williams. Cada vez que um novo fractal confirmado aparece no deslocamento da barra de sinal especificada, a estratégia mede a distância entre os fractais de subida e descida mais recentes. Uma ordem de mercado só é permitida quando essa distância excede o limiar necessário expresso em pips.

A conversão mantém o comportamento original de fechar qualquer exposição oposta imediatamente antes de reverter. Ao contrário da versão MQL, o tamanho da posição é retirado da propriedade `Volume` da estratégia em vez de realizar cálculos de risco baseados na conta. Nenhuma ordem de stop-loss ou take-profit é enviada, correspondendo ao expert de origem.

## Lógica de sinais
1. Assinar o tipo de candle definido por `CandleType` e construir buffers deslizantes de máximas e mínimas que sempre contenham a barra localizada `SignalBar` candles no passado junto com dois vizinhos em cada lado.
2. Detectar um **fractal superior** quando a máxima da barra central é estritamente maior que as máximas das duas velas anteriores e das duas seguintes. Detectar um **fractal inferior** de forma análoga para as mínimas.
3. Converter o parâmetro `DistancePips` em uma distância de preço usando o `PriceStep` do símbolo. Símbolos com três ou cinco dígitos decimais são automaticamente ajustados para tratar cotações de 0.001/0.00001 como um pip.
4. Quando um fractal superior é confirmado:
   - Armazenar o novo nível superior e fechar as posições compradas existentes.
   - Se tanto o último fractal superior quanto o inferior forem conhecidos e sua diferença absoluta for pelo menos o limiar de distância, enviar uma ordem de venda a mercado usando `Volume`.
5. Quando um fractal inferior é confirmado:
   - Armazenar o novo nível inferior e fechar as posições vendidas existentes.
   - Se a condição de distância for satisfeita, enviar uma ordem de compra a mercado usando `Volume`.

As operações são colocadas apenas após o fechamento do candle que finaliza o fractal, garantindo que barras inacabadas nunca acionem entradas. A estratégia baseia-se em `IsFormedAndOnlineAndAllowTrading()` para evitar colocar ordens antes que o ambiente esteja pronto.

## Parâmetros
| Nome | Descrição | Notas |
| --- | --- | --- |
| `DistancePips` | Espaçamento mínimo entre os últimos fractais de subida e descida medido em pips. | Convertido internamente para unidades de preço usando o tamanho do tick do instrumento. |
| `SignalBar` | Número de barras completamente fechadas que devem passar após a barra que hospeda o fractal. | O valor efetivo mínimo é 2, correspondendo à confirmação de duas barras usada pelos fractais de Bill Williams. |
| `CandleType` | Série de dados que alimenta os cálculos. | O padrão é o período de um minuto; altere para trabalhar com outras resoluções. |
| `Volume` | Propriedade padrão da estratégia StockSharp que define o tamanho da operação. | Substitui o dimensionamento baseado em risco original do expert MetaTrader. |

## Gestão de posições e diferenças em relação ao MQL
- As posições são sempre zeradas antes de reverter a direção, exatamente como o helper `ClosePositions` de origem fazia.
- O expert original chamava `RefreshRates()` e realizava configurações explícitas de slippage. Esses aspectos são delegados à infraestrutura do StockSharp neste port.
- Ordens de stop-loss e take-profit não faziam parte da lógica MQL e permanecem ausentes aqui.
- `DistancePips` usa precisão inteira como a entrada `ushort`, enquanto `SignalBar` espelha a entrada `uchar` do MQL.
- Como o StockSharp trabalha com posições líquidas, abrir uma ordem na direção oposta vira automaticamente a exposição, correspondendo ao comportamento de netting do MetaTrader.

## Dicas de uso
- Comece com o mesmo deslocamento de barra de sinal (`SignalBar = 3`) do código original e calibre o limiar de distância de acordo com a volatilidade do instrumento.
- Aumente `SignalBar` para aguardar mais candles após o aparecimento de um fractal, o que pode filtrar oscilações rápidas.
- Combine com gerenciamento de risco externo, como o helper integrado `StartProtection()`, se um stop de proteção for necessário.
