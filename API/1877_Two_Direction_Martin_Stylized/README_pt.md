# Estratégia Two Direction Martin Stylized
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa uma abordagem martingale bidirecional simplificada. No início, abre posições compradas e vendidas simultaneamente e coloca ordens limitadas a uma distância configurável para capturar lucros.

## Como funciona
1. Calcula o spread e define a distância do take profit como porcentagem do preço ask atual.
2. Envia uma ordem de venda a mercado inicial com um alvo de compra limitada abaixo do bid e uma ordem de compra a mercado com um alvo de venda limitada acima do ask.
3. Quando uma das ordens limitadas está ausente ou o preço se move fora do intervalo predefinido, o algoritmo recalcula os volumes usando `Same Side %` e substitui as ordens pendentes. Ordens de mercado adicionais são enviadas para equilibrar a posição se necessário.
4. Todas as ordens são divididas em partes que não excedem o parâmetro `Volume Limit`.

## Parâmetros
- **Take Profit %** – distância do preço atual para os alvos de lucro.
- **Base Volume** – volume mínimo para cada ordem inicial.
- **Volume Limit** – volume máximo para uma única parte de ordem.
- **Same Side %** – porcentagem do volume total alocado para o lado dominante.
- **Candle Type** – tipo de candle usado como motor de tempo.
