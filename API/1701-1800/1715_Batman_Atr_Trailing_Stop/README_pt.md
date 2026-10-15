# Estratégia Batman ATR com Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa uma abordagem de stop trailing baseada em ATR inspirada no Expert Advisor original "Batman".
Ela rastreia níveis dinâmicos de suporte e resistência derivados do indicador **Average True Range (ATR)** e reage quando o preço cruza esses níveis.

## Lógica

1. Calcular o ATR com o período configurável.
2. Determinar suporte e resistência:
   - `support = price - ATR * factor`
   - `resistance = price + ATR * factor`
3. Manter o suporte ou resistência mais próximo dependendo da tendência atual.
4. Quando o preço rompe acima da resistência, abrir uma posição **comprada**.
5. Quando o preço cai abaixo do suporte, abrir uma posição **vendida**.

O preço pode ser o preço de fechamento ou o preço típico `(high + low + close) / 3`.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `ATR Period` | Período do indicador ATR. |
| `ATR Factor` | Multiplicador aplicado ao valor ATR para construir as linhas de stop. |
| `Use Typical Price` | Se habilitado, usa `(High + Low + Close)/3` em vez do preço de fechamento. |
| `Candle Type` | Tipo de velas usadas para os cálculos. |

## Notas

- A estratégia usa a API de alto nível com `SubscribeCandles` e `Bind`.
- `StartProtection()` é chamado no início para garantir a segurança da posição.
- A negociação é realizada apenas em velas concluídas.
