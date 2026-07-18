# Estratégia de volume adaptativo MA2CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia MA2CCI é uma porta direta do consultor especialista MetaTrader 4 originalmente distribuído como "MA2CCI.mq4". O sistema combina um cruzamento de média móvel simples rápido/lento (SMA) com uma confirmação de linha zero do índice de canal de commodities (CCI). Cada cruzamento validado abre uma única posição de mercado e imediatamente coloca um stop de proteção baseado em Average True Range (ATR). O dimensionamento da posição segue a lógica original de gerenciamento de dinheiro, dimensionando o tamanho do pedido em relação ao patrimônio e reduzindo-o após séries de negociações perdidas.

## Indicadores e Dados
- **Rápido SMA (FMa)** e **Lento SMA (SMa)** no período de tempo configurado para detectar reversões de tendência.
- **Commodity Channel Index (CCI)** com o mesmo fluxo de preços para confirmar a direção do impulso por meio de cruzamentos de linha zero.
- **Average True Range (ATR)** para medir a volatilidade recente e derivar a distância do stop-loss.
- **Velas** do período escolhido (padrão 15 minutos) fornecem a série de entrada para todos os indicadores.

## Regras de negociação
- **Entrada longa**: O SMA rápido cruza acima do SMA lento enquanto CCI cruza de negativo para positivo na mesma barra, nenhuma posição é aberta e a negociação é permitida. Uma ordem de compra a mercado é enviada e um stop loss é armado em `close − ATR × AtrMultiplier`.
- **Entrada curta**: O rápido SMA cruza abaixo do lento SMA enquanto CCI cruza de positivo para negativo, nenhuma posição está aberta. Uma ordem de venda a mercado é colocada com um stop loss em `close + ATR × AtrMultiplier`.
- **Saída para posições compradas**: Se o SMA rápido voltar abaixo do SMA lento, toda a posição comprada será fechada no mercado. A parada de proteção também é cancelada.
- **Saída para vendas**: Se o rápido SMA cruzar novamente acima do lento SMA, a posição curta será coberta no mercado e o stop será cancelado.
- **Stop-loss**: Cada nova posição restaura um stop de volatilidade que reflete a lógica MetaTrader. As paradas são recalculadas apenas em novas entradas e são armazenadas como ordens condicionais separadas.

## Dimensionamento de posições
- O tamanho do lote base começa no parâmetro `BaseVolume` (lote padrão 0,1).
- Se `RiskFraction` for positivo, a estratégia calcula um tamanho adicional usando `equity × RiskFraction / 1000`, imitando a fórmula original `AccountFreeMargin`, e usa o máximo entre os dois valores.
- Após duas ou mais negociações perdedoras consecutivas, o tamanho do lote é reduzido em `volume × losses / DecreaseFactor`, replicando o controle de rebaixamento de `DcF`.
- Os volumes são normalizados para o `VolumeStep` do instrumento.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `FastMaPeriod` | 4 | Período de retrospectiva rápido SMA. |
| `SlowMaPeriod` | 8 | Período de lookback SMA lento. |
| `CciPeriod` | 4 | Período do índice de canal de commodities. |
| `AtrPeriod` | 4 | Período médio de True Range usado para distância de parada. |
| `AtrMultiplier` | 1,0 | Multiplicador aplicado a ATR antes de colocar o stop-loss. |
| `BaseVolume` | 0,1 | Tamanho mínimo de negociação antes dos ajustes de risco. |
| `RiskFraction` | 0,02 | Fração de capital arriscado por negociação (por 1.000 unidades monetárias). |
| `DecreaseFactor` | 3 | Divisor que controla a rapidez com que o tamanho diminui após perdas. |
| `CandleType` | Velas de 15 minutos | Prazo usado para indicadores e sinais. |

## Notas
- As notificações por e-mail do consultor especialista original (`SndMl`) são omitidas intencionalmente.
- Apenas uma posição pode ser aberta por vez, correspondendo ao comportamento MetaTrader do código-fonte.
- As paradas de proteção são recriadas sempre que a posição muda ou fecha para evitar que os pedidos órfãos permaneçam no livro.
