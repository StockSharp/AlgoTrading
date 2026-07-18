# Estratégia de Nível DVD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma tradução simplificada do consultor especialista MQL5 original "DVD Level". Emprega o Range Action Verification Index (RAVI) para determinar a direção do mercado. O RAVI é calculado usando médias móveis exponenciais de 2 e 24 períodos em velas de 1 hora.

## Parâmetros
- `Volume` – volume da ordem usado para as negociações.

## Lógica
1. Assinar velas de 1 hora e calcular EMA(2) e EMA(24).
2. Calcular `RAVI = (EMA2 - EMA24) / EMA24 * 100`.
3. Se o RAVI cruzar abaixo de zero, a estratégia compra se estiver flat ou vendida.
4. Se o RAVI cruzar acima de zero, a estratégia vende se estiver flat ou comprada.
5. A proteção de posição integrada é ativada via `StartProtection()`.

A abordagem captura potenciais reversões quando o momentum de curto prazo diverge da tendência de longo prazo.
