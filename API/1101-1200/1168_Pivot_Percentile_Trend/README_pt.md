# Estratégia Pivot Percentile Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula a força de tendência baseada em percentis em múltiplos períodos de retrocesso e a combina com um filtro SuperTrend. Operações compradas ocorrem quando a força da tendência é positiva e o preço está acima da linha SuperTrend. Operações vendidas são realizadas quando a força da tendência é negativa e o preço está abaixo do SuperTrend.

## Detalhes

- **Indicadores**: análise de percentis, SuperTrend
- **Direção**: Ambos
- **Período**: Qualquer

## Cálculo deste exemplo

Sete janelas `PercentileLength * 1..7` usam quartis High/Low (75%/25%, posição mais próxima). Cada janela soma +1 quando o High atual está estritamente acima do quartil superior e -1 quando o Low está estritamente abaixo do inferior. A vela atual pertence a todas as janelas.

A soma define a força da tendência. Força positiva e Close acima do SuperTrend de preço abrem compra; força negativa e Close abaixo abrem venda. Uma posição oposta é totalmente invertida até `Volume`. Força neutra ou rejeição pelo filtro mantêm a posição. Todas as janelas precisam estar completas; não há intervalo obrigatório entre sinais nem stop de proteção.

Padrões: `PercentileLength=40`, `AtrPeriod=10`, `AtrMultiplier=3`, `CandleType=5m`. SuperTrend usa ATR de Wilder e as bandas de preço da plataforma.
