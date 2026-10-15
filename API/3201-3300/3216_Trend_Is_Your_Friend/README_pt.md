# Estratégia de Trend Is Your Friend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Trend Is Your Friend é um sistema de seguimento de tendência multi-período inspirado no consultor especialista MetaTrader original. Alinha o momentum intradiário com um filtro MACD de período superior, enquanto o risco é gerenciado através de saídas de Bandas de Bollinger, alvos clássicos de stop-loss e take-profit, um bloqueio de break-even opcional e gerenciamento de trailing stop.

A estratégia trabalha em um período base configurável (padrão: 1 hora) e analisa a estrutura de velas para um padrão de momentum de curto prazo: uma vela baixista seguida de uma vela altista mais forte para negociações longas, ou o inverso para negociações curtas. Esses padrões devem concordar com um filtro de tendência de média móvel e um sinal MACD mensal antes de um posição ser aberta.

## Lógica de entrada
1. Calcular uma EMA rápida e uma LWMA lenta no período de entrada.
2. Rastrear as últimas duas velas concluídas para formar um padrão de momentum:
   - **Setup longo:** a vela de duas barras atrás é baixista, a vela anterior é altista e de maior magnitude.
   - **Setup curto:** a vela de duas barras atrás é altista, a vela anterior é baixista e de menor magnitude.
3. Confirmar o setup com o filtro de tendência de média móvel (MA rápida acima da MA lenta para negociações longas, abaixo para curtas).
4. Confirmar a tendência de longo prazo com um sinal MACD calculado no período superior (padrão: mensal). A linha MACD deve estar acima da linha de sinal para negociações longas e abaixo para curtas.
5. Quando todos os filtros se alinham, abrir uma posição a mercado com o volume configurado.

## Lógica de saída
- **Saída por Bandas de Bollinger:** posições longas são fechadas quando o preço fecha acima da banda superior; posições curtas quando o preço fecha abaixo da banda inferior.
- **Take-profit / stop-loss:** distâncias fixas opcionais medidas em pips. A implementação converte pips para distância de preço via o passo de preço do ativo.
- **Break-even:** opcional, move o stop de proteção para (ou além do) preço de entrada após um limiar de lucro configurável ter sido atingido.
- **Trailing stop:** opcional, é ativado após um limiar de lucro e segue o preço por uma distância fixa de pip. O trailing stop compartilha o mesmo armazenamento com o nível de break-even.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| Entry Candle | Tipo de vela para lógica de entrada | 1 hora |
| MACD Candle | Período superior usado para filtro MACD | 30 dias |
| Fast MA | Comprimento da EMA rápida | 8 |
| Slow MA | Comprimento da LWMA lenta | 20 |
| Bollinger Length | Período das Bandas de Bollinger | 20 |
| Bollinger Width | Multiplicador de desvio padrão das Bandas de Bollinger | 2.0 |
| Stop Loss (pips) | Distância de stop de proteção | 20 |
| Take Profit (pips) | Distância do alvo de lucro | 50 |
| Use Break-Even | Habilitar ajuste de break-even | true |
| Break-Even Trigger | Lucro (pips) necessário para mover o stop | 10 |
| Break-Even Offset | Offset aplicado ao stop de break-even | 5 |
| Use Trailing | Habilitar trailing stop | true |
| Trailing Activation | Lucro (pips) necessário para ativar o trailing | 40 |
| Trailing Distance | Distância (pips) mantida pelo trailing stop | 40 |

## Notas
- A estratégia armazena apenas as duas últimas velas concluídas para evitar buffers históricos pesados.
- Os dados MACD são subscritos do período superior configurado com agregação habilitada, permitindo que sinais mensais sejam construídos a partir de dados diários quando necessário.
- A conversão de pip para preço usa o passo de preço do ativo. Instrumentos com definições de pip não-padrão podem requerer ajuste de parâmetros.
