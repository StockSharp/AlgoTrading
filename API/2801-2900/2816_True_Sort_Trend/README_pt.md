# Estratégia True Sort Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o clássico template "True Sort" do MetaTrader aguardando que cinco médias móveis exponenciais se alinhem em ordem estrita. Quando tanto a vela atual quanto a anterior completadas respeitam o mesmo ordenamento de alta ou baixa e o Average Directional Index (ADX) confirma o momentum, a estratégia abre uma posição na direção da tendência. O risco é controlado por distâncias opcionais absolutas de stop-loss e take-profit junto com um trailing stop que só é ativado após o preço se mover o suficiente a favor da operação.

## Como funciona

1. Construir cinco EMAs (de rápida a lenta: padrão 10, 20, 50, 100, 200 períodos) na série de velas selecionada.
2. Calcular o ADX com um período configurável (padrão 24) para qualificar se a tendência tem força suficiente (limiar padrão 20).
3. Apenas no momento em que uma vela fecha analisamos os indicadores. Os sinais são ignorados para velas não finalizadas para evitar decisões prematuras.
4. Um setup comprado requer o seguinte para a vela completada **atual** e a **anterior**:
   - `EMA_rápida > EMA_2 > EMA_3 > EMA_4 > EMA_lenta` (empilhamento de alta perfeito).
   - `ADX > limiar` para garantir que a inclinação é significativa.
5. Um setup vendido espelha o acima com todas as desigualdades invertidas.
6. As posições são fechadas quando o empilhamento ordenado se quebra, quando os níveis de proteção são atingidos, ou quando o trailing stop devolve uma quantidade configurável de lucro.

Esta lógica mantém a estratégia estritamente em mercados com forte tendência e força o alinhamento em duas barras para reduzir o ruído.

## Regras de trading

- **Entrada**
  - **Comprado**: ADX maior que o limiar e cinco EMAs ordenadas da mais rápida para a mais lenta para tanto a vela atual quanto a anterior finalizada. Qualquer posição vendida aberta é fechada primeiro, então um novo comprado é aberto com o `Volume` configurado.
  - **Vendido**: ADX maior que o limiar e EMAs ordenadas em ordem descendente por duas velas consecutivas. Qualquer posição comprada é achatada antes que a entrada vendida seja enviada.
- **Saída**
  - Se o empilhamento de EMA perder sua ordenação estrita, a posição é imediatamente fechada.
  - Saídas de proteção opcionais:
    - Distância de stop-loss em unidades de preço absoluto abaixo (comprado) ou acima (vendido) do preço de entrada.
    - Distância de take-profit em unidades de preço absoluto além do preço de entrada.
    - Trailing stop que se aciona somente após o preço avançar `TrailingStopDistance + TrailingStepDistance` e então segue o preço em `TrailingStopDistance`.
  - Fechamentos manuais ou execuções externas também resetarão o estado interno.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Tipo de dados das velas usadas para todos os cálculos. | Período de 1 hora |
| `FastEmaLength` | Período da EMA mais rápida (alinhamento de entrada). | 10 |
| `SecondEmaLength` | Período da segunda EMA. | 20 |
| `ThirdEmaLength` | Período da terceira EMA. | 50 |
| `FourthEmaLength` | Período da quarta EMA. | 100 |
| `SlowEmaLength` | Período da EMA mais lenta que representa a tendência de longo prazo. | 200 |
| `AdxPeriod` | Comprimento de média para o indicador ADX. | 24 |
| `AdxThreshold` | Valor mínimo de ADX necessário para permitir trades. | 20 |
| `StopLossDistance` | Distância de preço absoluta do stop protetor (0 desabilita). | 0.005 |
| `TakeProfitDistance` | Distância de preço absoluta da meta de lucro (0 desabilita). | 0.015 |
| `TrailingStopDistance` | Distância entre o preço mais alto/baixo e a saída por trailing. | 0.0005 |
| `TrailingStepDistance` | Avanço extra necessário antes que o trailing stop seja ativado ou movido. | 0.0001 |

Todos os valores de distância são expressos em unidades de preço. Para símbolos FX cotados com quatro ou cinco decimais, valores como `0.005` correspondem aproximadamente a 50 pips. Ajuste os números para corresponder ao tamanho do tick do instrumento negociado.

## Notas e dicas

- Funciona melhor em instrumentos com tendência, como os principais pares de FX ou índices em períodos intradiários ou swing. Aumente os comprimentos de EMA para barras diárias ou encurte-os para scalping.
- A confirmação de duas velas reduz drasticamente os whipsaws, mas pode causar entradas tardias. Considere otimizar o limiar de ADX e os comprimentos de EMA para seu mercado.
- Os trailing stops permanecem inativos até que o preço se mova `TrailingStopDistance + TrailingStepDistance` a partir da entrada. Definir o passo como zero imita o comportamento do MetaTrader onde o trailing começa assim que o preço percorre a distância base.
- A estratégia depende de ordens a mercado (`BuyMarket`, `SellMarket`). Configure a propriedade `Volume` da instância de estratégia para controlar o dimensionamento de posição ou integrar com o gerenciamento de dinheiro do portfólio se necessário.
- Combine com filtros de sessão ou confirmação de período de tempo superior se precisar limitar os horários de trading.
