# Estratégia de Momentum Color AMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o Consultor Especialista MetaTrader *Exp_ColorMomentum_AMA* para StockSharp.
Ela calcula o momentum do preço ao longo de um período configurável e o suaviza com a Média Móvel Adaptativa de Kaufman (AMA).
Os sinais de negociação são gerados quando o momentum suavizado mostra duas subidas ou quedas consecutivas.

## Lógica
- **Entrada comprada**: O Momentum AMA sobe por duas barras seguidas. Qualquer posição vendida existente é fechada antes de abrir uma nova posição comprada.
- **Entrada vendida**: O Momentum AMA cai por duas barras seguidas. Qualquer posição comprada existente é fechada antes de abrir uma nova posição vendida.
- Sinais opostos fecham as posições atuais.

## Parâmetros
- Tipo de vela
- Período de momentum
- Período AMA
- Período rápido
- Período lento
- Barra de sinal
