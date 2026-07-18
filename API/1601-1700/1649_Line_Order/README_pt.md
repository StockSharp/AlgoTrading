# Estratégia de Ordem em Linha
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Ordem em Linha** dispara uma ordem de mercado quando o preço cruza uma linha horizontal definida pelo usuário. Foi concebida como uma conversão simplificada do script MQL original *LineOrder.mq4*, fornecendo funcionalidade de negociação manual por linhas através da API de alto nível do StockSharp.

A estratégia expõe parâmetros para controlar direção, nível de entrada e gerenciamento de risco. Após entrar em uma posição, níveis opcionais de stop-loss, take-profit e trailing stop são monitorados em cada vela concluída. A lógica é totalmente orientada a eventos e não mantém coleções personalizadas.

## Parâmetros
- **LinePrice** – nível de preço para colocar a ordem.
- **IsBuy** – `true` para entradas compradas, `false` para entradas vendidas.
- **StopLoss** – distância do stop-loss em unidades de preço (0 desativa).
- **TakeProfit** – distância do take-profit em unidades de preço (0 desativa).
- **TrailingStop** – distância do trailing stop em unidades de preço (0 desativa).
- **Volume** – volume da ordem.
- **CandleType** – tipo de vela utilizado para monitorar o preço.

## Regras de Negociação
- **Entrada**: quando o preço de fechamento cruza `LinePrice` na direção escolhida.
- **Stop-loss**: fecha a posição quando a perda supera a distância `StopLoss` a partir da entrada.
- **Take-profit**: fecha a posição quando o lucro atinge a distância `TakeProfit`.
- **Trailing stop**: após a entrada, ajusta-se ao preço mais favorável e fecha quando o preço se move contra a posição em `TrailingStop`.

## Notas
- Funciona com qualquer ativo suportado pelo StockSharp.
- Projetado para fins educacionais para ilustrar a tradução de negociação manual por linhas do MQL.
- A versão em Python foi intencionalmente omitida.
