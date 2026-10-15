# Estratégia de Cruzamento do Indicador Vortex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera os cruzamentos das linhas positiva (VI+) e negativa (VI-) do indicador Vortex.
Quando VI+ cruza acima de VI-, a estratégia vai comprado; quando VI- cruza acima de VI+, vai vendido.
Um stop-loss e um take-profit em passos de preço são gerenciados automaticamente.

## Parâmetros

- **Vortex Length** – período do indicador Vortex.
- **Candle Type** – período utilizado para o cálculo do indicador.
- **Stop Loss** – stop de proteção em passos de preço.
- **Take Profit** – lucro alvo em passos de preço.

## Detalhes

- **Indicadores**: Vortex
- **Direção**: Comprado e vendido
- **Período**: Configurável
- **Gestão de risco**: Stop-loss e take-profit via `StartProtection`.
