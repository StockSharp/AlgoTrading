# Estratégia Triple SMA Spread
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é uma porta em C# do consultor especialista MetaTrader 5 `3sma.mq5` (id 21495). Segue a mesma ideia de negociar quando três médias móveis simples se separam umas das outras por um spread configurável. A implementação usa a API de alto nível do StockSharp com assinaturas de velas e vinculação de indicadores para que nenhum gerenciamento manual de séries seja necessário.

## Comportamento original do MT5
O especialista MT5 baseia-se em três médias móveis simples com diferentes períodos e deslocamentos de exibição. A média rápida usa a barra atual, enquanto as médias média e lenta são deslocadas uma e duas barras para o passado. Em cada tick:

1. Converte o spread definido pelo usuário de pips para unidades de preço com base na precisão do símbolo.
2. Fecha posições compradas quando a SMA rápida cai abaixo da SMA média em pelo menos metade do spread, e fecha posições vendidas quando a SMA rápida sobe acima da SMA média em metade do spread.
3. Abre novas posições compradas se `MA1 > MA2 + spread` e `MA2 > MA3 + spread` enquanto nenhum outro trade comprado do especialista permanecer. Analogamente, abre posições vendidas quando todas as três médias estão alinhadas na ordem oposta.
4. Usa apenas ordens de mercado com um tamanho de lote fixo e não aplica níveis explícitos de stop-loss ou take-profit.

## Implementação no StockSharp
* Indicadores – três instâncias de `SimpleMovingAverage` se inscrevem na mesma fonte de velas. Buffers de histórico compactos reproduzem os parâmetros de "shift" do MT5 para que cada comparação use valores de barras terminadas com os deslocamentos solicitados.
* Tratamento do spread – o parâmetro de spread é inserido em pips. A estratégia deriva um tamanho de pip de `Security.PriceStep` (ou `Security.Step`) e multiplica por dez para símbolos FX de 3/5 dígitos, coincidindo com o ajuste do MT5 para cotações fracionárias.
* Fluxo de ordens – as ordens são enviadas com `BuyMarket`/`SellMarket`. Quando uma condição de reversão aparece, a estratégia adiciona o valor absoluto da posição líquida atual ao volume base para nivelar a exposição oposta e estabelecer a nova direção com uma única ordem de mercado.
* Visualização – se gráficos estiverem disponíveis, a estratégia traça as velas fonte junto com as três médias móveis e trades executados.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-----------|--------|
| `Volume` | Volume de ordem usado para cada entrada de mercado. | `0.1` |
| `FastMaPeriod` | Período da SMA rápida (equivalente a MA1 no MT5). | `9` |
| `FastMaShift` | Número de barras terminadas usadas para deslocar a SMA rápida. | `0` |
| `MiddleMaPeriod` | Período da SMA média (MA2). | `14` |
| `MiddleMaShift` | Deslocamento em barras terminadas para a SMA média. | `1` |
| `SlowMaPeriod` | Período da SMA lenta (MA3). | `29` |
| `SlowMaShift` | Deslocamento em barras terminadas para a SMA lenta. | `2` |
| `MaSpreadPips` | Spread mínimo requerido entre SMAs consecutivas medido em pips. | `10` |
| `CandleType` | Série de velas usada para cálculos. | Período de `1 minuto` |

## Lógica de trading
1. Aguardar até que todas as três médias móveis estejam formadas e os buffers de histórico contenham valores para os deslocamentos solicitados.
2. Converter o parâmetro de spread de pips para unidades de preço e calcular o meio-spread para filtros de saída.
3. **Filtros de saída** –
   * Fechar exposição comprada se a SMA rápida deslocada cair abaixo da SMA média deslocada em pelo menos metade do spread.
   * Fechar exposição vendida se a SMA rápida deslocada subir acima da SMA média deslocada em pelo menos metade do spread.
4. **Condições de entrada** –
   * Entrar comprado (ou reverter de vendido para comprado) quando a SMA rápida é maior que a SMA média mais o spread **e** a SMA média é maior que a SMA lenta mais o spread.
   * Entrar vendido (ou reverter de comprado para vendido) quando a SMA rápida é menor que a SMA média menos o spread **e** a SMA média é menor que a SMA lenta menos o spread.

## Diferenças da versão MT5
* O StockSharp trabalha com uma única posição líquida por instrumento. Quando um sinal de reversão aparece, a estratégia emite uma única ordem de mercado dimensionada para aplanar a exposição líquida anterior e estabelecer a nova direção. O especialista MT5 poderia manter posições compradas e vendidas independentes.
* A conversão de pips usa os melhores metadados de `Security` disponíveis. Se o broker não fornecer nem `PriceStep` nem `Step`, o valor `1` é usado como fallback.
* As ordens são enviadas em velas terminadas em vez de cada tick porque a API de alto nível opera em assinaturas de velas.
* A estratégia não implementa os helpers de registro verboso do código MT5; o registro integrado do StockSharp pode ser usado se necessário.

## Notas de uso
* Garantir que a série de velas selecionada corresponda ao período usado na configuração original do MT5.
* Ajustar o parâmetro de spread sempre que o instrumento usar tamanhos de pip não padrão.
* Como a estratégia trabalha com velas terminadas, a execução será atrasada até que a vela atual feche.
