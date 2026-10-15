# Estratégia Exp Blau CSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão em C# do consultor especializado MetaTrader 5 `Exp_BlauCSI`. Ela opera no Blau Candle Stochastic Index (CSI) calculado sobre uma série de velas selecionada. A estratégia pode reagir a rompimentos da linha zero ou a mudanças de direção no indicador e suporta níveis configuráveis de stop-loss e take-profit medidos em passos de preço.

## Lógica de trading

O Blau CSI compara um componente de momentum com o intervalo máximo-mínimo de velas recentes. Ambas as partes são suavizadas três vezes usando um tipo de média móvel selecionado.

* **Modo Rompimento** – abre uma posição comprada quando o indicador cruza abaixo de zero e fecha qualquer vendido enquanto o valor anterior era positivo. Abre uma posição vendida em um cruzamento acima de zero e fecha qualquer comprado enquanto o valor anterior era negativo.
* **Modo Torção** – abre uma posição comprada quando o indicador se vira para cima (valor sobe comparado com a barra anterior após declinar). Abre uma posição vendida quando o indicador se vira para baixo. A direção da barra anterior é sempre usada para fechar posições existentes no lado oposto.

Os sinais são avaliados em uma barra histórica configurável (`Signal Bar`) para garantir a confirmação em velas completamente fechadas.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `Entry Mode` | Seleciona a lógica `Breakdown` ou `Twist`. |
| `Smoothing Method` | Tipo de média móvel usado dentro do Blau CSI (Simple, Exponential, Smoothed, LinearWeighted ou Jurik). |
| `Momentum Length` | Número de barras usadas para calcular os componentes de momentum e intervalo. |
| `First/Second/Third Smoothing` | Profundidade dos três estágios de suavização aplicados ao momentum e intervalo. |
| `Smoothing Phase` | Parâmetro de fase para suavização Jurik (ignorado por outros métodos). |
| `Momentum Price` / `Reference Price` | Constantes de preço aplicadas para os valores de momentum líderes e defasados (fechamento, abertura, máximo, mínimo, mediana, típico, ponderado, simples, quarto, seguidor de tendência ou Demark). |
| `Signal Bar` | Deslocamento (em barras) ao avaliar o buffer do Blau CSI. Padrão `1` significa a barra fechada anterior. |
| `Stop Loss (pts)` | Distância do stop-loss em passos de preço (`0` desabilita). |
| `Take Profit (pts)` | Distância do take-profit em passos de preço (`0` desabilita). |
| `Allow Long/Short Entries` | Habilitar ou desabilitar a abertura de posições para cada direção. |
| `Allow Long/Short Exits` | Habilitar ou desabilitar sinais de saída para posições existentes. |
| `Candle Type` | Tipo de dados para a subscrição (padrão para período de 4 horas). |
| `Start Date` / `End Date` | Filtros de data para atividade de trading. |
| `Order Volume` | Volume de ordem a mercado. |

## Gestão de risco

Quando uma nova posição é aberta, a estratégia calcula os níveis de stop-loss e take-profit usando o `PriceStep` do instrumento. Se o instrumento não fornecer um passo, os stops são desabilitados automaticamente. O trailing não é realizado; cada posição mantém os níveis de proteção iniciais até ser fechada por um sinal ou ao atingir um alvo.

## Notas de uso

1. Anexar a estratégia a um instrumento que forneça dados de velas para o `Candle Type` selecionado.
2. Escolher o modo do indicador e os parâmetros de suavização de acordo com seu plano de trading.
3. Certificar-se de que o instrumento tem um `PriceStep` válido ao usar distâncias de stop-loss ou take-profit.
4. Opcionalmente restringir o trading a um intervalo de tempo usando `Start Date` e `End Date`.

## Diferenças comparadas com a versão original MT5

* A implementação usa indicadores StockSharp e APIs de estratégia em C# em vez de funções de trading do MetaTrader.
* O gerenciamento de tamanho de lote está simplificado: o volume da ordem é retirado diretamente do parâmetro `Order Volume`.
* Apenas os métodos de suavização fornecidos pelo StockSharp são suportados (Simple, Exponential, Smoothed, LinearWeighted, Jurik). Os modos MT5 não suportados recorrem à suavização Exponencial.
* Os toggles de direção de negociação e o gerenciamento de stop permanecem compatíveis com o comportamento original.

A estratégia está pronta para backtesting no StockSharp Designer, Shell, Runner ou em qualquer aplicação host personalizada do StockSharp.
