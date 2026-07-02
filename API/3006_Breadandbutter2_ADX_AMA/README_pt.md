# Estratégia Bread and Butter 2 (ADX + AMA)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma portabilidade do consultor especialista MetaTrader 5 *Breadandbutter2* criado por Ron Thompson. A lógica original aguarda uma barra nova, compara o valor mais recente do Average Directional Index (ADX) com o anterior, e verifica se a Kaufman Adaptive Moving Average (KAMA, também conhecida como AMA) está subindo ou descendo. Uma posição comprada é aberta quando a força da tendência enfraquece enquanto o momentum do preço melhora, enquanto uma posição vendida é aberta quando a força da tendência aumenta enquanto o momentum se deteriora. A versão StockSharp mantém o comportamento de fechar qualquer exposição contrária antes de abrir uma nova ordem, e aplica as mesmas distâncias fixas de stop-loss e take-profit que foram especificadas em pips no script original.

## Indicadores
- **Average Directional Index (ADX)** – mede a força da tendência atual. A estratégia observa a linha principal do ADX e compara os últimos dois valores para determinar se a força da tendência está aumentando ou diminuindo.
- **Kaufman Adaptive Moving Average (KAMA/AMA)** – adapta-se à volatilidade do mercado usando constantes de suavização rápida e lenta separadas. A estratégia compara os últimos dois valores para avaliar a direção do momentum.

## Lógica da estratégia
1. Trabalhar com o tipo de vela configurado (padrão: barras de 1 hora) e aguardar até que uma vela esteja completamente fechada antes de processar.
2. Calcular KAMA com o comprimento selecionado, período rápido e período lento.
3. Calcular ADX com o período de média configurado e extrair o valor da linha principal.
4. Comparar as leituras atuais e anteriores do indicador:
   - **Configuração comprada** – o valor ADX diminui (a força da tendência enfraquece) enquanto KAMA sobe (o momentum do preço melhora).
   - **Configuração vendida** – o valor ADX aumenta enquanto KAMA cai.
5. Quando um sinal aparece, fechar qualquer exposição do lado contrário e abrir uma nova ordem de mercado para que a posição final corresponda ao volume base da estratégia.
6. Monitorar continuamente a posição ativa. Se o preço tocar os níveis de stop-loss ou take-profit configurados (expressos em pips e convertidos para unidades de preço de acordo com o tamanho do tick do instrumento), sair da operação imediatamente.

## Gestão de operações
- **Stop-loss** – expresso em pips; convertido para unidades de preço usando o `PriceStep` do instrumento. Para símbolos cotados com 3 ou 5 decimais, o tamanho do pip é 10 vezes o passo de preço, correspondendo à implementação do MetaTrader.
- **Take-profit** – também expresso em pips e tratado da mesma forma que a distância do stop-loss.
- A estratégia usa ordens de mercado para entradas e saídas e inverte a posição quando ocorre um sinal contrário.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Tipo de vela usado para todos os cálculos. |
| `AdxPeriod` | `14` | Comprimento de média da linha principal do ADX. |
| `AmaPeriod` | `9` | Período base da Kaufman Adaptive Moving Average. |
| `AmaFastPeriod` | `2` | Período EMA rápido usado dentro do AMA. |
| `AmaSlowPeriod` | `30` | Período EMA lento usado dentro do AMA. |
| `StopLossPips` | `50` | Distância ao stop-loss de proteção em pips. Definir como `0` para desabilitar. |
| `TakeProfitPips` | `50` | Distância ao objetivo de lucro em pips. Definir como `0` para desabilitar. |

## Notas de uso
- Garantir que a estratégia esteja vinculada a um instrumento que exponha um `PriceStep` válido. Para símbolos forex com pips fracionários, o tamanho do pip é calculado automaticamente.
- `Volume` controla o tamanho base da ordem. Quando um sinal de reversão aparece, o algoritmo adiciona volume suficiente para fechar qualquer exposição contrária e estabelecer uma posição igual a `Volume` na nova direção.
- Como as saídas de stop-loss e take-profit são avaliadas nos máximos e mínimos das velas, o comportamento aproxima a execução de ordens pendentes do MetaTrader.

## Referências
- Estratégia original do MetaTrader 5: `MQL/22003/Breadandbutter2.mq5`
- Indicadores StockSharp: `KaufmanAdaptiveMovingAverage`, `AverageDirectionalIndex`
