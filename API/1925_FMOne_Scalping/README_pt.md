# Estratégia de Scalping FmOne
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Scalping FmOne é uma tradução simplificada do expert advisor FMOneEA para MetaTrader 4. A estratégia combina uma média móvel exponencial rápida e uma lenta com o indicador MACD para capturar momentum de curto prazo em qualquer período.

## Como Funciona
1. As EMAs rápida e lenta definem a direção atual da tendência.
2. O histograma MACD confirma o momentum na direção da tendência.
3. Uma ordem de compra é aberta quando a EMA rápida está acima da EMA lenta e o histograma MACD é positivo.
4. Uma ordem de venda é aberta quando a EMA rápida está abaixo da EMA lenta e o histograma MACD é negativo.
5. Cada posição é protegida com níveis de stop-loss e take-profit configuráveis. O trailing stop pode ser ativado para acompanhar movimentos lucrativos.

## Parâmetros
- **FastMaPeriod** – Comprimento da EMA rápida.
- **SlowMaPeriod** – Comprimento da EMA lenta.
- **MacdSignalPeriod** – Período da linha de sinal do indicador MACD.
- **StopLossPercent** – Tamanho do stop-loss em percentual do preço de entrada.
- **TakeProfitPercent** – Tamanho do take-profit em percentual do preço de entrada.
- **EnableTrailingStop** – Ativa o gerenciamento do trailing stop.
- **CandleType** – Período para os candles recebidos.

## Notas
Este port foca na lógica central do EA original. Recursos avançados como ciclos de resgate e automação de break-even da versão MQL são intencionalmente omitidos para manter o exemplo legível.
