# Estratégia de Fusão de Indicadores para Trading Intradiário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera velas de 5 minutos usando Parabolic SAR, MACD (12,26,9), Stochastic Oscillator (5,3,3) e Momentum (14). Requer que todos os indicadores estejam alinhados antes de entrar em uma posição.

- **Entrada comprada**: SAR abaixo do preço com SAR anterior acima do atual, Momentum < 100, linha MACD abaixo da linha de sinal, Stochastic %K < 35.
- **Entrada vendida**: SAR acima do preço com SAR anterior abaixo do atual, Momentum > 100, linha MACD acima da linha de sinal, Stochastic %K > 60.

As posições são fechadas quando ocorrem as condições opostas. O gerenciamento de risco usa um trailing stop e take profit opcional.

## Parâmetros
- **Volume** – volume da ordem.
- **Take Profit** – lucro alvo em pontos.
- **Trailing Stop** – distância do trailing stop em pontos.
- **Candle Type** – tipo de subscrição de velas (padrão: 5 minutos).
