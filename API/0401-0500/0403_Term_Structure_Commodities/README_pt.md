# Estratégia de Estrutura a Termo em Commodities
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera a inclinação das curvas de futuros de commodities. Compra contratos em backwardation e vende os que estão em contango, apostando na reversão à média da estrutura a termo.

A cada mês o sistema classifica os futuros por carry, assumindo posições compradas na maior backwardation e vendidas no contango mais acentuado. As posições são roladas antes do vencimento.

## Detalhes

- **Dados**: Preços de futuros próximos e diferidos.
- **Entrada**: Comprado em commodities de maior carry, vendido nas de menor carry.
- **Saída**: Rolar no vencimento do contrato ou se o carry mudar de sinal.
- **Instrumentos**: Futuros de commodities.
- **Risco**: Ponderação equitativa em dólares com stop em caso de variação adversa do carry.

