# Edição Simulador VisualTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma portabilidade simplificada dos scripts VisualTrader do MetaTrader.

Ela abre uma única posição a mercado na direção escolhida e anexa ordens protetoras de stop-loss e take-profit. Os parâmetros permitem configurar a direção, o take profit e o stop loss em valores de preço absolutos. A estratégia demonstra como scripts de gerenciamento manual de operações podem ser recriados usando a API de alto nível do StockSharp.

## Parâmetros

- **Trade Direction** – escolher Buy ou Sell para a ordem inicial.
- **Take Profit** – valor opcional de take profit em preço absoluto. Definir como 0 para desativar.
- **Stop Loss** – valor opcional de stop loss em preço absoluto. Definir como 0 para desativar.
- **Volume** – volume base da estratégia usado para a ordem a mercado.

## Lógica de Trading

Ao iniciar, a estratégia:

1. Cria ordens protetoras usando `StartProtection`.
2. Envia uma ordem a mercado com base na direção de trading selecionada.

O exemplo não depende de indicadores ou dados de mercado e tem fins de demonstração.
