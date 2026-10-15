# Estratégia de Sinal de Cruzamento de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera no cruzamento de duas Médias Móveis Exponenciais (EMA). Uma EMA mais rápida e uma mais lenta são calculadas a partir da série de velas escolhida. Quando a EMA rápida cruza acima da EMA lenta, a estratégia pode fechar qualquer posição vendida existente e opcionalmente abrir uma posição comprada. Quando a EMA rápida cruza abaixo da EMA lenta, pode fechar uma posição comprada e opcionalmente abrir uma posição vendida.

Para gerenciar o risco, a estratégia permite colocar ordens de take profit e stop loss após abrir uma nova posição. Ambas as distâncias são especificadas em ticks. Essas ordens de proteção são canceladas e recriadas a cada nova entrada.

A estratégia fornece interruptores separados para habilitar ou desabilitar entradas compradas e vendidas, bem como para fechar independentemente posições compradas e vendidas no sinal oposto. Todos os cálculos usam apenas velas finalizadas.

## Parâmetros
- **Fast Period** – comprimento da EMA rápida.
- **Slow Period** – comprimento da EMA lenta.
- **Candle Type** – período das velas usadas para os cálculos.
- **Allow Buy Open** – abrir comprado quando a EMA rápida cruza acima da EMA lenta.
- **Allow Sell Open** – abrir vendido quando a EMA rápida cruza abaixo da EMA lenta.
- **Allow Buy Close** – fechar comprado quando a EMA rápida cruza abaixo da EMA lenta.
- **Allow Sell Close** – fechar vendido quando a EMA rápida cruza acima da EMA lenta.
- **Take Profit Ticks** – distância de take profit em ticks a partir do preço de entrada.
- **Stop Loss Ticks** – distância de stop loss em ticks a partir do preço de entrada.
