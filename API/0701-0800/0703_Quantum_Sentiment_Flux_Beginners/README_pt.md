# Estratégia Quantum Sentiment Flux (Iniciante)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprada quando a EMA rápida cruza acima da EMA lenta e a diferença entre elas supera um limiar baseado em ATR. Entra vendida no sinal oposto. As posições são encerradas quando o preço se move um múltiplo de ATR contra a operação ou atinge uma meta de lucro de dois múltiplos de ATR. Um período de resfriamento limita a frequência de operações.

## Parâmetros
- Tipo de vela
- Comprimento da EMA rápida
- Comprimento da EMA lenta
- Período ATR
- Multiplicador ATR
- Limiar de força da MA
- Barras de resfriamento
- Quantidade
