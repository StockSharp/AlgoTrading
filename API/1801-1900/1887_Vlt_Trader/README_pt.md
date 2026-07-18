# Estratégia Vlt Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia detecta períodos de volatilidade muito baixa e prepara ordens de rompimento. Quando o intervalo da vela atual se torna o menor ao longo do período de lookback especificado, a estratégia coloca ordens stop de compra e venda ao redor da vela anterior.

## Parâmetros
- **Period** – período de lookback para o cálculo do intervalo mínimo.
- **Pending level** – distância em ticks da máxima/mínima da vela anterior para colocar as ordens stop.
- **Stop loss** – stop de proteção em ticks.
- **Take profit** – alvo de lucro em ticks.
- **Candle type** – período utilizado para a análise.

## Lógica
1. Para cada vela concluída, calcular seu intervalo (`High - Low`).
2. Rastrear o menor intervalo nas últimas *Period* velas.
3. Quando o intervalo atual estabelece um novo mínimo, cancelar as ordens existentes e colocar ordens stop acima e abaixo da vela anterior com o deslocamento fornecido.
4. `StartProtection` gerencia o stop-loss e o take-profit assim que uma posição é aberta.
