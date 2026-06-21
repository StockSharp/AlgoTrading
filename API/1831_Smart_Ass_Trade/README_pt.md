# Estratégia Smart Ass Trade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Smart Ass Trade é uma estratégia de seguimento de tendência multiperíodo convertida da implementação MQL.
Ela analisa o histograma MACD (OsMA) e médias móveis simples de 20 períodos nos gráficos de 5, 15 e 30 minutos.
Um filtro diário de Williams %R bloqueia negociações em condições de sobrecompra ou sobrevenda.

## Algoritmo
1. Calcular histograma MACD e SMA(20) nos períodos de 5m, 15m e 30m.
2. Definir tendência de alta quando o histograma cresce e a SMA sobe em todos os três períodos.
3. Definir tendência de baixa quando o histograma cai e a SMA desce em todos os três períodos.
4. Usar Williams %R diário (período 26) para evitar comprar acima de -2 ou vender abaixo de -98.
5. Quando todas as condições se alinham, abrir uma ordem a mercado na direção correspondente.
6. O tamanho da posição pode ser fixo ou otimizado com base no valor da conta.

## Parâmetros
- **Hedging** – permite abrir posições opostas simultaneamente.
- **LotsOptimization** – ativa o cálculo dinâmico de lotes.
- **Lots** – volume de negociação fixo quando a otimização está desativada.
- **AutomaticTakeProfit** – marcador para take profit dinâmico, atualmente não usado.
- **MinimumTakeProfit** – alvo de lucro em pontos para modo manual.
- **AutomaticStopLoss** – marcador para stop loss dinâmico, atualmente não usado.
- **StopLoss** – stop loss em pontos para modo manual.
- **CandleType** – período base para assinaturas (padrão 5 minutos).

## Notas
A estratégia usa a API de alto nível com chamadas `SubscribeCandles` e `Bind`.
Os valores de take profit e stop loss foram deixados para extensão futura; a versão atual foca em
geração de sinais e execução de ordens.
