# Estratégia de Rompimento de Barras de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia detecta reversões de tendência usando o indicador Parabolic SAR. Ela aguarda um número configurável de reversões negativas antes de entrar na nova direção da tendência. As distâncias para o stop-loss e o take-profit são medidas em pips ou como uma porcentagem do preço de entrada.

## Parâmetros

- **Reversal Mode** – escolher entre cálculos de distância baseados em pips ou em porcentagem.
- **Delta** – movimento mínimo de preço necessário entre reversões.
- **Negative Signals** – quantas reversões falhas devem ocorrer antes de uma operação ser aberta.
- **Stop Loss** – distância de proteção de perda a partir do preço de entrada.
- **Take Profit** – distância do alvo de lucro a partir do preço de entrada.
- **Candle Type** – série de velas usada para cálculos do indicador.

## Lógica

1. Subscrever dados de velas e calcular o Parabolic SAR.
2. Quando o Parabolic SAR muda de direção e o preço se move pelo menos *Delta*, armazenar o preço de reversão.
3. Contar reversões negativas onde o preço se moveu contra a tendência anterior.
4. Uma vez que o contador atinge o valor de **Negative Signals**, abrir uma posição na nova direção da tendência.
5. Cada vela verifica os níveis de stop-loss e take-profit usando o **Reversal Mode** selecionado.
6. As posições são fechadas em mudança de tendência oposta ou quando os limites de risco são atingidos.

A estratégia é adequada para sistemas de rompimento de seguidor de tendência e pode ser otimizada ajustando as distâncias de delta, stop-loss e take-profit.
