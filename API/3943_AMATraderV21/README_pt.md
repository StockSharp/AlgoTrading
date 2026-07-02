# Estratégia AMA Trader v2.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia AMA Trader v2.1 é uma conversão do consultor especialista MetaTrader 4 **AMA_TRADER_v2_1.mq4** que combina rajadas de média móvel adaptativa (AMA) de Kaufman com um filtro Heiken Ashi com suavização dupla e verificações de impulso RSI.

## Lógica principal

1. **Filtro de tendência adaptativo** – Um mecanismo AMA personalizado reproduz o indicador original, incluindo as constantes rápidas/lentas, a taxa de eficiência e o parâmetro de potência. O algoritmo observa picos de impulso onde o valor AMA salta mais de `AmaThreshold` etapas de preço em comparação com a barra anterior.
2. **Confirmação de Heiken Ashi** – As velas de preço são suavizadas duas vezes: primeiro por uma média móvel configurável nos preços brutos OHLC, depois por uma segunda média móvel nos buffers de Heiken Ashi. Uma barra suavizada de alta (fechada acima da aberta) permite negociações longas, enquanto uma barra de baixa permite vendas curtas.
3. **RSI Verificação de Momentum** – Um RSI clássico com período configurável confirma o momentum: os longos exigem que o RSI recue de um valor anterior enquanto permanece abaixo de 70, os vendidos exigem um salto enquanto o oscilador permanece acima de 30.
4. **Gerenciamento de posição** – A estratégia abre uma única posição por vez, aplica distâncias opcionais de stop-loss e take-profit (em etapas de preço) e pode rastrear o stop quando o preço se move na direção da negociação. Quando RSI cruza os extremos 70/30, um fechamento parcial opcional é executado antes que ocorra uma saída completa no próximo cruzamento.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Velas de 15 minutos | Prazo para todos os cálculos. |
| `TradeVolume` | 0,1 | Volume base de ordens de mercado. |
| `AmaLength` | 9 | Lookback usado pela média móvel adaptativa. |
| `AmaFastPeriod` | 2 | Constante rápida em barras para suavização AMA. |
| `AmaSlowPeriod` | 30 | Constante lenta em barras para suavização AMA. |
| `AmaPower` | 2 | Expoente aplicado à constante de suavização (corresponde a `G` no código MQ4). |
| `AmaThreshold` | 2 etapas | Alteração mínima da AMA (em etapas de preço) para acionar um sinal. |
| `FirstMaMethod` | Suavizado | Primeiro método de suavização para construção Heiken Ashi. |
| `FirstMaPeriod` | 6 | Comprimento da primeira média móvel de suavização. |
| `SecondMaMethod` | Linear Ponderado | Segundo método de suavização aplicado aos buffers Heiken Ashi. |
| `SecondMaPeriod` | 2 | Comprimento da segunda média móvel de suavização. |
| `RsiPeriod` | 14 | RSI período usado pelo filtro de impulso. |
| `PartialClosePercent` | 70% | Parte da posição ativa a ser fechada quando RSI cruza um extremo. Defina como `0` para desativar. |
| `StopLossSteps` | 50 | Distância de stop-loss expressa em etapas de preço do instrumento. Defina como `0` para desativar. |
| `TakeProfitSteps` | 100 | Distância de lucro expressa em etapas de preço. Defina como `0` para desativar. |
| `TrailingSteps` | 30 | Distância do trailing stop em etapas de preço. Defina como `0` para desativar o rastreamento. |

## Regras de negociação

- **Entrada longa** – Quando o salto AMA é positivo e excede `AmaThreshold`, a última vela Heiken Ashi suavizada é de alta e RSI está recuando (valor anterior maior que o valor atual) enquanto permanece em ou abaixo de 70.
- **Entrada curta** – Quando o salto da AMA é negativo além de `AmaThreshold`, a vela Heiken Ashi suavizada é de baixa e RSI está subindo (valor anterior menor que o atual) enquanto permanece em 30 ou acima.
- **Fechamento parcial** – Se ativado, fecha `PartialClosePercent` da posição quando RSI cruza acima de 70 (comprados) ou abaixo de 30 (vendidos).
- **Saída Total** – Feche a posição inteira no extremo oposto RSI, em stop-loss, take-profit ou quando o trailing stop for atingido.

A implementação usa o StockSharp API de alto nível: uma assinatura de vela alimenta a calculadora AMA personalizada, o pipeline de suavização Heiken Ashi e o indicador RSI. Todos os comentários no código-fonte estão em inglês, refletindo os requisitos das diretrizes de conversão.
