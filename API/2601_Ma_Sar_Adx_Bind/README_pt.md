# Estratégia Ma SAR ADX Bind
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão para a API de alto nível do StockSharp do consultor especialista original **MaSarADX.mq5** do MetaTrader 5. O sistema combina um filtro de tendência de média móvel simples com sinais do Índice de Movimento Direcional (ADX) e o trailing stop Parabolic SAR. As decisões de trading são avaliadas apenas em candles concluídos, replicando o comportamento do "primeiro tick de uma nova barra" da versão MQL. Quando o fechamento do candle está alinhado tanto com a tendência da média móvel quanto com o equilíbrio direcional do ADX, uma posição é aberta. O Parabolic SAR orienta tanto a direção do trade quanto as saídas forçando uma liquidação total quando o preço cruza para o lado oposto dos pontos SAR.

## Indicadores e dados
- **Média Móvel Simples (SMA)** – fornece o filtro de direção de tendência primária. Comprimento padrão: 100 candles.
- **Índice Direcional Médio (ADX)** – fornece +DI e −DI para confirmar a força direcional. Comprimento padrão: 14.
- **Parabolic SAR** – atua como uma sobreposição de stop-and-reverse e define as condições de saída. Aceleração padrão: 0.02; aceleração máxima: 0.10.
- **Candles** – qualquer timeframe pode ser solicitado. Por padrão a estratégia assina candles de 1 hora, mas o parâmetro pode ser ajustado para corresponder ao símbolo e regime de teste.

A implementação assina fluxos de candles do StockSharp e vincula os três indicadores usando o helper `BindEx` de modo que cada callback recebe valores sincronizados para o mesmo candle.

## Lógica de trading
### Entrada comprada
1. O fechamento do candle está acima da média móvel.
2. +DI é maior ou igual a −DI, indicando pressão direcional de alta.
3. O fechamento do candle está acima do valor do Parabolic SAR.
4. Não há posição comprada atualmente aberta (`Position <= 0`).

Quando todas as regras se alinham, uma ordem de compra de mercado é enviada pelo volume base configurado mais qualquer tamanho necessário para cobrir uma posição vendida.

### Entrada vendida
1. O fechamento do candle está abaixo da média móvel.
2. +DI é menor ou igual a −DI, indicando pressão direcional de baixa.
3. O fechamento do candle está abaixo do valor do Parabolic SAR.
4. Não há posição vendida atualmente aberta (`Position >= 0`).

Uma ordem de venda de mercado é colocada quando todas as regras de venda correspondem.

### Saídas
- As **posições compradas** são fechadas imediatamente assim que o preço cai abaixo do Parabolic SAR.
- As **posições vendidas** são cobertas quando o preço sobe acima do Parabolic SAR.

Nenhum nível separado de stop-loss ou take-profit é adicionado; o cruzamento do SAR é o único sinal de saída, seguindo o consultor especialista original. Como as saídas são avaliadas antes de novas entradas, a estratégia não vai inverter de vendido para comprado (ou vice-versa) no mesmo candle, espelhando o ciclo de abertura/fechamento em duas etapas do script MQL.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `MaPeriod` | Comprimento da média móvel simples usada para definir o filtro de tendência. | 100 | Otimizável, deve ser maior que zero. |
| `AdxPeriod` | Período do cálculo de ADX que produz +DI e −DI. | 14 | Otimizável, deve ser maior que zero. |
| `SarStep` | Fator de aceleração e incremento para o Parabolic SAR. | 0.02 | Equivalente ao parâmetro `step` do MQL. |
| `SarMax` | Fator máximo de aceleração para Parabolic SAR. | 0.10 | Espelha a configuração `maximum` do MQL. |
| `Volume` | Tamanho de ordem base para novas entradas. | 1 | Substitui o dimensionamento de lotes baseado em margem da versão MetaTrader. O tamanho real da ordem é `Volume + |Position|` para que as reversões liquidem a exposição existente. |
| `CandleType` | O tipo de dados de candles assinado através do StockSharp. | 1 hora | Ajustável para qualquer timeframe. |

## Notas de implementação
- O processamento de indicadores usa o pipeline de alto nível `BindEx` do StockSharp, garantindo que SMA, ADX e SAR sejam atualizados em sincronia perfeita sem buffering manual.
- As saídas são executadas mesmo se `AllowTrading` estiver temporariamente desabilitado, mantendo os controles de risco ativos em todos os momentos.
- Helpers de graficação estão incluídos: o painel principal mostra preço, SMA e SAR, enquanto um painel secundário mostra o indicador ADX para diagnósticos.
- Declarações de log descrevem cada decisão de trading com os valores subjacentes do indicador para simplificar testes futuros e depuração.

## Diretrizes de uso
1. Anexe a estratégia a um título e portfólio no Designer ou Backtester.
2. Ajuste o tipo de candle para corresponder ao seu horizonte de trading (p.ex., candles M15, H1, ou D1).
3. Ajuste o período da média móvel, o período do ADX e os parâmetros SAR para se adaptar à volatilidade do instrumento.
4. Defina o parâmetro `Volume` para o tamanho de posição preferido. Se precisar do gerenciamento de dinheiro adaptativo usado no script original, integre seu próprio dimensionamento baseado em portfólio antes de enviar ordens.
5. Execute a estratégia. Trades serão acionados somente após todos os indicadores terem produzido valores históricos suficientes para estarem formados.

## Diferenças em relação ao consultor especialista original
- O cálculo de lotes baseado em margem foi substituído por um parâmetro fixo `Volume` para manter a estratégia broker-neutral dentro do StockSharp.
- A gestão de trades, os valores do indicador e a ordem de avaliação (saída antes de entrada) seguem estritamente a lógica de referência do MetaTrader.
- Todos os comentários dentro do código-fonte estão em inglês para cumprir com as diretrizes do projeto.
