# Estratégia especializada em velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Expert Candles** é uma versão StockSharp do consultor especialista MetaTrader 5 *Expert_Candles*. Ele monitora mais
ação de preço recente para formações de reversão de velas que apresentam sombras alongadas. Sempre que um composto de alta ou baixa
vela é detectada, a estratégia abre uma posição na respectiva direção e, opcionalmente, aplica gestão de dinheiro idêntica a
o original EA.

A implementação segue o StockSharp API de alto nível: assinaturas de velas são usadas para construir barras compostas, enquanto o mercado
ordens e níveis de proteção são gerenciados diretamente pela estratégia.

## Lógica de negociação

1. Cada vez que uma vela fecha, a estratégia a mescla com até `Range` velas anteriores até a altura total do composto
a barra excede `MinimumPoints` (convertido em pontos de preço usando o tamanho do pip do instrumento).
2. Um sinal de **alta** é emitido quando a barra composta tem uma sombra superior rasa (`ShadowSmall`) e uma sombra inferior profunda
(`ShadowBig`). Um sinal de **baixa** é emitido quando a sombra inferior é rasa e a sombra superior é dominante.
3. O preço de entrada é deslocado da vela próxima em `LimitFactor * rangeSize`. Valores positivos emulam o limite original
ordem que fica dentro do intervalo da vela.
4. As metas de stop-loss e take-profit são posicionadas em `StopLossFactor` e `TakeProfitFactor` múltiplos da altura composta.
Se qualquer um dos níveis for atingido nas velas subsequentes, a posição será fechada imediatamente.
5. Os sinais são considerados válidos para `ExpirationBars` velas concluídas. Depois que a janela de tempo passa, a estratégia espera por uma nova
formação antes de enviar novos pedidos.
6. Os sinais opostos fecham as posições existentes antes de iniciar as negociações na nova direção, imitando o comportamento MQL5.

## Gestão de dinheiro

* `FixedVolume` é usado como tamanho de pedido padrão.
* Quando um stop-loss está disponível e `RiskPercent` é maior que zero, a estratégia arrisca a percentagem selecionada do
patrimônio do portfólio. A distância de parada é convertida em valor monetário usando `Security.PriceStep` e `Security.StepPrice`.
* Os volumes são arredondados para o instrumento `VolumeStep` quando a troca expõe esses metadados.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | H1 | Prazo utilizado para solicitar velas. |
| `Range` | 3 | Número máximo de velas vizinhas combinadas em um padrão composto. |
| `MinimumPoints` | 50 | Altura composta mínima em pontos (com base em `PriceStep`) necessária para avaliar o padrão. |
| `ShadowBig` | 0,5 | Proporção que a sombra dominante deve ultrapassar para confirmar a reversão. |
| `ShadowSmall` | 0,2 | Proporção máxima permitida para a sombra oposta. |
| `LimitFactor` | 0,0 | Deslocamento de entrada como uma fração da altura composta (valores positivos deslocam o preço dentro da vela). |
| `StopLossFactor` | 2,0 | Distância de stop-loss como um múltiplo da altura composta. Defina como zero para desabilitar a parada de proteção. |
| `TakeProfitFactor` | 1,0 | Distância de lucro como um múltiplo da altura composta. Defina como zero para desabilitar o alvo. |
| `ExpirationBars` | 4 | Número de velas concluídas durante as quais um sinal permanece ativo. |
| `FixedVolume` | 0,1 | Tamanho do pedido substituto usado quando o dimensionamento baseado em risco não pode ser calculado. |
| `RiskPercent` | 10 | Percentagem de capital arriscado por negociação quando um stop-loss está disponível. |

## Notas de uso

- A estratégia depende de `Security.PriceStep`, `Security.StepPrice` e `Security.VolumeStep` para replicar o ponto MetaTrader
cálculos. Forneça metadados precisos do instrumento ou ajuste os parâmetros adequadamente.
- Os sinais são avaliados apenas em velas fechadas. Anexe a estratégia a um conector de série temporal que emite `CandleStates.Finished`
eventos para uma execução confiável.
- As saídas de proteção são simuladas fechando a posição assim que a máxima ou mínima de uma vela finalizada violar o valor calculado.
nível de stop-loss ou take-profit.
- A lista composta de velas é limitada a 500 itens para manter previsível o consumo de memória.

## Diferenças vs. versão MetaTrader

- A porta StockSharp usa ordens de mercado em vez de ordens de limite pendentes. O deslocamento de entrada reproduz o comportamento do limite por
mudando o preço de execução em relação ao fechamento da vela.
- A gestão do dinheiro é opcional; definir `RiskPercent` como zero restaura o comportamento do lote fixo do EA original.
- O tratamento de stop-loss e take-profit é realizado dentro da estratégia, e não por módulos externos de rastreamento.
