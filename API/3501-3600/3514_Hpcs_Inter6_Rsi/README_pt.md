# Estratégia Hpcs Inter6 RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Hpcs Inter6 RSI transporta o MetaTrader especialista `_HPCS_Inter6_MT4_EA_V01_WE` para o StockSharp API de alto nível. O algoritmo observa o Índice de Força Relativa (RSI) em uma série de velas configurável e reage a reversões rápidas em torno dos limites clássicos de 70/30. Sempre que RSI cruza acima de 70, a estratégia muda para uma posição curta, enquanto uma cruz abaixo de 30 muda para uma posição longa. Cada negociação atribui imediatamente níveis simétricos de take-profit e stop-loss medidos em pips.

## Dados e indicadores
- **Fonte da vela** – período de tempo selecionado pelo usuário (padrão 1 hora).
- **Indicador** – Índice de Força Relativa com comprimento configurável (padrão 14). O indicador é recalculado por meio do pipeline de vinculação do indicador StockSharp.

## Lógica de entrada
1. A estratégia espera por uma vela finalizada para evitar negociar com dados incompletos.
2. Em cada vela concluída, ele compara o novo valor RSI com o valor anterior.
3. **Configuração curta:** se RSI acabou de cruzar acima de `UpperLevel` (padrão 70) de baixo, a estratégia vende usando uma ordem de mercado. A exposição longa existente é fechada antes que a posição curta seja estabelecida, de modo que a posição líquida resultante seja vendida exatamente no volume configurado.
4. **Configuração longa:** se RSI acabou de cruzar abaixo de `LowerLevel` (padrão 30) de cima, a estratégia compra usando uma ordem de mercado. As posições vendidas existentes são cobertas primeiro para que a posição líquida se torne comprada de acordo com o volume configurado.
5. É permitida apenas uma entrada por vela. Vários sinais dentro da mesma barra são ignorados para espelhar a implementação MetaTrader que usa a proteção de carimbo de data/hora da barra.

## Lógica de saída
- Cada entrada define um alvo fixo e para na mesma distância medida em pips.
- Enquanto estiver em uma posição comprada, a estratégia será encerrada se a máxima da vela tocar o alvo ou se a mínima tocar o stop de proteção.
- Enquanto estiver em uma posição curta, a estratégia cobre se a mínima da vela atingir a meta ou se a máxima atingir o stop de proteção.
- Quando a posição é plana, todos os níveis de proteção são apagados.

A distância do pip é traduzida em unidades de preço usando o tamanho do tick do instrumento. Para instrumentos com três ou cinco casas decimais, o algoritmo multiplica a distância por dez para corresponder à noção MetaTrader de um pip.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | Período de 1 hora | Período que alimenta o indicador RSI. |
| `RsiLength` | 14 | Período de lookback de RSI. |
| `UpperLevel` | 70 | Nível RSI que aciona entradas curtas quando cruzadas por baixo. |
| `LowerLevel` | 30 | Nível RSI que aciona entradas longas quando cruzado de cima. |
| `TradeVolume` | 1 | Tamanho do pedido para entradas no mercado. A exposição existente é fechada antes de reverter. |
| `OffsetInPips` | 10 | Distância entre o take-profit e o stop-loss do preço de entrada, expressa em pips. |

Todos os parâmetros são expostos através de objetos `StrategyParam` para que possam ser otimizados dentro de StockSharp.

## Notas
- A estratégia depende da alta/baixa da vela para simular preenchimentos de take-profit e stop-loss, correspondendo ao comportamento das metas de preço fixo em MetaTrader.
- Nenhuma ordem pendente é colocada; todas as execuções são ordens de mercado tratadas pelo núcleo da estratégia.
- As ligações do indicador e do gráfico são criadas automaticamente quando uma área do gráfico está disponível, fornecendo uma sobreposição visual de velas e RSI.
