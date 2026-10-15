# Estratégia de Gatilho MA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina médias móveis exponenciais (EMA) rápidas e lentas com RSI para detectar reversões de tendência.
Quando a EMA rápida e o RSI rápido estão ambos acima de seus equivalentes lentos, o mercado é tratado como altista e uma posição comprada é aberta.
Quando ambos estão abaixo, uma posição vendida é aberta. Os parâmetros permitem habilitar ou desabilitar entradas ou saídas compradas e vendidas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápida > EMA lenta E RSI rápido > RSI lento com tendência anterior baixista.
  - **Vendido**: EMA rápida < EMA lenta E RSI rápido < RSI lento com tendência anterior altista.
- **Critérios de saída**:
  - **Comprado**: tendência se torna baixista e saídas compradas são permitidas.
  - **Vendido**: tendência se torna altista e saídas vendidas são permitidas.
- **Indicadores**: EMA, RSI.
- **Stops**: Não incluídos.
- **Período**: velas de 4 horas por padrão.
- **Parâmetros**:
  - `FastRsiPeriod` = 3
  - `SlowRsiPeriod` = 13
  - `FastMaPeriod` = 5
  - `SlowMaPeriod` = 10
  - `AllowBuyEntry` = true
  - `AllowSellEntry` = true
  - `AllowLongExit` = true
  - `AllowShortExit` = true
