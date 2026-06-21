# Estratégia I-Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia I-Trend** é um algoritmo de trading seguidor de tendência convertido do especialista MQL5 original `Exp_i_Trend`. Combina uma média móvel com Bandas de Bollinger para identificar mudanças de momentum. A estratégia calcula um valor personalizado *iTrend* e uma linha de sinal correspondente, e abre ou fecha posições quando ocorrem cruzamentos.

## Como Funciona

1. **Configuração de Indicadores**
   - Calcula uma Média Móvel Exponencial (EMA) com período configurável.
   - Constrói Bandas de Bollinger usando o mesmo período e parâmetros de desvio.
   - Deriva o valor *iTrend* como a diferença entre o preço escolhido e a linha de Banda de Bollinger selecionada (superior, inferior ou média).
   - Calcula uma linha de sinal como `2 * MA - (High + Low)`.
2. **Geração de Sinais**
   - Quando o iTrend cruza **acima** da linha de sinal, a estratégia fecha posições vendidas e abre uma posição comprada.
   - Quando o iTrend cruza **abaixo** da linha de sinal, a estratégia fecha posições compradas e abre uma posição vendida.
3. **Execução de Ordens**
   - Entradas e saídas são executadas a preço de mercado.
   - O tamanho da posição é definido pelo parâmetro de estratégia `Volume`.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `MaPeriod` | Período da média móvel usada nos cálculos. |
| `BbPeriod` | Período das Bandas de Bollinger. |
| `BbDeviation` | Desvio padrão para as Bandas de Bollinger. |
| `PriceType` | Tipo de preço usado para calcular o valor iTrend (Close, Open, High, Low, Median, Typical, etc.). |
| `BbMode` | Seleciona qual linha de Banda de Bollinger é usada (Upper, Lower, Middle). |
| `CandleType` | Período de velas fornecidas à estratégia. |
| `Volume` | Volume de ordens para entradas. |

## Notas

- A estratégia trabalha apenas com velas concluídas; velas inacabadas são ignoradas.
- Foi projetada para fins educacionais e pode exigir ajustes para trading ao vivo.
