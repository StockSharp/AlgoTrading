# Estratégia de Grade de Compra e Venda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia implementa uma abordagem de grade simples que sempre mantém uma posição comprada e uma vendida abertas. Quando o mercado se move o suficiente para atingir o take profit de um lado, o lado oposto também é fechado e o próximo nível de grade é aberto com um volume maior. O volume cresce geometricamente de acordo com o parâmetro `VolumeMultiplier`.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `TakeProfitPoints` | Distância do take profit medida em passos de preço. |
| `InitialVolume` | Volume utilizado para o primeiro par de ordens. |
| `VolumeMultiplier` | Multiplicador aplicado ao volume para cada novo nível de grade. |
| `MaxTrades` | Número máximo de níveis de grade permitidos. |
| `CandleType` | Tipo de dados de velas usado para acionar a lógica da estratégia. |

## Lógica de trading

1. **Início** – A estratégia assina a série de velas especificada e abre o primeiro par de ordens de mercado de compra e venda.
2. **Monitoramento** – Em cada vela concluída, o último preço é verificado em relação aos preços de entrada. Se o objetivo de lucro de um lado for atingido, ambas as posições são fechadas.
3. **Progressão da grade** – Após fechar todas as posições, o próximo nível de grade é aberto com volume multiplicado por `VolumeMultiplier`.
4. **Limites** – O processo se repete até que `MaxTrades` níveis sejam abertos.

A estratégia não usa indicadores ou cálculos complexos, o que a torna adequada para demonstração de gerenciamento de ordens e posições dentro do StockSharp.

## Notas

- Todos os comentários no código são escritos em inglês conforme necessário.
- A estratégia usa a API de alto nível com `SubscribeCandles` para dados de mercado.
