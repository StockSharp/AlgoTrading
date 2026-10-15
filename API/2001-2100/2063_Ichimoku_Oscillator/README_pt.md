# Estratégia de Oscilador Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Ichimoku Oscillator** utiliza um oscilador personalizado derivado do indicador Ichimoku. O oscilador é definido como a diferença entre a linha de atraso e Senkou Span B menos a diferença entre Tenkan-sen e Kijun-sen. O valor resultante é suavizado com uma média móvel Jurik.

A estratégia entra em posições quando este oscilador suavizado muda de direção e cruza seu valor anterior, tentando capturar tendências emergentes.

## Como funciona
- **Entrada Comprado**: O oscilador sobe e o valor atual cruza acima do valor anterior. Qualquer posição vendida é fechada antes de abrir a comprada.
- **Entrada Vendido**: O oscilador cai e o valor atual cruza abaixo do valor anterior. Qualquer posição comprada é fechada antes de abrir a vendida.
- Stop loss e take profit opcionais em percentagem são aplicados para gestão de risco.

## Parâmetros
- **Tenkan Period** – Período Tenkan-sen do indicador Ichimoku.
- **Kijun Period** – Período Kijun-sen do indicador Ichimoku.
- **Senkou Span B Period** – Período Senkou Span B do indicador Ichimoku.
- **Smoothing Period** – Período para o suavizamento com a média móvel Jurik do oscilador.
- **Candle Type** – Período utilizado para os cálculos.
- **Stop Loss %** – Stop loss expresso em percentagem.
- **Enable Stop Loss** – Ativa ou desativa a proteção de stop loss.
- **Take Profit %** – Take profit expresso em percentagem.

## Indicadores
- Ichimoku
- Jurik Moving Average

## Notas
Esta estratégia destina-se a fins educativos e deve ser testada em dados históricos antes de operar em tempo real.
