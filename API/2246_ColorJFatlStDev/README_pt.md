# Estratégia ColorJFatl StDev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma tradução do consultor especialista **ColorJFatl_StDev** de MQL5 para a API StockSharp. Ela combina a Média Móvel Jurik (JMA) com bandas de desvio padrão para gerar sinais de negociação.

## Lógica da estratégia

1. Calcular o JMA sobre os preços de fechamento.
2. Calcular o desvio padrão durante um período configurável.
3. Construir dois conjuntos de bandas dinâmicas usando os multiplicadores `K1` e `K2`:
   - `upper1 = JMA + K1 * StdDev`
   - `upper2 = JMA + K2 * StdDev`
   - `lower1 = JMA - K1 * StdDev`
   - `lower2 = JMA - K2 * StdDev`
4. Dependendo do modo de sinal selecionado, a estratégia abre ou fecha posições:
   - **Point** – ativado quando o preço cruza as bandas.
   - **Direct** – usa os pontos de inflexão da linha JMA.
   - **Without** – desativa o sinal correspondente.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `CandleTimeFrame` | Período para dados de velas. |
| `JmaLength` | Período da Média Móvel Jurik. |
| `JmaPhase` | Fase para o cálculo do JMA. |
| `StdPeriod` | Período para o desvio padrão. |
| `K1` | Primeiro multiplicador de desvio. |
| `K2` | Segundo multiplicador de desvio. |
| `BuyOpenMode` | Modo para abrir posições compradas. |
| `SellOpenMode` | Modo para abrir posições vendidas. |
| `BuyCloseMode` | Modo para fechar posições compradas. |
| `SellCloseMode` | Modo para fechar posições vendidas. |

## Uso

A estratégia subscreve velas do período especificado, processa os valores de JMA e desvio padrão e envia automaticamente ordens de mercado com base nos modos definidos.

Esta implementação foca na clareza e pode servir como ponto de partida para melhorias adicionais ou gestão de risco personalizada.
