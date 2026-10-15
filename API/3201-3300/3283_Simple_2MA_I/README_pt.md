# Estratégia Simple 2 MA I
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Simple 2 MA I é uma estratégia de seguimento de tendência que replica a lógica central do expert advisor original do MetaTrader. Ela usa um par de médias móveis ponderadas lineares (LWMAs) calculadas sobre preços típicos para identificar a tendência dominante. Confirmação de momentum e filtros de direção MACD removem sinais fracos. A estratégia opcionalmente gerencia risco por meio de stop-loss automático, take-profit, movimentos para break-even e trailing stops baseados em candles.

## Lógica de negociação

### Configuração comprada

1. A LWMA rápida está acima da LWMA lenta, confirmando uma tendência de alta.
2. A mínima do candle de duas barras atrás está abaixo da máxima da barra anterior, sinalizando estrutura altista recente.
3. Pelo menos uma das três últimas leituras de taxa de variação está acima do limite de momentum configurado.
4. A linha MACD está acima da linha de sinal.
5. O volume líquido da posição é menor que o limite `Max Net Volume`.

Quando todas as condições são atendidas, a estratégia fecha exposição vendida (se houver) e compra a mercado.

### Configuração vendida

1. A LWMA rápida está abaixo da LWMA lenta, confirmando uma tendência de baixa.
2. A mínima da barra anterior está abaixo da máxima da barra de dois períodos atrás, indicando estrutura baixista.
3. Pelo menos uma das três últimas leituras de taxa de variação está acima do limite de momentum (valor absoluto).
4. A linha MACD está abaixo da linha de sinal.
5. O volume líquido da posição é menor que `Max Net Volume`.

Quando as condições se mantêm, a estratégia cobre compradas (se houver) e vende a mercado.

### Gestão de risco

* **Stop-loss / take-profit:** distâncias fixas opcionais definidas em pontos relativos ao preço de entrada.
* **Break-even:** quando o preço atinge a distância de gatilho em lucro, o stop é movido para entrada ± offset.
* **Trailing por candle:** depois que a distância de ativação é alcançada, o stop segue extremos de candles com um buffer configurável.
* Ordens de proteção são canceladas automaticamente quando a posição é fechada.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | --------- | ------ |
| Candle Type | Período usado para cálculos dos indicadores. | Candles de 15 minutos |
| Fast LWMA | Período da LWMA rápida. | 6 |
| Slow LWMA | Período da LWMA lenta. | 85 |
| Momentum Length | Período de retrospectiva do indicador de taxa de variação. | 14 |
| Momentum Threshold | Valor absoluto mínimo de taxa de variação exigido. | 0.3 |
| MACD Fast | Comprimento da EMA rápida usada no MACD. | 12 |
| MACD Slow | Comprimento da EMA lenta usada no MACD. | 26 |
| MACD Signal | Comprimento da EMA de sinal usada no MACD. | 9 |
| Use Stop-Loss | Habilita a colocação de ordens stop-loss. | true |
| Stop-Loss (points) | Distância do preço de entrada até o stop-loss. | 20 |
| Use Take-Profit | Habilita a colocação de ordens take-profit. | true |
| Take-Profit (points) | Distância do preço de entrada até o take-profit. | 50 |
| Use Break-Even | Habilita o movimento automático para break-even. | true |
| Break-Even Trigger | Lucro (pontos) necessário antes do break-even. | 30 |
| Break-Even Offset | Offset (pontos) adicionado ao mover para break-even. | 30 |
| Use Candle Trailing | Habilita trailing stops baseados em extremos de candles. | true |
| Trailing Activation | Lucro (pontos) exigido antes da ativação do trailing. | 40 |
| Trailing Padding | Distância extra (pontos) adicionada ao extremo do candle. | 10 |
| Max Net Volume | Volume líquido absoluto máximo permitido. | 1 |

## Observações

* Todas as distâncias de preço são expressas em passos de preço do ativo (pontos). A estratégia multiplica automaticamente valores de parâmetros pelo tamanho do tick do ativo.
* O mapeamento padrão de períodos segue os padrões do expert original, mas pode ser ajustado livremente.
* A estratégia espera candles concluídos. Barras não finalizadas são ignoradas para permanecer consistente com o EA de origem.
