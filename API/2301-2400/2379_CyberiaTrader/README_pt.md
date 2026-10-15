# Estratégia CyberiaTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port simplificado do StockSharp do sistema original **CyberiaTrader.mq5**. Combina vários indicadores técnicos clássicos para avaliar a direção do mercado e abrir operações quando a maioria dos filtros concordam.

## Indicadores

- **MACD** – Detecta mudanças de momentum usando EMAs rápidas/lentas e uma linha de sinal.
- **Média Móvel Simples** – Determina a tendência predominante.
- **Commodity Channel Index** – Filtra condições de sobrecompra/sobrevenda.
- **Average Directional Index** – Confirma a força direcional através dos componentes +DI e -DI.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `MacdFast` | Período da EMA rápida para MACD. |
| `MacdSlow` | Período da EMA lenta para MACD. |
| `MacdSignal` | Período da linha de sinal para MACD. |
| `MaPeriod` | Comprimento do filtro de tendência da média móvel. |
| `CciPeriod` | Período do Commodity Channel Index. |
| `AdxPeriod` | Período do Average Directional Index. |
| `EnableMacd` | Ativar/desativar o filtro MACD. |
| `EnableMa` | Ativar/desativar o filtro de média móvel. |
| `EnableCci` | Ativar/desativar o filtro CCI. |
| `EnableAdx` | Ativar/desativar o filtro ADX. |
| `CandleType` | Período das velas de entrada. |

## Lógica de trading

1. Os valores de todos os indicadores ativados são calculados em cada vela completada.
2. Os filtros podem bloquear compras ou vendas com base em suas respectivas regras:
   - MACD acima da sua linha de sinal bloqueia entradas vendidas; abaixo bloqueia compradas.
   - Preço acima da média móvel bloqueia vendidos; abaixo bloqueia comprados.
   - CCI acima de +100 bloqueia comprados; abaixo de -100 bloqueia vendidos.
   - +DI maior que -DI bloqueia vendidos; -DI maior que +DI bloqueia comprados.
3. Uma operação é aberta somente se um lado é permitido e o oposto está bloqueado.
4. A proteção básica de posição usa take-profit de 2% e stop-loss de 1%.

## Notas

Esta tradução foca nos filtros direcionais principais do algoritmo original. A extensa análise de probabilidade e os módulos auxiliares da versão MQL5 são intencionalmente omitidos para maior clareza.
