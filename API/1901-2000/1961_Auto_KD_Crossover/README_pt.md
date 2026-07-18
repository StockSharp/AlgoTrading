# Estratégia de Cruzamento Auto KD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de Cruzamento Auto KD replica o exemplo MQL5 original `autoKD_EA`.  
Usa o indicador `StochasticOscillator` para gerar sinais de compra e venda com base em cruzamentos das linhas %K e %D.

O cálculo base usa a fórmula RSV:
`RSV = (Close - LowestLow) / (HighestHigh - LowestLow) * 100`
onde a máxima mais alta e a mínima mais baixa são calculadas sobre barras `KDPeriod`.  
A linha %K é uma média móvel do RSV com comprimento `KPeriod`; %D é uma média móvel de %K com comprimento `DPeriod`.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-----------|--------|
| `KDPeriod` | Número de barras para o período base do RSV. | 30 |
| `KPeriod` | Período de suavização para a linha %K. | 3 |
| `DPeriod` | Período de suavização para a linha %D. | 6 |
| `CandleType` | Tipo e período de velas usadas para cálculos. | Período de 5 minutos |
| `Volume` | Volume de ordem herdado de `Strategy`. | `Strategy.Volume` |

Todos os parâmetros estão disponíveis para otimização.

## Lógica de Trading
1. Inscrever-se na série de velas selecionada e calcular o oscilador Estocástico.
2. Quando o valor anterior de %K estava abaixo de %D e o %K atual cruza acima de %D, uma posição comprada é aberta.
3. Quando o valor anterior de %K estava acima de %D e o %K atual cruza abaixo de %D, uma posição vendida é aberta.
4. A estratégia mantém apenas uma posição por vez. Cruzamentos na direção oposta fecham a posição e abrem o lado oposto.
5. `StartProtection()` habilita os mecanismos de proteção de perda/lucro padrão fornecidos pelo StockSharp.

## Visualização
A estratégia exibe automaticamente velas, o indicador Estocástico e trades executados no gráfico.

## Notas
- Funciona com qualquer instrumento e período.
- Os parâmetros devem ser adaptados à volatilidade do mercado selecionado.
